using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class DragonHookSkillLogic : ISkillLogic
    {
        private readonly DragonHookSkillAsset _skill;

        public DragonHookSkillLogic(DragonHookSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (!ReferenceEquals(context?.Skill, _skill))
                throw new ArgumentException("Dragon Hook Logic received another Skill.", nameof(context));
            if (_skill.CrankerStatus == null)
                throw new InvalidOperationException("Dragon Hook requires Dragon Cranker Status.");

            var target = context.Targets.GetFrontEnemy();
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

            var actualTarget = result.ActualTarget;
            if (actualTarget.IsAlive && result.FinalDamage > 0)
            {
                var dragon = context.GetAttributeValue(PachimonAttribute.Dragon);
                var crankerValue = Math.Max(
                    0,
                    _skill.BaseCrankerValue
                    + SignedStatMath.FloorStat(
                        dragon * _skill.CrankerDragonRatio / 100m,
                        clampToNonNegative: false));
                if (crankerValue > 0)
                {
                    context.State.Statuses.ApplyAttackStatus(
                        actualTarget,
                        new BattleStatusInstance(
                            BattleStatusId.DragonCranker,
                            BattleStatusCategory.None,
                            context.User,
                            crankerValue,
                            definition: _skill.CrankerStatus));
                }
            }

            return new SkillResolution(
                context.User,
                context.Skill,
                new[]
                {
                    new SkillEffectResult(
                        actualTarget,
                        result.AppliedDamage,
                        false),
                });
        }
    }
}
