using System;
using System.Collections.Generic;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class FirstTouchSkillLogic : ISkillLogic
    {
        private readonly FirstTouchSkillAsset _skill;

        public FirstTouchSkillLogic(FirstTouchSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill))
                throw new ArgumentException("First Touch received another Skill.");
            var target = context.Targets.GetFrontEnemy()
                ?? throw new SkillTargetUnavailableException();
            var wasFullHp = target.CurrentHp == target.MaxHp;
            var poison = context.GetAttributeValue(PachimonAttribute.Poison);
            var damageRatio = context.GetAttributeRatio(
                PachimonAttribute.Poison,
                AttributeDamageRules.ScalingRatio);
            var toxinRatio = context.GetAttributeRatio(
                PachimonAttribute.Poison,
                _skill.PoisonRatio);
            int ScaleDamage(int baseValue) => SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(baseValue, poison, damageRatio));
            int ScaleToxin(int baseValue) => SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(baseValue, poison, toxinRatio));
            var effects = new List<SkillEffectResult>();

            var normalHit = context.BeginAttackHit(target);
            var normalResult = ApplyDamage(
                context,
                target,
                ScaleDamage(_skill.BaseDamage),
                effects,
                normalHit);
            if (wasFullHp && target.IsAlive)
            {
                var hit = context.BeginAttackHit(target);
                var result = ApplyDamage(
                    context,
                    target,
                    ScaleDamage(_skill.BonusBaseDamage),
                    effects,
                    hit);
                var toxinValue = ScaleToxin(_skill.BaseToxinValue);
                if (!result.WasEvaded && toxinValue > 0)
                {
                    hit.ApplyStatus(BattleStatusFactory.CreateToxin(
                        context.User,
                        toxinValue,
                        _skill.ToxinStatus));
                }
            }
            else if (!wasFullHp && target.IsAlive && !normalResult.WasEvaded)
            {
                var toxinValue = ScaleToxin(_skill.BaseNormalToxinValue);
                if (toxinValue > 0)
                {
                    normalHit.ApplyStatus(BattleStatusFactory.CreateToxin(
                        context.User,
                        toxinValue,
                        _skill.ToxinStatus));
                }
            }

            return new SkillResolution(context.User, _skill, effects);
        }

        private BattleDamageApplicationResult ApplyDamage(
            SkillExecutionContext context,
            BattleUnitState target,
            int damage,
            ICollection<SkillEffectResult> effects,
            SkillHit hit = null)
        {
            hit ??= context.BeginAttackHit(target);
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
                    PachimonAttribute.Poison,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false),
                hit);
            effects.Add(new SkillEffectResult(
                result.ActualTarget,
                result.AppliedDamage,
                isTrueDamage: false,
                hit: hit));
            return result;
        }
    }
}
