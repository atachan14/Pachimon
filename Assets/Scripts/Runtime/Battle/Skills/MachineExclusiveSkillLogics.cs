using System;
using System.Linq;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    internal static class MachineSkillTarget
    {
        public static BattleUnitState Front(SkillExecutionContext context) =>
            context.Targets.GetFrontEnemy() ?? throw new SkillTargetUnavailableException();
    }

    public sealed class TriAttackSkillLogic : ISkillLogic
    {
        private static readonly PachimonAttribute[] AttributeOrder =
        {
            PachimonAttribute.Fire, PachimonAttribute.Aqua,
            PachimonAttribute.Leaf, PachimonAttribute.Electric,
            PachimonAttribute.Poison, PachimonAttribute.Ice,
            PachimonAttribute.Wind, PachimonAttribute.Dragon,
        };
        private readonly TriAttackSkillAsset _skill;
        public TriAttackSkillLogic(TriAttackSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var target = MachineSkillTarget.Front(context);
            var hit = context.BeginAttackHit(target);
            var total = 0;
            foreach (var attribute in AttributeOrder
                         .OrderByDescending(context.GetAttributeValue)
                         .ThenBy(attribute => Array.IndexOf(AttributeOrder, attribute))
                         .Take(3))
            {
                var damage = SignedStatMath.FloorNonNegative(
                    context.ScaleFromAttribute(
                        _skill.BaseDamage,
                        attribute,
                        _skill.AttributeRatio));
                var result = BattleAttributeDamageService.Apply(
                    context.State,
                    context.User,
                    target,
                    new DamageContext(
                        DamageOriginKind.Skill,
                        _skill.SkillId,
                        damage,
                        context.User.GetBattleStats(),
                        target.GetBattleStats(),
                        attribute,
                        isAttack: true,
                        applyAttackerAttributeMultiplier: false),
                    hit);
                total = checked(total + result.AppliedDamage);
            }

            return SingleEffect(context, hit, total, isTrueDamage: false);
        }

        private static SkillResolution SingleEffect(
            SkillExecutionContext context,
            SkillHit hit,
            int damage,
            bool isTrueDamage)
        {
            return new SkillResolution(context.User, context.Skill,
                new[] { new SkillEffectResult(hit.Target, damage, isTrueDamage, hit: hit) });
        }
    }

    public sealed class BodySlamSkillLogic : ISkillLogic
    {
        private readonly BodySlamSkillAsset _skill;
        public BodySlamSkillLogic(BodySlamSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var target = MachineSkillTarget.Front(context);
            var hit = context.BeginAttackHit(target);
            var damage = SignedStatMath.FloorNonNegative(
                context.User.CurrentHp * _skill.CurrentHpPercent / 100m);
            var result = BattleTrueDamageService.Apply(
                context.State,
                context.User,
                target,
                new TrueDamageContext(
                    DamageOriginKind.Skill,
                    _skill.SkillId,
                    damage,
                    isAttack: true),
                hit);
            return TrueDamageResolution(context, hit, result.AppliedDamage);
        }

        internal static SkillResolution TrueDamageResolution(
            SkillExecutionContext context,
            SkillHit hit,
            int damage)
        {
            return new SkillResolution(context.User, context.Skill,
                new[] { new SkillEffectResult(hit.Target, damage, true, hit: hit) });
        }
    }

    public sealed class FakeOutSkillLogic : ISkillLogic
    {
        private readonly FakeOutSkillAsset _skill;
        public FakeOutSkillLogic(FakeOutSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context.SkillSlotId <= 0
                || !context.User.TryUseOncePerBattleSkill(context.SkillSlotId))
            {
                throw new InvalidOperationException(
                    "Fake Out requires an unused regular Skill Slot.");
            }

            var target = MachineSkillTarget.Front(context);
            var hit = context.BeginAttackHit(target);
            var result = BattleTrueDamageService.Apply(
                context.State,
                context.User,
                target,
                new TrueDamageContext(
                    DamageOriginKind.Skill,
                    _skill.SkillId,
                    _skill.TrueDamage,
                    isAttack: true),
                hit);
            if (target.IsAlive)
            {
                hit.ApplyStatus(BattleStatusFactory.CreateStun(
                    context.User,
                    _skill.StunTicks,
                    _skill.StunStatus));
            }
            return BodySlamSkillLogic.TrueDamageResolution(
                context,
                hit,
                result.AppliedDamage);
        }
    }

    public sealed class DestructionBeamSkillLogic : ISkillLogic
    {
        private readonly DestructionBeamSkillAsset _skill;
        public DestructionBeamSkillLogic(DestructionBeamSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var target = MachineSkillTarget.Front(context);
            var hit = context.BeginAttackHit(target);
            var damage = SignedStatMath.FloorNonNegative(
                target.MaxHp * _skill.MaxHpPercent / 100m);
            var result = BattleTrueDamageService.Apply(
                context.State,
                context.User,
                target,
                new TrueDamageContext(
                    DamageOriginKind.Skill,
                    _skill.SkillId,
                    damage,
                    isAttack: true),
                hit);
            return BodySlamSkillLogic.TrueDamageResolution(
                context,
                hit,
                result.AppliedDamage);
        }
    }
}
