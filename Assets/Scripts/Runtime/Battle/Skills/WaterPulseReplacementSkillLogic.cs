using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class WaterPulseReplacementSkillLogic : ISkillLogic
    {
        private readonly WaterPulseReplacementSkillAsset _skill;

        public WaterPulseReplacementSkillLogic(
            WaterPulseReplacementSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var target = context.Targets.GetFrontEnemy();
            var damage = SignedStatMath.FloorNonNegative(
                context.EffectiveManaSpent
                * _skill.DamagePerMana
                * SignedStatMath.AmplificationMultiplier(
                    context.GetAttributeValue(PachimonAttribute.Aqua)
                    * context.GetAttributeRatio(
                        PachimonAttribute.Aqua,
                        _skill.AquaDamageRatio) / 100m));
            var hit = context.BeginAttackHit(target);
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
                    applyAttackerAttributeMultiplier: false),
                hit);
            return new SkillResolution(
                context.User,
                context.Skill,
                new[]
                {
                    new SkillEffectResult(
                        result.ActualTarget,
                        result.AppliedDamage,
                        false,
                        result.WasEvaded,
                        hit),
                });
        }
    }
}
