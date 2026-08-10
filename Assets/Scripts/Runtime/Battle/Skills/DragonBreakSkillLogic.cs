using System;
using Pachimon.Reward;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class DragonBreakSkillLogic : ISkillLogic
    {
        private readonly DragonBreakSkillAsset _skill;

        public DragonBreakSkillLogic(DragonBreakSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill))
                throw new ArgumentException("Dragon Break Logic received another Skill.", nameof(context));

            var target = context.Targets.GetFrontEnemy();
            var removedShield = target.RemoveAllShields();
            if (removedShield > 0)
            {
                context.State.Presentation.RecordLog(
                    $"{target.DisplayName}のShieldを全て破壊した！");
            }

            var damage = context.ScaleFromAttribute(
                _skill.BaseDragonDamage,
                PachimonAttribute.Dragon,
                _skill.DragonDamageRatio);
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
                    PachimonAttribute.Dragon,
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
                        false),
                });
        }
    }
}
