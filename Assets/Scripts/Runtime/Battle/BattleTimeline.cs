using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class BattleTimeline
    {
        public const int DefaultInitialBaseTurnCost = 100;
        private const uint TiePriorityStreamSalt = 0x54494501u;

        private readonly BattleState _state;
        private readonly Queue<BattleUnitState> _sameTickQueue = new();
        private BattleUnitState _currentActor;

        internal BattleTimeline(
            BattleState state,
            int initialBaseTurnCost = DefaultInitialBaseTurnCost)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            if (initialBaseTurnCost <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialBaseTurnCost));
            }

            var units = GetAllUnitsInStableOrder().ToArray();
            AssignTiePriorities(units, state.BattleSeed);
            foreach (var unit in units)
            {
                unit.NextTurnTick = BattleTickMath.GetEffectiveTurnCost(
                    initialBaseTurnCost,
                    unit.StartingStats.GetValue(PachimonStatType.Speed));
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
                    if (!queuedUnit.IsAlive)
                    {
                        continue;
                    }

                    _currentActor = queuedUnit;
                    actor = queuedUnit;
                    return true;
                }

                var livingUnits = GetAllUnitsInStableOrder()
                    .Where(unit => unit.IsAlive)
                    .ToArray();
                if (livingUnits.Length == 0)
                {
                    return false;
                }

                var nextTick = livingUnits.Min(unit => unit.NextTurnTick);
                if (nextTick < _state.CurrentTick)
                {
                    throw new InvalidOperationException(
                        "A Unit was scheduled before the current Battle Tick.");
                }

                _state.CurrentTick = nextTick;
                foreach (var unit in livingUnits
                             .Where(unit => unit.NextTurnTick == nextTick)
                             .OrderBy(unit => unit.TiePriority))
                {
                    _sameTickQueue.Enqueue(unit);
                }
            }
        }

        public void CompleteTurn(
            BattleUnitState actor,
            int usedSkillSlotId,
            int baseTurnCost,
            int baseCooldown)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (!ReferenceEquals(actor, _currentActor))
            {
                throw new InvalidOperationException(
                    "Only the Unit whose turn is active can complete the turn.");
            }

            if (usedSkillSlotId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(usedSkillSlotId));
            }

            var effectiveTurnCost = BattleTickMath.GetEffectiveTurnCost(
                baseTurnCost,
                actor.StartingStats.GetValue(PachimonStatType.Speed));
            var effectiveCooldown = BattleTickMath.GetEffectiveCooldown(
                baseCooldown,
                actor.StartingStats.GetValue(PachimonStatType.Haste));
            actor.NextTurnTick = AddTicks(_state.CurrentTick, effectiveTurnCost);
            // Slot 0 is reserved for system actions such as Struggle.
            if (usedSkillSlotId > 0)
            {
                actor.SetCooldownReadyTick(
                    usedSkillSlotId,
                    AddTicks(_state.CurrentTick, effectiveCooldown));
            }
            _currentActor = null;
            _state.EvaluateOutcome();
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
