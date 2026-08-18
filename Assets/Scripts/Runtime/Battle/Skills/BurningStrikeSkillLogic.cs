using System;
using System.Collections.Generic;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class BurningStrikeSkillLogic : ISkillLogic
    {
        private readonly BurningStrikeSkillAsset _skill;

        public BurningStrikeSkillLogic(BurningStrikeSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var effects = new List<SkillEffectResult>();
            var selfHit = context.BeginAttackHit(context.User);
            var selfDamage = ApplyFireDamage(
                context,
                context.User,
                _skill.SelfBaseDamage,
                _skill.SelfFireRatio,
                selfHit);
            effects.Add(new SkillEffectResult(
                selfDamage.ActualTarget,
                selfDamage.AppliedDamage,
                false,
                selfDamage.WasEvaded,
                selfHit));
            if (!context.User.IsAlive)
                return new SkillResolution(context.User, context.Skill, effects);

            var target = context.Targets.GetFrontEnemy();
            var enemyHit = context.BeginAttackHit(target);
            var enemyDamage = ApplyFireDamage(
                context,
                target,
                _skill.EnemyBaseDamage,
                _skill.EnemyFireRatio,
                enemyHit);
            effects.Add(new SkillEffectResult(
                enemyDamage.ActualTarget,
                enemyDamage.AppliedDamage,
                false,
                enemyDamage.WasEvaded,
                enemyHit));
            var burnValue = SignedStatMath.FloorNonNegative(
                context.ScaleFromAttribute(
                    _skill.BaseBurnValue,
                    PachimonAttribute.Fire,
                    _skill.BurnFireRatio));
            if (burnValue > 0)
            {
                enemyHit.ApplyStatus(BattleStatusFactory.CreateBurn(
                    context.User,
                    burnValue,
                    _skill.BurnStatus));
            }
            return new SkillResolution(context.User, context.Skill, effects);
        }

        private BattleDamageApplicationResult ApplyFireDamage(
            SkillExecutionContext context,
            BattleUnitState target,
            int baseDamage,
            int ratio,
            SkillHit hit)
        {
            var damage = context.ScaleFromAttribute(
                baseDamage,
                PachimonAttribute.Fire,
                ratio);
            return BattleAttributeDamageService.Apply(
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
                    applyAttackerAttributeMultiplier: false),
                hit);
        }
    }
}
