using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class FrozenBreakSkillLogic : ISkillLogic
    {
        private readonly FrozenBreakSkillAsset _skill;

        public FrozenBreakSkillLogic(FrozenBreakSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Frozen Break Logic received another Skill Asset.",
                    nameof(context));
            }

            return context.User.CurrentHp * 2 >= context.User.MaxHp
                ? ResolveAttack(context)
                : ResolveRecovery(context);
        }

        private SkillResolution ResolveAttack(SkillExecutionContext context)
        {
            var target = context.Targets.GetFrontEnemy();
            var hit = context.BeginAttackHit(target);
            var damage = context.ScaleFromAttribute(
                _skill.BaseIceDamage,
                PachimonAttribute.Ice,
                _skill.IceDamageRatio);
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
                    PachimonAttribute.Ice,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false),
                hit);
            var duration = CalculateDuration(context);
            hit.ApplyStatus(
                new BattleStatusInstance(
                    BattleStatusId.Freeze,
                    BattleStatusCategory.Stun,
                    context.User,
                    duration,
                    durationTicks: duration,
                    definition: _skill.FreezeStatus));
            return new SkillResolution(
                context.User,
                context.Skill,
                new[]
                {
                    new SkillEffectResult(
                        result.ActualTarget,
                        result.AppliedDamage,
                        isTrueDamage: false,
                        hit: hit),
                });
        }

        private SkillResolution ResolveRecovery(SkillExecutionContext context)
        {
            var duration = CalculateDuration(context);
            var healPerTick = context.ScaleFromAttribute(
                _skill.BaseHealPerTick,
                PachimonAttribute.Ice,
                _skill.HealIceRatio);
            context.State.Statuses.ApplyStatus(
                context.User,
                new BattleStatusInstance(
                    BattleStatusId.FrozenBreakSelf,
                    BattleStatusCategory.Stun
                        | BattleStatusCategory.Untargetable,
                    context.User,
                    value: 0,
                    durationTicks: duration,
                    runtimeData: new FrozenBreakRuntimeState(
                        duration,
                        healPerTick),
                    definition: _skill.SelfStatus));
            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }

        private int CalculateDuration(SkillExecutionContext context)
        {
            return Math.Max(
                1,
                SignedStatMath.FloorNonNegative(
                    _skill.BaseDuration
                    + context.GetAttributeValue(PachimonAttribute.Ice)
                    * _skill.DurationIceRatio / 100m));
        }
    }
}
