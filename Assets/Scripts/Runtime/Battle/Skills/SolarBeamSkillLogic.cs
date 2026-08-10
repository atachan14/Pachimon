using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class SolarBeamSkillLogic : ISkillLogic
    {
        private readonly SolarBeamSkillAsset _skill;
        public SolarBeamSkillLogic(SolarBeamSkillAsset skill) => _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill)) throw new ArgumentException("Solar Beam Logic received another Skill.", nameof(context));
            var target = context.Targets.GetFrontEnemy();
            if (target == null) return new SkillResolution(context.User, _skill, Array.Empty<SkillEffectResult>(), wasTargetUnavailable: true);
            var damage = context.ScaleFromAttribute(_skill.BaseLeafDamage, PachimonAttribute.Leaf, _skill.LeafDamageRatio);
            var result = BattleAttributeDamageService.Apply(context.State, context.User, target,
                new DamageContext(DamageOriginKind.Skill, _skill.SkillId, damage,
                    context.User.GetBattleStats(), target.GetBattleStats(), PachimonAttribute.Leaf,
                    isAttack: true, applyAttackerAttributeMultiplier: false));
            return new SkillResolution(context.User, _skill, new[] { new SkillEffectResult(result.ActualTarget, result.AppliedDamage, false) });
        }
    }
}
