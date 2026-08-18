using System;
using System.Collections.Generic;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class KachofugetsuSkillLogic : ISkillLogic
    {
        private readonly KachofugetsuSkillAsset _skill;
        public KachofugetsuSkillLogic(KachofugetsuSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var target = context.Targets.GetFrontEnemy()
                ?? throw new SkillTargetUnavailableException();
            var hit = context.BeginAttackHit(target);
            var totalDamage = 0;
            foreach (var component in Components())
            {
                var damage = context.ScaleFromAttribute(component.BaseDamage,
                    component.Attribute, component.Ratio);
                var result = BattleAttributeDamageService.Apply(context.State,
                    context.User, target, new DamageContext(
                        DamageOriginKind.Skill, _skill.SkillId, damage,
                        context.User.GetBattleStats(), target.GetBattleStats(),
                        component.Attribute, true,
                        applyAttackerAttributeMultiplier: false), hit);
                totalDamage = checked(totalDamage + result.AppliedDamage);
            }
            return new SkillResolution(context.User, _skill, new[]
            {
                new SkillEffectResult(hit.Target, totalDamage, false, hit: hit),
            });
        }

        private IEnumerable<(PachimonAttribute Attribute, int BaseDamage,
            int Ratio)> Components()
        {
            yield return (PachimonAttribute.Fire, _skill.BaseFireDamage,
                _skill.FireDamageRatio);
            yield return (PachimonAttribute.Aqua, _skill.BaseAquaDamage,
                _skill.AquaDamageRatio);
            yield return (PachimonAttribute.Wind, _skill.BaseWindDamage,
                _skill.WindDamageRatio);
        }
    }
}
