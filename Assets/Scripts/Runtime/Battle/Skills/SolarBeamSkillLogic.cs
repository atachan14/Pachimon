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
            var damage = context.ScaleFromAttribute(
                _skill.BaseLeafDamage,
                PachimonAttribute.Leaf);
            var hit = context.BeginAttackHit(target);
            var result = BattleAttributeDamageService.Apply(context.State, context.User, target,
                new DamageContext(DamageOriginKind.Skill, _skill.SkillId, damage,
                    context.User.GetBattleStats(), target.GetBattleStats(), PachimonAttribute.Leaf,
                    isAttack: true, applyAttackerAttributeMultiplier: false), hit);
            if (_skill.PollenStatus == null)
                throw new InvalidOperationException("Solar Beam requires a Pollen Definition.");
            var pollenValue = SignedStatMath.FloorNonNegative(
                context.ScaleFromAttribute(
                    _skill.PollenBaseValue,
                    PachimonAttribute.Wind,
                    _skill.PollenWindRatio));
            if (pollenValue > 0)
            {
                hit.ApplyStatus(BattleStatusFactory.CreatePollen(
                    context.User,
                    pollenValue,
                    _skill.PollenStatus));
            }
            return new SkillResolution(context.User, _skill, new[]
            {
                new SkillEffectResult(
                    result.ActualTarget,
                    result.AppliedDamage,
                    false,
                    hit: hit),
            });
        }
    }
}
