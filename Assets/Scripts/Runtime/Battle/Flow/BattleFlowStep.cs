using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public enum BattleFlowPhase
    {
        Ready = 0,
        AwaitingPlayerSkill = 1,
        Completed = 2,
    }

    public enum BattleFlowStepKind
    {
        PlayerInputRequired = 0,
        ActionStarted = 1,
        ActionResolved = 2,
        ActionCancelled = 3,
        BattleCompleted = 4,
    }

    public sealed class BattleFlowStep
    {
        private BattleFlowStep(
            BattleFlowStepKind kind,
            BattleUnitState actor,
            IReadOnlyList<BattleSkillChoice> skillChoices,
            SkillAsset struggleSkill,
            SkillResolution resolution,
            PendingSkillAction pendingAction,
            BattleOutcome outcome,
            bool wasAutomaticallySelected)
        {
            Kind = kind;
            Actor = actor;
            SkillChoices = skillChoices ?? Array.Empty<BattleSkillChoice>();
            UsableSkills = SkillChoices
                .Where(choice => choice.IsUsable)
                .Select(choice => choice.Skill)
                .ToArray();
            StruggleSkill = struggleSkill;
            Resolution = resolution;
            PendingAction = pendingAction;
            Outcome = outcome;
            WasAutomaticallySelected = wasAutomaticallySelected;
            RequiresStruggleConfirmation = StruggleSkill != null;
        }

        public BattleFlowStepKind Kind { get; }
        public BattleUnitState Actor { get; }
        public IReadOnlyList<BattleSkillChoice> SkillChoices { get; }
        public IReadOnlyList<SkillAsset> UsableSkills { get; }
        public SkillAsset StruggleSkill { get; }
        public SkillResolution Resolution { get; }
        public PendingSkillAction PendingAction { get; }
        public BattleOutcome Outcome { get; }
        public bool WasAutomaticallySelected { get; }
        public bool RequiresStruggleConfirmation { get; }

        internal static BattleFlowStep RequirePlayerInput(
            BattleUnitState actor,
            IEnumerable<BattleSkillChoice> skillChoices,
            SkillAsset struggleSkill)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            var choices = skillChoices?.ToArray()
                ?? throw new ArgumentNullException(nameof(skillChoices));
            return new BattleFlowStep(
                BattleFlowStepKind.PlayerInputRequired,
                actor,
                choices,
                struggleSkill,
                null,
                null,
                BattleOutcome.Undecided,
                false);
        }

        internal static BattleFlowStep StartAction(
            PendingSkillAction pendingAction,
            bool wasAutomaticallySelected)
        {
            if (pendingAction == null) throw new ArgumentNullException(nameof(pendingAction));
            return new BattleFlowStep(
                BattleFlowStepKind.ActionStarted,
                pendingAction.User,
                Array.Empty<BattleSkillChoice>(),
                null,
                null,
                pendingAction,
                BattleOutcome.Undecided,
                wasAutomaticallySelected);
        }

        internal static BattleFlowStep ResolveAction(
            SkillResolution resolution,
            BattleOutcome outcome,
            bool wasAutomaticallySelected)
        {
            if (resolution == null) throw new ArgumentNullException(nameof(resolution));
            return new BattleFlowStep(
                BattleFlowStepKind.ActionResolved,
                resolution.User,
                Array.Empty<BattleSkillChoice>(),
                null,
                resolution,
                null,
                outcome,
                wasAutomaticallySelected);
        }

        internal static BattleFlowStep CancelAction(
            PendingSkillAction pendingAction,
            bool wasAutomaticallySelected)
        {
            if (pendingAction == null) throw new ArgumentNullException(nameof(pendingAction));
            return new BattleFlowStep(
                BattleFlowStepKind.ActionCancelled,
                pendingAction.User,
                Array.Empty<BattleSkillChoice>(),
                null,
                null,
                pendingAction,
                BattleOutcome.Undecided,
                wasAutomaticallySelected);
        }

        internal static BattleFlowStep Complete(BattleOutcome outcome)
        {
            if (outcome == BattleOutcome.Undecided)
            {
                throw new ArgumentException("A completed Battle requires a decided outcome.", nameof(outcome));
            }

            return new BattleFlowStep(
                BattleFlowStepKind.BattleCompleted,
                null,
                Array.Empty<BattleSkillChoice>(),
                null,
                null,
                null,
                outcome,
                false);
        }
    }
}
