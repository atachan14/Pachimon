using System;
using System.Collections.Generic;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class IceShardSkillLogic : ISkillLogic
    {
        private readonly IceShardSkillAsset _skill;

        public IceShardSkillLogic(IceShardSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Ice Shard Logic received another Skill Asset.",
                    nameof(context));
            }
            if (_skill.ChillStatus == null)
            {
                throw new InvalidOperationException(
                    "Ice Shard requires a Chill Definition.");
            }

            var targets = context.Targets.GetAllEnemies();
            var effects = new List<SkillEffectResult>(targets.Count);
            for (var index = 0; index < targets.Count; index++)
            {
                var target = targets[index];
                var isFront = index == 0;
                var baseDamage = isFront
                    ? _skill.FrontBaseDamage
                    : _skill.OtherBaseDamage;
                var damageRatio = isFront
                    ? _skill.FrontDamageIceRatio
                    : _skill.OtherDamageIceRatio;
                var baseChill = isFront
                    ? _skill.FrontBaseChill
                    : _skill.OtherBaseChill;
                var chillRatio = isFront
                    ? _skill.FrontChillIceRatio
                    : _skill.OtherChillIceRatio;
                var damage = context.ScaleFromAttribute(
                    baseDamage,
                    PachimonAttribute.Ice,
                    damageRatio);
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
                        PachimonAttribute.Ice,
                        isAttack: true,
                        applyAttackerAttributeMultiplier: false));
                effects.Add(new SkillEffectResult(
                    damageResult.ActualTarget,
                    damageResult.AppliedDamage,
                    isTrueDamage: false));

                var chill = SignedStatMath.FloorNonNegative(
                    context.ScaleFromAttribute(
                        baseChill,
                        PachimonAttribute.Ice,
                        chillRatio));
                if (chill > 0)
                {
                    context.State.Statuses.ApplyAttackStatus(
                        damageResult.ActualTarget,
                        BattleStatusFactory.CreateSlow(
                            context.User,
                            chill,
                            _skill.ChillStatus));
                }
            }

            return new SkillResolution(context.User, context.Skill, effects);
        }
    }
}
