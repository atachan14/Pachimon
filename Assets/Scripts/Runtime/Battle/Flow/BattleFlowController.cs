using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    /// <summary>
    /// Advances the Battle by at most one action per call.
    /// Presentation controls pacing by deciding when to request the next step.
    /// </summary>
    public sealed class BattleFlowController
    {
        private readonly BattleState _state;
        private readonly BattleSkillRuntime _skillRuntime;
        private readonly SeededEnemySkillSelector _enemySkillSelector;
        private BattleUnitState _awaitingPlayerUnit;
        private HashSet<int> _awaitingPlayerSkillSlotIds = new();
        private readonly List<ScheduledFlowAction> _pendingActions = new();

        public BattleFlowController(BattleState state, BattleSkillRuntime skillRuntime)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _skillRuntime = skillRuntime ?? throw new ArgumentNullException(nameof(skillRuntime));
            _enemySkillSelector = new SeededEnemySkillSelector(state.BattleSeed);
        }

        public BattleFlowPhase Phase { get; private set; } = BattleFlowPhase.Ready;
        public BattleState State => _state;

        public BattleFlowStep Advance()
        {
            if (Phase == BattleFlowPhase.AwaitingPlayerSkill)
            {
                throw new InvalidOperationException(
                    "Submit the awaiting Player Skill before advancing the Battle.");
            }

            var outcome = _state.EvaluateOutcome();
            if (outcome != BattleOutcome.Undecided)
            {
                Phase = BattleFlowPhase.Completed;
                return BattleFlowStep.Complete(outcome);
            }

            var cancelledAction = _pendingActions
                .FirstOrDefault(action => !action.Action.User.IsAlive);
            if (cancelledAction != null)
            {
                _pendingActions.Remove(cancelledAction);
                return BattleFlowStep.CancelAction(
                    cancelledAction.Action,
                    cancelledAction.WasAutomaticallySelected);
            }

            while (true)
            {
                var nextPending = _pendingActions
                    .Where(action =>
                        action.Action.User.IsAlive
                        && !action.Action.User.Timing.IsPaused
                        && action.Action.User.Timing.Phase
                            == BattleActionPhase.Startup)
                    .OrderBy(action =>
                        action.Action.User.GetActionRemainingTicks())
                    .ThenBy(action => action.Action.User.TiePriority)
                    .FirstOrDefault();
                var nextPendingTick = nextPending == null
                    ? long.MaxValue
                    : AddTicks(
                        _state.CurrentTick,
                        nextPending.Action.User.GetActionRemainingTicks());
                var nextTurnTick = _state.Timeline.GetNextTurnTick();
                var nextStatusTick =
                    _state.Timeline.GetNextStatusExpirationTick();
                if (nextStatusTick != long.MaxValue
                    && nextStatusTick <= nextPendingTick
                    && nextStatusTick <= nextTurnTick)
                {
                    _state.Timeline.AdvanceToTick(nextStatusTick);
                    outcome = _state.EvaluateOutcome();
                    if (outcome != BattleOutcome.Undecided)
                    {
                        Phase = BattleFlowPhase.Completed;
                        return BattleFlowStep.Complete(outcome);
                    }
                    continue;
                }

                if (nextPending == null || nextPendingTick > nextTurnTick)
                {
                    break;
                }

                _state.Timeline.AdvanceToTick(nextPendingTick);
                outcome = _state.EvaluateOutcome();
                if (outcome != BattleOutcome.Undecided)
                {
                    Phase = BattleFlowPhase.Completed;
                    return BattleFlowStep.Complete(outcome);
                }
                if (!nextPending.Action.User.IsAlive)
                {
                    _pendingActions.Remove(nextPending);
                    return BattleFlowStep.CancelAction(
                        nextPending.Action,
                        nextPending.WasAutomaticallySelected);
                }

                // Dynamic Speed modifiers can change while time advances, so
                // a prediction made before AdvanceToTick may be early.
                if (nextPending.Action.User.Timing.Phase
                        != BattleActionPhase.Startup
                    || !nextPending.Action.User.Timing.IsComplete)
                {
                    continue;
                }

                _pendingActions.Remove(nextPending);
                var pendingResolution = _skillRuntime.ExecutePending(
                    _state,
                    nextPending.Action);
                outcome = _state.EvaluateOutcome();
                Phase = outcome == BattleOutcome.Undecided
                    ? BattleFlowPhase.Ready
                    : BattleFlowPhase.Completed;
                return BattleFlowStep.ResolveAction(
                    pendingResolution,
                    outcome,
                    nextPending.WasAutomaticallySelected);
            }

            if (!_state.Timeline.TryBeginNextTurn(out var actor))
            {
                outcome = _state.EvaluateOutcome();
                if (outcome == BattleOutcome.Undecided)
                {
                    throw new InvalidOperationException(
                        "The Timeline could not produce an actor for an undecided Battle.");
                }

                Phase = BattleFlowPhase.Completed;
                return BattleFlowStep.Complete(outcome);
            }

            if (actor.Side == BattleSide.Player)
            {
                _awaitingPlayerUnit = actor;
                Phase = BattleFlowPhase.AwaitingPlayerSkill;
                return RefreshPlayerSkillChoices();
            }

            var usableChoices = _skillRuntime.GetUsableRegularSkillChoices(_state, actor);
            var selectedSkillSlotId = usableChoices.Count == 0
                ? 0
                : _enemySkillSelector.Select(
                    usableChoices.Select(choice => choice.SlotId).ToArray());
            return ResolveOrStartAction(actor, selectedSkillSlotId, true);
        }

        private static long AddTicks(long currentTick, int tickCount)
        {
            if (currentTick > long.MaxValue - tickCount)
            {
                throw new OverflowException("Battle Tick exceeded the Int64 range.");
            }

            return currentTick + tickCount;
        }

        public BattleFlowStep RefreshPlayerSkillChoices()
        {
            if (Phase != BattleFlowPhase.AwaitingPlayerSkill
                || _awaitingPlayerUnit == null)
            {
                throw new InvalidOperationException(
                    "The Battle is not awaiting a Player Skill.");
            }

            var playerChoices = _skillRuntime.GetRegularSkillChoices(
                _state,
                _awaitingPlayerUnit);
            var usableSkillSlotIds = playerChoices
                .Where(choice => choice.IsUsable)
                .Select(choice => choice.SlotId)
                .ToArray();
            var struggleSkill = usableSkillSlotIds.Length == 0
                ? _skillRuntime.GetStruggleSkill()
                : null;
            _awaitingPlayerSkillSlotIds = struggleSkill == null
                ? new HashSet<int>(usableSkillSlotIds)
                : new HashSet<int> { 0 };
            return BattleFlowStep.RequirePlayerInput(
                _awaitingPlayerUnit,
                playerChoices,
                struggleSkill);
        }

        public BattleFlowStep SubmitPlayerSkill(int skillSlotId)
        {
            if (Phase != BattleFlowPhase.AwaitingPlayerSkill || _awaitingPlayerUnit == null)
            {
                throw new InvalidOperationException("The Battle is not awaiting a Player Skill.");
            }

            if (!_awaitingPlayerSkillSlotIds.Contains(skillSlotId))
            {
                throw new ArgumentException(
                    $"Skill Slot {skillSlotId} is not available for the current Player turn.",
                    nameof(skillSlotId));
            }

            var actor = _awaitingPlayerUnit;
            var step = ResolveOrStartAction(actor, skillSlotId, false);
            _awaitingPlayerUnit = null;
            _awaitingPlayerSkillSlotIds = new HashSet<int>();
            return step;
        }

        public SkillPreview PreviewPlayerSkill(int skillSlotId)
        {
            if (Phase != BattleFlowPhase.AwaitingPlayerSkill || _awaitingPlayerUnit == null)
            {
                throw new InvalidOperationException("The Battle is not awaiting a Player Skill.");
            }

            if (!_awaitingPlayerSkillSlotIds.Contains(skillSlotId))
            {
                throw new ArgumentException(
                    $"Skill Slot {skillSlotId} is not available for the current Player turn.",
                    nameof(skillSlotId));
            }

            return _skillRuntime.PreviewCurrentTurn(
                _state,
                _awaitingPlayerUnit,
                skillSlotId);
        }

        private BattleFlowStep ResolveOrStartAction(
            BattleUnitState actor,
            int skillSlotId,
            bool wasAutomaticallySelected)
        {
            if (_skillRuntime.RequiresStartup(_state, actor, skillSlotId))
            {
                var pendingAction = _skillRuntime.BeginStartup(
                    _state,
                    actor,
                    skillSlotId);
                _pendingActions.Add(new ScheduledFlowAction(
                    pendingAction,
                    wasAutomaticallySelected));
                Phase = BattleFlowPhase.Ready;
                return BattleFlowStep.StartAction(
                    pendingAction,
                    wasAutomaticallySelected);
            }

            var resolution = _skillRuntime.ExecuteCurrentTurn(_state, actor, skillSlotId);
            var outcome = _state.EvaluateOutcome();
            Phase = outcome == BattleOutcome.Undecided
                ? BattleFlowPhase.Ready
                : BattleFlowPhase.Completed;
            return BattleFlowStep.ResolveAction(
                resolution,
                outcome,
                wasAutomaticallySelected);
        }

        private sealed class ScheduledFlowAction
        {
            public ScheduledFlowAction(
                PendingSkillAction action,
                bool wasAutomaticallySelected)
            {
                Action = action ?? throw new ArgumentNullException(nameof(action));
                WasAutomaticallySelected = wasAutomaticallySelected;
            }

            public PendingSkillAction Action { get; }
            public bool WasAutomaticallySelected { get; }
        }
    }
}
