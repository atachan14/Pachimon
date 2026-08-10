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
            BattleSkillTimingPlan timing,
            SkillStatusConsumptionSnapshot statusConsumption,
            int actualManaSpent,
            decimal effectiveManaSpent,
            object runtimeData)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            SkillSlotId = skillSlotId;
            StartupTicks = startupTicks;
            Timing = timing;
            StatusConsumption = statusConsumption;
            ActualManaSpent = actualManaSpent;
            EffectiveManaSpent = effectiveManaSpent;
            RuntimeData = runtimeData;
        }

        public BattleUnitState User { get; }
        public SkillAsset Skill { get; }
        public int SkillSlotId { get; }
        public int StartupTicks { get; }
        public BattleSkillTimingPlan Timing { get; }
        public SkillStatusConsumptionSnapshot StatusConsumption { get; }
        public int ActualManaSpent { get; }
        public decimal EffectiveManaSpent { get; }
        public object RuntimeData { get; }
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
                        skill);
                    var hasEnoughMn = skill.ConsumesAllCurrentMana
                        ? mana.Actual > 0
                        : user.CanSpendMn(mana.Actual);
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
                var statusConsumption = state.Statuses
                    .CaptureSkillStatusConsumption(user);
                var manaSpent = SpendMana(state, user, skill);
                var timing = SkillTimingCalculator.CreatePlan(
                    skill,
                    user,
                    state);
                var resolution = ResolveSkill(
                    state,
                    user,
                    skill,
                    actualManaSpent: manaSpent.Actual,
                    effectiveManaSpent: manaSpent.Effective);
                state.Statuses.CompleteSkillStatusConsumption(
                    user,
                    statusConsumption);
                resolution = resolution.WithPresentation(
                    state.Presentation.Complete());
                state.Timeline.CompleteImmediateAction(
                    user,
                    skillSlotId,
                    timing);
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
            if (skill.BaseStartupTicks <= 0)
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
            var manaSpent = SpendMana(state, user, skill);
            var timing = SkillTimingCalculator.CreatePlan(
                skill,
                user,
                state);
            var runtimeData = logic is IStartupSkillLogic startupLogic
                ? startupLogic.BeginStartup(
                    new SkillExecutionContext(state, user, skill))
                : null;
            var startupTicks = state.Timeline.BeginStartup(
                user,
                skillSlotId,
                timing);
            return new PendingSkillAction(
                user,
                skill,
                skillSlotId,
                startupTicks,
                timing,
                statusConsumption,
                manaSpent.Actual,
                manaSpent.Effective,
                runtimeData);
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
                var resolution = ResolveSkill(
                    state,
                    action.User,
                    action.Skill,
                    action.RuntimeData,
                    action.ActualManaSpent,
                    action.EffectiveManaSpent);
                state.Statuses.CompleteSkillStatusConsumption(
                    action.User,
                    action.StatusConsumption);
                resolution = resolution.WithPresentation(
                    state.Presentation.Complete());
                state.Timeline.CompleteDelayedAction(
                    action.User,
                    action.Timing);
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

            var mana = BattleSkillManaCostCalculator.CreatePlan(user, skill);
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
            decimal effectiveManaSpent = 0m)
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
                effectiveManaSpent);
        }

        private static BattleSkillManaSpendPlan SpendMana(
            BattleState state,
            BattleUnitState user,
            SkillAsset skill)
        {
            var before = user.CurrentMn;
            var plan = BattleSkillManaCostCalculator.CreatePlan(user, skill);
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
            return plan;
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
