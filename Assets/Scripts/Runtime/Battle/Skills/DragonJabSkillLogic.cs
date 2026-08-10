using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class DragonJabSkillLogic : ISkillLogic
    {
        private readonly DragonJabSkillAsset _skill;

        public DragonJabSkillLogic(DragonJabSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Dragon Jab Logic received another Skill Asset.",
                    nameof(context));
            }
            if (_skill.OneTwoStatus == null)
            {
                throw new InvalidOperationException(
                    "Dragon Jab requires a One Two Definition.");
            }

            var target = context.Targets.GetFrontEnemy();
            var damage = context.ScaleFromAttribute(
                _skill.BaseDragonDamage,
                PachimonAttribute.Dragon,
                _skill.DragonDamageRatio);
            var damageResult = BattleAttributeDamageService.Apply(
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

            if (_skill.OneTwoValue > 0)
            {
                context.State.Statuses.ApplyStatus(
                    context.User,
                    new BattleStatusInstance(
                        BattleStatusId.OneTwo,
                        BattleStatusCategory.None,
                        context.User,
                        _skill.OneTwoValue,
                        definition: _skill.OneTwoStatus));
            }

            return new SkillResolution(
                context.User,
                context.Skill,
                new[]
                {
                    new SkillEffectResult(
                        damageResult.ActualTarget,
                        damageResult.AppliedDamage,
                        isTrueDamage: false),
                });
        }
    }
}
