using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class BattleTimeline
    {
        public const int DefaultInitialBaseDelay = 100;
        private const uint TiePriorityStreamSalt = 0x54494501u;

        private readonly BattleState _state;
        private readonly Queue<BattleUnitState> _sameTickQueue = new();
        private BattleUnitState _currentActor;

        internal BattleTimeline(
            BattleState state,
            int initialBaseDelay = DefaultInitialBaseDelay)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            if (initialBaseDelay <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialBaseDelay));
            }

            var units = GetAllUnitsInStableOrder().ToArray();
            AssignTiePriorities(units, state.BattleSeed);
            foreach (var unit in units)
            {
                unit.Timing.BeginInitialDelay(initialBaseDelay);
            }
        }

        public BattleUnitState CurrentActor => _currentActor;

        public bool TryBeginNextTurn(out BattleUnitState actor)
        {
            if (_currentActor != null)
            {
                throw new InvalidOperationException(
                    "The current turn must be completed before beginning another turn.");
            }

            actor = null;
            if (_state.EvaluateOutcome() != BattleOutcome.Undecided)
            {
                return false;
            }

            while (true)
            {
                while (_sameTickQueue.Count > 0)
                {
                    var queuedUnit = _sameTickQueue.Dequeue();
                    if (!queuedUnit.IsAlive
                        || queuedUnit.Timing.IsPaused
                        || !queuedUnit.Timing.IsComplete
                        || !IsTurnClockPhase(queuedUnit.Timing.Phase))
                    {
                        continue;
                    }

                    queuedUnit.Timing.MarkReady();
                    _currentActor = queuedUnit;
                    actor = queuedUnit;
                    return true;
                }

                var eligibleUnits = GetAllUnitsInStableOrder()
                    .Where(IsTurnClockEligible)
                    .ToArray();
                var nextTurnRemaining = eligibleUnits
                    .Select(unit => unit.GetActionRemainingTicks())
                    .DefaultIfEmpty(int.MaxValue)
                    .Min();
                var nextStatusRemaining =
                    _state.Statuses.GetNextExpirationTicks();
                var nextRemaining = Math.Min(
                    nextTurnRemaining,
                    nextStatusRemaining);
                if (nextRemaining == int.MaxValue)
                {
                    return false;
                }

                if (nextRemaining < 0)
                {
                    throw new InvalidOperationException(
                        "A Unit has a negative remaining action clock.");
                }

                AdvanceBy(nextRemaining);
                foreach (var unit in GetAllUnitsInStableOrder()
                             .Where(IsTurnClockEligible)
                             .Where(unit => unit.Timing.IsComplete)
                             .OrderBy(unit => unit.TiePriority))
                {
                    _sameTickQueue.Enqueue(unit);
                }
            }
        }

        public int BeginStartup(
            BattleUnitState actor,
            int usedSkillSlotId,
            BattleSkillTimingPlan timing)
        {
            ValidateCurrentActor(actor, usedSkillSlotId);
            if (timing.StartupWork <= 0m)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timing),
                    "A delayed action requires positive Startup.");
            }

            StartCooldown(actor, usedSkillSlotId, timing.CooldownWork);
            actor.Timing.BeginStartup(timing.StartupWork);
            _currentActor = null;
            return actor.GetActionRemainingTicks();
        }

        public void CompleteImmediateAction(
            BattleUnitState actor,
            int usedSkillSlotId,
            BattleSkillTimingPlan timing)
        {
            ValidateCurrentActor(actor, usedSkillSlotId);
            StartCooldown(actor, usedSkillSlotId, timing.CooldownWork);
            CompleteRecovery(actor, timing.RecoveryWork);
            _currentActor = null;
            _state.EvaluateOutcome();
        }

        public void CompleteDelayedAction(
            BattleUnitState actor,
            BattleSkillTimingPlan timing)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (_currentActor != null)
            {
                throw new InvalidOperationException(
                    "A delayed action cannot resolve during an active turn.");
            }

            CompleteRecovery(actor, timing.RecoveryWork);
            _state.EvaluateOutcome();
        }

        public long GetNextTurnTick()
        {
            var remainingTicks = GetAllUnitsInStableOrder()
                .Where(IsTurnClockEligible)
                .Select(unit => unit.GetActionRemainingTicks())
                .DefaultIfEmpty(int.MaxValue)
                .Min();
            return remainingTicks == int.MaxValue
                ? long.MaxValue
                : AddTicks(_state.CurrentTick, remainingTicks);
        }

        public long GetNextStatusExpirationTick()
        {
            var remainingTicks = _state.Statuses.GetNextExpirationTicks();
            return remainingTicks == int.MaxValue
                ? long.MaxValue
                : AddTicks(_state.CurrentTick, remainingTicks);
        }

        public void AdvanceToTick(long tick)
        {
            if (_currentActor != null)
            {
                throw new InvalidOperationException(
                    "Cannot advance the Timeline during an active turn.");
            }

            if (tick < _state.CurrentTick)
            {
                throw new ArgumentOutOfRangeException(nameof(tick));
            }

            var delta = tick - _state.CurrentTick;
            if (delta > int.MaxValue)
            {
                throw new OverflowException(
                    "A single Timeline advance exceeded the Int32 tick range.");
            }

            AdvanceBy((int)delta);
        }

        private static void CompleteRecovery(
            BattleUnitState actor,
            decimal recoveryWork)
        {
            actor.Timing.BeginRecovery(recoveryWork);
        }

        private void ValidateCurrentActor(BattleUnitState actor, int usedSkillSlotId)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (!ReferenceEquals(actor, _currentActor))
            {
                throw new InvalidOperationException(
                    "Only the Unit whose turn is active can use an action.");
            }

            if (usedSkillSlotId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(usedSkillSlotId));
            }
        }

        private void StartCooldown(
            BattleUnitState actor,
            int usedSkillSlotId,
            decimal cooldownWork)
        {
            // Slot 0 is reserved for system actions such as Struggle.
            if (usedSkillSlotId > 0)
            {
                actor.StartCooldown(usedSkillSlotId, cooldownWork);
            }
        }

        private void AdvanceBy(int ticks)
        {
            if (ticks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ticks));
            }

            for (var elapsed = 0; elapsed < ticks; elapsed++)
            {
                foreach (var unit in GetAllUnitsInStableOrder())
                {
                    unit.AdvanceClocksOneTick();
                }

                // This tick uses the current status values, then decays them.
                _state.Statuses.AdvanceTime(1);
                _state.CurrentTick = AddTicks(_state.CurrentTick, 1);
            }
        }

        private static bool IsTurnClockEligible(BattleUnitState unit)
        {
            if (unit == null || !unit.IsAlive || unit.Timing.IsPaused)
            {
                return false;
            }

            return IsTurnClockPhase(unit.Timing.Phase);
        }

        private static bool IsTurnClockPhase(BattleActionPhase phase)
        {
            return phase == BattleActionPhase.InitialDelay
                || phase == BattleActionPhase.Recovery
                || phase == BattleActionPhase.Ready;
        }

        private IEnumerable<BattleUnitState> GetAllUnitsInStableOrder()
        {
            return _state.Player.Units.Concat(_state.Enemy.Units);
        }

        private static long AddTicks(long currentTick, int tickCount)
        {
            if (currentTick > long.MaxValue - tickCount)
            {
                throw new OverflowException("Battle Tick exceeded the Int64 range.");
            }

            return currentTick + tickCount;
        }

        private static void AssignTiePriorities(
            IReadOnlyList<BattleUnitState> units,
            int battleSeed)
        {
            var shuffled = units.ToArray();
            var random = new StableBattleRandom(battleSeed, TiePriorityStreamSalt);
            for (var index = shuffled.Length - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);
                (shuffled[index], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[index]);
            }

            for (var priority = 0; priority < shuffled.Length; priority++)
            {
                shuffled[priority].TiePriority = priority;
            }
        }

    }
}
