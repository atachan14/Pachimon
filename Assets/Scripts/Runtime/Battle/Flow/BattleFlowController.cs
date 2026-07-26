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
                var playerChoices = _skillRuntime.GetRegularSkillChoices(_state, actor);
                var usableSkillSlotIds = playerChoices
                    .Where(choice => choice.IsUsable)
                    .Select(choice => choice.SlotId)
                    .ToArray();
                var struggleSkill = usableSkillSlotIds.Length == 0
                    ? _skillRuntime.GetStruggleSkill()
                    : null;
                _awaitingPlayerUnit = actor;
                _awaitingPlayerSkillSlotIds = struggleSkill == null
                    ? new HashSet<int>(usableSkillSlotIds)
                    : new HashSet<int> { 0 };
                Phase = BattleFlowPhase.AwaitingPlayerSkill;
                return BattleFlowStep.RequirePlayerInput(
                    actor,
                    playerChoices,
                    struggleSkill);
            }

            var usableChoices = _skillRuntime.GetUsableRegularSkillChoices(_state, actor);
            var selectedSkillSlotId = usableChoices.Count == 0
                ? 0
                : _enemySkillSelector.Select(
                    usableChoices.Select(choice => choice.SlotId).ToArray());
            return ResolveAction(actor, selectedSkillSlotId, true);
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
            var step = ResolveAction(actor, skillSlotId, false);
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

        private BattleFlowStep ResolveAction(
            BattleUnitState actor,
            int skillSlotId,
            bool wasAutomaticallySelected)
        {
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
    }
}
