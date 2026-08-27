using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Run;
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
            BattleSkillTimingPlan timing,
            SkillStatusConsumptionSnapshot statusConsumption,
            object runtimeData,
            int resolutionCount)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            SkillSlotId = skillSlotId;
            StartupTicks = startupTicks;
            Timing = timing;
            StatusConsumption = statusConsumption;
            RuntimeData = runtimeData;
            ResolutionCount = resolutionCount;
        }

        public BattleUnitState User { get; }
        public SkillAsset Skill { get; }
        public int SkillSlotId { get; }
        public int StartupTicks { get; }
        public BattleSkillTimingPlan Timing { get; }
        public SkillStatusConsumptionSnapshot StatusConsumption { get; }
        public object RuntimeData { get; }
        public int ResolutionCount { get; }
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
                    var mana = BattleSkillManaCostCalculator.CreatePlan(
                        user,
                        skill,
                        slot.UpgradeLevel);
                    var hasEnoughMn = skill.ConsumesAllCurrentMana
                        ? mana.Actual > 0
                        : user.CanSpendMn(mana.Actual);
                    var isUsable = _logicRegistry.TryGet(slot.SkillId, out _)
                        && isCooldownReady
                        && hasEnoughMn
                        && (skill is not FakeOutSkillAsset
                            || !user.HasUsedOncePerBattleSkill(slot.SlotId));
                    return new BattleSkillChoice(
                        slot.SlotId,
                        skill,
                        slot.UpgradeLevel,
                        mana.Actual,
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
            if (!_logicRegistry.TryGet(skill.SkillId, out var logic))
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
            return CreateTimingPlan(state, user, skill, skillSlotId).StartupWork > 0m;
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
            var timing = CreateTimingPlan(state, user, skill, skillSlotId);
            if (timing.StartupWork > 0m)
            {
                throw new InvalidOperationException(
                    $"Skill {skill.SkillId} requires Startup before it can resolve.");
            }

            state.Presentation.Begin(user, skill);
            try
            {
                var statusConsumption = state.Statuses
                    .CaptureSkillStatusConsumption(user);
                var manaSpent = SpendMana(state, user, skill, skillSlotId);
                var resolutionCount = ConsumeResolutionCount(user);
                var resolution = ResolveSkillRepeated(
                    state,
                    user,
                    skill,
                    resolutionCount,
                    skillSlotId: skillSlotId,
                    actualManaSpent: manaSpent.Actual,
                    effectiveManaSpent: manaSpent.Effective);
                var continueTurn = state.Passives.ShouldContinueTurn(
                    state,
                    resolution);
                state.Statuses.CompleteSkillStatusConsumption(
                    user,
                    statusConsumption);
                resolution = resolution.WithPresentation(
                    state.Presentation.Complete());
                state.Timeline.CompleteImmediateAction(
                    user,
                    skillSlotId,
                    timing,
                    continueTurn,
                    GetDisplayName(user, skill, skillSlotId));
                if (resolution.RefundCooldown && skillSlotId > 0)
                    user.ClearCooldown(skillSlotId);
                state.Statuses.RefreshAllActionClockPauses();
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
            var timing = CreateTimingPlan(state, user, skill, skillSlotId);
            if (timing.StartupWork <= 0m)
            {
                throw new InvalidOperationException(
                    $"Skill {skill.SkillId} does not require Startup.");
            }

            if (!_logicRegistry.TryGet(skill.SkillId, out var logic))
            {
                throw new InvalidOperationException(
                    $"Skill {skill.SkillId} has no registered Logic.");
            }

            var statusConsumption = state.Statuses
                .CaptureSkillStatusConsumption(user);
            var resolutionCount = ConsumeResolutionCount(user);
            var runtimeData = logic is IStartupSkillLogic startupLogic
                ? startupLogic.BeginStartup(
                    new SkillExecutionContext(
                        state,
                        user,
                        skill,
                        skillSlotId: skillSlotId))
                : null;
            var startupTicks = state.Timeline.BeginStartup(
                user,
                skillSlotId,
                timing,
                GetDisplayName(user, skill, skillSlotId));
            return new PendingSkillAction(
                user,
                skill,
                skillSlotId,
                startupTicks,
                timing,
                statusConsumption,
                runtimeData,
                resolutionCount);
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
                var manaSpent = SpendMana(
                    state,
                    action.User,
                    action.Skill,
                    action.SkillSlotId);
                var resolution = ResolveSkillRepeated(
                    state,
                    action.User,
                    action.Skill,
                    action.ResolutionCount,
                    action.RuntimeData,
                    manaSpent.Actual,
                    manaSpent.Effective,
                    action.SkillSlotId);
                var continueTurn = state.Passives.ShouldContinueTurn(
                    state,
                    resolution);
                state.Statuses.CompleteSkillStatusConsumption(
                    action.User,
                    action.StatusConsumption);
                resolution = resolution.WithPresentation(
                    state.Presentation.Complete());
                state.Timeline.CompleteDelayedAction(
                    action.User,
                    action.Timing,
                    continueTurn);
                if (resolution.RefundCooldown && action.SkillSlotId > 0)
                    action.User.ClearCooldown(action.SkillSlotId);
                state.Statuses.RefreshAllActionClockPauses();
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
                spendMana: true,
                skillSlotId: skillSlotId,
                upgradeLevel: GetUpgradeLevel(user, skillSlotId),
                resolutionCount: GetResolutionCount(user));
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

            if (skill is FakeOutSkillAsset
                && user.HasUsedOncePerBattleSkill(skillSlotId))
            {
                throw new InvalidOperationException(
                    $"Skill Slot {skillSlotId} can be used only once per Battle.");
            }

            var mana = BattleSkillManaCostCalculator.CreatePlan(
                user,
                skill,
                slot.UpgradeLevel);
            if (skill.ConsumesAllCurrentMana
                ? mana.Actual <= 0
                : !user.CanSpendMn(mana.Actual))
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
            SkillAsset skill,
            object runtimeData = null,
            int actualManaSpent = 0,
            decimal effectiveManaSpent = 0m,
            int skillSlotId = 0)
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
                logic,
                runtimeData,
                actualManaSpent,
                effectiveManaSpent,
                skillSlotId);
        }

        private SkillResolution ResolveSkillRepeated(
            BattleState state,
            BattleUnitState user,
            SkillAsset skill,
            int resolutionCount,
            object runtimeData = null,
            int actualManaSpent = 0,
            decimal effectiveManaSpent = 0m,
            int skillSlotId = 0)
        {
            if (resolutionCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(resolutionCount));

            var effects = new List<SkillEffectResult>();
            var allTargetUnavailable = true;
            var refundCooldown = false;
            for (var index = 0; index < resolutionCount; index++)
            {
                if (index > 0)
                    state.Presentation.BeginNextBlock();
                var current = ResolveSkill(
                    state,
                    user,
                    skill,
                    runtimeData,
                    actualManaSpent: index == 0 ? actualManaSpent : 0,
                    effectiveManaSpent: effectiveManaSpent,
                    skillSlotId: skillSlotId);
                effects.AddRange(current.Effects);
                allTargetUnavailable &= current.WasTargetUnavailable;
                refundCooldown |= current.RefundCooldown;
            }

            var combined = new SkillResolution(
                user,
                skill,
                effects,
                wasTargetUnavailable: allTargetUnavailable,
                actualManaSpent: actualManaSpent,
                effectiveManaSpent: effectiveManaSpent);
            return refundCooldown ? combined.WithCooldownRefund() : combined;
        }

        private static int ConsumeResolutionCount(BattleUnitState user)
        {
            return user.TryConsumeStatus(BattleStatusId.Clone, out var clone)
                ? checked(1 + clone.StackCount)
                : 1;
        }

        private static int GetResolutionCount(BattleUnitState user)
        {
            return checked(1 + (user.GetStatus(BattleStatusId.Clone)?.StackCount ?? 0));
        }

        private static BattleSkillManaSpendPlan SpendMana(
            BattleState state,
            BattleUnitState user,
            SkillAsset skill,
            int skillSlotId)
        {
            var before = user.CurrentMn;
            var plan = BattleSkillManaCostCalculator.CreatePlan(
                state,
                user,
                skill,
                GetUpgradeLevel(user, skillSlotId));
            var amount = plan.Actual;
            if (amount <= 0 && skill.ConsumesAllCurrentMana)
            {
                throw new InvalidOperationException(
                    $"Unit '{user.InstanceId}' requires positive MN for Skill {skill.SkillId}.");
            }
            if (!user.TrySpendMn(amount))
            {
                throw new InvalidOperationException(
                    $"Unit '{user.InstanceId}' could not spend "
                    + $"{amount} MN for Skill {skill.SkillId}.");
            }

            state.Presentation.RecordInitialManaSpent(
                user,
                before,
                user.CurrentMn);
            if (amount > 0)
                state.Events.Publish(new MnSpentEvent(state, user, amount));
            return plan;
        }

        private static BattleSkillTimingPlan CreateTimingPlan(
            BattleState state,
            BattleUnitState user,
            SkillAsset skill,
            int skillSlotId)
        {
            return SkillTimingCalculator.CreatePlan(
                skill,
                user,
                state,
                GetUpgradeLevel(user, skillSlotId));
        }

        private static int GetUpgradeLevel(
            BattleUnitState user,
            int skillSlotId)
        {
            return skillSlotId == 0
                ? 0
                : user.GetSkillSlot(skillSlotId)?.UpgradeLevel ?? 0;
        }

        private static string GetDisplayName(
            BattleUnitState user,
            SkillAsset skill,
            int skillSlotId)
        {
            return SkillUpgradeMath.FormatDisplayName(
                skill.DisplayName,
                GetUpgradeLevel(user, skillSlotId));
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
