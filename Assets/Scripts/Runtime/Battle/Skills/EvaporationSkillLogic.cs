using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class EvaporationSkillLogic : ISkillLogic
    {
        private readonly EvaporationSkillAsset _skill;

        public EvaporationSkillLogic(EvaporationSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var target = context.Targets.GetFrontEnemy()
                ?? throw new SkillTargetUnavailableException();
            var hit = context.BeginAttackHit(target);
            var damage = ScalePair(
                context,
                _skill.BaseFireDamage,
                PachimonAttribute.Fire,
                _skill.FireDamageRatio,
                _skill.BaseAquaDamage,
                PachimonAttribute.Aqua,
                _skill.AquaDamageRatio);
            var penetration = ScalePair(
                context,
                _skill.BaseFirePenetration,
                PachimonAttribute.Fire,
                _skill.FirePenetrationRatio,
                _skill.BaseAquaPenetration,
                PachimonAttribute.Aqua,
                _skill.AquaPenetrationRatio);
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
                    PachimonAttribute.Fire,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false,
                    penetrationPercent: penetration),
                hit);

            if (!hit.WasEvaded && result.ActualTarget.IsAlive)
            {
                var weakness = SignedStatMath.FloorNonNegative(ScalePair(
                    context,
                    _skill.BaseFireWeakness,
                    PachimonAttribute.Fire,
                    _skill.FireWeaknessRatio,
                    _skill.BaseAquaWeakness,
                    PachimonAttribute.Aqua,
                    _skill.AquaWeaknessRatio));
                if (weakness > 0)
                {
                    hit.ApplyStatus(new BattleStatusInstance(
                        BattleStatusId.Weakness,
                        BattleStatusCategory.None,
                        context.User,
                        weakness,
                        definition: _skill.WeaknessStatus));
                }
            }

            return new SkillResolution(
                context.User,
                _skill,
                new[]
                {
                    new SkillEffectResult(
                        result.ActualTarget,
                        result.AppliedDamage,
                        isTrueDamage: false,
                        hit: hit),
                });
        }

        private static decimal ScalePair(
            SkillExecutionContext context,
            int firstBase,
            PachimonAttribute firstAttribute,
            int firstRatio,
            int secondBase,
            PachimonAttribute secondAttribute,
            int secondRatio)
        {
            return context.ScaleFromAttribute(
                    firstBase,
                    firstAttribute,
                    firstRatio)
                + context.ScaleFromAttribute(
                    secondBase,
                    secondAttribute,
                    secondRatio);
        }
    }
}
