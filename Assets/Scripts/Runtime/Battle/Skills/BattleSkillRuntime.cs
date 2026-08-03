using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Skills;
using Pachimon.Passives;

namespace Pachimon.Battle
{
    public sealed class PendingSkillAction
    {
        public PendingSkillAction(
            BattleUnitState user,
            SkillAsset skill,
            int skillSlotId,
            int startupTicks,
            BattleSkillTimingPlan timing)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            SkillSlotId = skillSlotId;
            StartupTicks = startupTicks;
            Timing = timing;
        }

        public BattleUnitState User { get; }
        public SkillAsset Skill { get; }
        public int SkillSlotId { get; }
        public int StartupTicks { get; }
        public BattleSkillTimingPlan Timing { get; }
    }

    public sealed class BattleSkillRuntime
    {
        private readonly SkillCatalog _skillCatalog;
        private readonly SkillLogicRegistry _logicRegistry;

        public BattleSkillRuntime(
            SkillCatalog skillCatalog,
            PassiveCatalog passiveCatalog,
            SkillLogicRegistry logicRegistry = null)
        {
            _skillCatalog = skillCatalog ?? throw new ArgumentNullException(nameof(skillCatalog));
            _logicRegistry = logicRegistry
                ?? new SkillLogicRegistry(skillCatalog, passiveCatalog);
        }

        public IReadOnlyList<BattleSkillChoice> GetUsableRegularSkillChoices(
            BattleState state,
            BattleUnitState user)
        {
            return GetRegularSkillChoices(state, user)
                .Where(choice => choice.IsUsable)
                .ToArray();
        }

        public IReadOnlyList<BattleSkillChoice> GetRegularSkillChoices(
            BattleState state,
            BattleUnitState user)
        {
            ValidateUser(state, user);
            return user.SkillSlots
                .Where(slot => slot.SkillId != SkillIdRanges.StruggleId)
                .Select(slot =>
                {
                    var skill = _skillCatalog.Get(slot.SkillId)
                        ?? throw new InvalidOperationException(
                            $"Skill {slot.SkillId} was not found.");
                    var remainingCooldown =
                        user.GetCooldownRemainingTicks(slot.SlotId);
                    var readyTick = state.CurrentTick + remainingCooldown;
                    var isCooldownReady = remainingCooldown == 0;
                    var hasEnoughMn = user.CanSpendMn(skill.BaseManaCost);
                    var isUsable = _logicRegistry.TryGet(slot.SkillId, out _)
                        && isCooldownReady
                        && hasEnoughMn;
                    return new BattleSkillChoice(
                        slot.SlotId,
                        skill,
                        isUsable,
                        readyTick,
                        isCooldownReady,
                        hasEnoughMn);
                })
                .ToArray();
        }

        public bool MustUseStruggle(BattleState state, BattleUnitState user)
        {
            return GetUsableRegularSkillChoices(state, user).Count == 0;
        }

        public SkillAsset GetStruggleSkill()
        {
            var skill = _skillCatalog.Get(SkillIdRanges.StruggleId)
                ?? throw new InvalidOperationException(
                    $"Skill {SkillIdRanges.StruggleId} (Struggle) was not found.");
            if (!_logicRegistry.TryGet(skill.SkillId, out _))
            {
                throw new InvalidOperationException(
                    $"Skill {SkillIdRanges.StruggleId} (Struggle) has no registered Logic.");
            }

            return skill;
        }

        public bool RequiresStartup(
            BattleState state,
            BattleUnitState user,
            int skillSlotId)
        {
            ValidateUser(state, user);
            if (!ReferenceEquals(state.Timeline.CurrentActor, user))
            {
                throw new InvalidOperationException("The Unit does not own the current turn.");
            }

            var skill = skillSlotId == 0
                ? GetStruggleSkill()
                : GetSkillForSlot(user, skillSlotId);
            ValidateSkillSelection(state, user, skillSlotId, skill);
            return skill.BaseStartupTicks > 0;
        }

        public SkillResolution ExecuteCurrentTurn(
            BattleState state,
            BattleUnitState user,
            int skillSlotId)
        {
            ValidateUser(state, user);
            if (!ReferenceEquals(state.Timeline.CurrentActor, user))
            {
                throw new InvalidOperationException("The Unit does not own the current turn.");
            }

            var skill = skillSlotId == 0
                ? GetStruggleSkill()
                : GetSkillForSlot(user, skillSlotId);
            ValidateSkillSelection(state, user, skillSlotId, skill);
            if (skill.BaseStartupTicks > 0)
            {
                throw new InvalidOperationException(
                    $"Skill {skill.SkillId} requires Startup before it can resolve.");
            }

            state.Presentation.Begin(user, skill);
            try
            {
                SpendMana(state, user, skill);
                var resolution = ResolveSkill(state, user, skill)
                    .WithPresentation(state.Presentation.Complete());
                var timing = SkillTimingCalculator.CreatePlan(
                    skill,
                    user);
                state.Timeline.CompleteImmediateAction(
                    user,
                    skillSlotId,
                    timing);
                return resolution;
            }
            catch
            {
                state.Presentation.Cancel();
                throw;
            }
        }

        public PendingSkillAction BeginStartup(
            BattleState state,
            BattleUnitState user,
            int skillSlotId)
        {
            ValidateUser(state, user);
            if (!ReferenceEquals(state.Timeline.CurrentActor, user))
            {
                throw new InvalidOperationException("The Unit does not own the current turn.");
            }

            var skill = skillSlotId == 0
                ? GetStruggleSkill()
                : GetSkillForSlot(user, skillSlotId);
            ValidateSkillSelection(state, user, skillSlotId, skill);
            if (skill.BaseStartupTicks <= 0)
            {
                throw new InvalidOperationException(
                    $"Skill {skill.SkillId} does not require Startup.");
            }

            if (!_logicRegistry.TryGet(skill.SkillId, out _))
            {
                throw new InvalidOperationException(
                    $"Skill {skill.SkillId} has no registered Logic.");
            }

            SpendMana(state, user, skill);
            var timing = SkillTimingCalculator.CreatePlan(
                skill,
                user);
            var startupTicks = state.Timeline.BeginStartup(
                user,
                skillSlotId,
                timing);
            return new PendingSkillAction(
                user,
                skill,
                skillSlotId,
                startupTicks,
                timing);
        }

        public SkillResolution ExecutePending(
            BattleState state,
            PendingSkillAction action)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (!action.User.IsAlive)
            {
                throw new InvalidOperationException(
                    "A defeated Unit cannot resolve a pending Skill.");
            }

            if (action.User.Timing.Phase != BattleActionPhase.Startup
                || !action.User.Timing.IsComplete)
            {
                throw new InvalidOperationException(
                    "The pending Skill has not reached its resolve Tick.");
            }

            state.Presentation.Begin(action.User, action.Skill);
            try
            {
                var resolution = ResolveSkill(state, action.User, action.Skill)
                    .WithPresentation(state.Presentation.Complete());
                state.Timeline.CompleteDelayedAction(
                    action.User,
                    action.Timing);
                return resolution;
            }
            catch
            {
                state.Presentation.Cancel();
                throw;
            }
        }

        public SkillPreview PreviewCurrentTurn(
            BattleState state,
            BattleUnitState user,
            int skillSlotId)
        {
            ValidateUser(state, user);
            if (!ReferenceEquals(state.Timeline.CurrentActor, user))
            {
                throw new InvalidOperationException("The Unit does not own the current turn.");
            }

            var skill = skillSlotId == 0
                ? GetStruggleSkill()
                : GetSkillForSlot(user, skillSlotId);
            ValidateSkillSelection(state, user, skillSlotId, skill);
            if (!_logicRegistry.TryGet(skill.SkillId, out var logic))
            {
                throw new InvalidOperationException(
                    $"Skill {skill.SkillId} has no registered Logic.");
            }

            return BattleSkillPreviewSimulator.Simulate(
                state,
                user,
                skill,
                logic,
                spendMana: true);
        }

        private void ValidateSkillSelection(
            BattleState state,
            BattleUnitState user,
            int skillSlotId,
            SkillAsset skill)
        {
            if (skill.SkillId == SkillIdRanges.StruggleId)
            {
                if (skillSlotId != 0)
                {
                    throw new InvalidOperationException(
                        "Struggle must use the reserved system Skill Slot.");
                }

                if (GetUsableRegularSkillChoices(state, user).Count > 0)
                {
                    throw new InvalidOperationException(
                        "Struggle can be used only when no regular Skill is available.");
                }

                return;
            }

            var slot = user.GetSkillSlot(skillSlotId);
            if (slot == null || slot.SkillId != skill.SkillId)
            {
                throw new InvalidOperationException(
                    $"Unit '{user.InstanceId}' does not own Skill Slot {skillSlotId}.");
            }

            if (!user.IsSkillReady(skillSlotId))
            {
                throw new InvalidOperationException(
                    $"Skill Slot {skillSlotId} is still on Cooldown.");
            }

            if (!user.CanSpendMn(skill.BaseManaCost))
            {
                throw new InvalidOperationException(
                    $"Unit '{user.InstanceId}' does not have enough MN "
                    + $"for Skill {skill.SkillId}.");
            }
        }

        private SkillAsset GetSkillForSlot(BattleUnitState user, int skillSlotId)
        {
            var slot = user.GetSkillSlot(skillSlotId)
                ?? throw new InvalidOperationException(
                    $"Unit '{user.InstanceId}' does not own Skill Slot {skillSlotId}.");
            return _skillCatalog.Get(slot.SkillId)
                ?? throw new InvalidOperationException($"Skill {slot.SkillId} was not found.");
        }

        private SkillResolution ResolveSkill(
            BattleState state,
            BattleUnitState user,
            SkillAsset skill)
        {
            if (!_logicRegistry.TryGet(skill.SkillId, out var logic))
            {
                throw new InvalidOperationException(
                    $"Skill {skill.SkillId} has no registered Logic.");
            }

            return BattleSkillResolver.Resolve(
                state,
                user,
                skill,
                logic);
        }

        private static void SpendMana(
            BattleState state,
            BattleUnitState user,
            SkillAsset skill)
        {
            var before = user.CurrentMn;
            if (!user.TrySpendMn(skill.BaseManaCost))
            {
                throw new InvalidOperationException(
                    $"Unit '{user.InstanceId}' could not spend "
                    + $"{skill.BaseManaCost} MN for Skill {skill.SkillId}.");
            }

            state.Presentation.RecordInitialManaSpent(
                user,
                before,
                user.CurrentMn);
        }

        private static void ValidateUser(BattleState state, BattleUnitState user)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (user == null) throw new ArgumentNullException(nameof(user));
            var side = user.Side == BattleSide.Player ? state.Player : state.Enemy;
            if (!ReferenceEquals(side.GetUnitAt(user.SlotIndex), user))
            {
                throw new ArgumentException("The Unit does not belong to this Battle.", nameof(user));
            }

            if (!user.IsAlive)
            {
                throw new InvalidOperationException("A defeated Unit cannot use a Skill.");
            }
        }
    }
}
