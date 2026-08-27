using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class WaterCutterSkillLogic : ISkillLogic
    {
        private readonly WaterCutterSkillAsset _skill;

        public WaterCutterSkillLogic(WaterCutterSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var target = context.Targets.GetFrontEnemy()
                ?? throw new SkillTargetUnavailableException();
            var aqua = context.User.GetBattleStatValue(PachimonStatType.Aqua);
            var wind = context.User.GetBattleStatValue(PachimonStatType.Wind);
            var damage = SignedStatMath.ScaleFromBase(
                _skill.BaseAquaDamage,
                aqua,
                context.GetAttributeRatio(
                    PachimonAttribute.Aqua,
                    _skill.AquaDamageRatio));
            var penetrationValue = wind
                * context.GetAttributeRatio(
                    PachimonAttribute.Wind,
                    100m)
                / 100m
                * _skill.WindPenetrationRatio
                / 100m;
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
                    PachimonAttribute.Aqua,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false,
                    penetration: new DamagePenetration(
                        attributePercentage:
                            PenetrationMath.CalculateDiminishingPercentage(
                                penetrationValue))));
            return new SkillResolution(
                context.User,
                _skill,
                new[]
                {
                    new SkillEffectResult(
                        result.ActualTarget,
                        result.AppliedDamage,
                        isTrueDamage: false,
                        hit: result.Hit),
                });
        }
    }
}
