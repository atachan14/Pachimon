using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class WaterPulseSkillLogic : ISkillLogic
    {
        private readonly WaterPulseSkillAsset _skill;

        public WaterPulseSkillLogic(WaterPulseSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var target = context.Targets.GetFrontEnemy();
            var baseDamage = SignedStatMath.FloorNonNegative(
                context.EffectiveManaSpent
                * SignedStatMath.AmplificationMultiplier(
                    context.GetAttributeValue(PachimonAttribute.Aqua)
                    * context.GetAttributeRatio(
                        PachimonAttribute.Aqua,
                        _skill.AquaDamageRatio) / 100m),
                minimum: 1);
            var result = BattleAttributeDamageService.Apply(
                context.State,
                context.User,
                target,
                new DamageContext(
                    DamageOriginKind.Skill,
                    _skill.SkillId,
                    baseDamage,
                    context.User.GetBattleStats(),
                    target.GetBattleStats(),
                    PachimonAttribute.Aqua,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false));
            return new SkillResolution(
                context.User,
                context.Skill,
                new[]
                {
                    new SkillEffectResult(
                        result.ActualTarget,
                        result.AppliedDamage,
                        isTrueDamage: false),
                });
        }
    }
}
