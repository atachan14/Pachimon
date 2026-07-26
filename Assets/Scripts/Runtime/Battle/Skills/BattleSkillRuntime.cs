using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class BattleSkillRuntime
    {
        private readonly SkillCatalog _skillCatalog;
        private readonly SkillLogicRegistry _logicRegistry;

        public BattleSkillRuntime(
            SkillCatalog skillCatalog,
            SkillLogicRegistry logicRegistry = null)
        {
            _skillCatalog = skillCatalog ?? throw new ArgumentNullException(nameof(skillCatalog));
            _logicRegistry = logicRegistry ?? new SkillLogicRegistry(skillCatalog);
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
                    var readyTick = user.GetCooldownReadyTick(slot.SlotId);
                    var isUsable = _logicRegistry.TryGet(slot.SkillId, out _)
                        && state.CurrentTick >= readyTick;
                    return new BattleSkillChoice(
                        slot.SlotId,
                        skill,
                        isUsable,
                        readyTick);
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
            if (!_logicRegistry.TryGet(skill.SkillId, out var logic))
            {
                throw new InvalidOperationException(
                    $"Skill {skill.SkillId} has no registered Logic.");
            }

            state.Events.Publish(new BeforeSkillEvent(state, user, skill));
            var resolution = logic.Resolve(new SkillExecutionContext(state, user, skill));
            state.Events.Publish(new SkillResolvedEvent(state, resolution));
            foreach (var defeatedUnit in resolution.Effects
                         .Where(effect => effect.Damage > 0 && effect.Target.IsDefeated)
                         .Select(effect => effect.Target)
                         .Distinct())
            {
                state.Events.Publish(new UnitDefeatedEvent(state, user, defeatedUnit));
            }

            state.Timeline.CompleteTurn(
                user,
                skillSlotId,
                skill.BaseTurnCostTicks,
                skill.BaseCooldownTicks);
            return resolution;
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

            return logic.Preview(new SkillExecutionContext(state, user, skill));
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

            if (!user.IsSkillReady(skillSlotId, state.CurrentTick))
            {
                throw new InvalidOperationException(
                    $"Skill Slot {skillSlotId} is still on Cooldown.");
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
