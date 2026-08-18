using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class MuddyWaterSkillLogic : ISkillLogic
    {
        private readonly MuddyWaterSkillAsset _skill;

        public MuddyWaterSkillLogic(MuddyWaterSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var target = context.Targets.GetFrontEnemy()
                ?? throw new SkillTargetUnavailableException();
            var hit = context.BeginAttackHit(target);
            var damage = context.ScaleFromAttribute(
                _skill.BaseAquaDamage,
                PachimonAttribute.Aqua,
                _skill.AquaDamageRatio);
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
                    PachimonAttribute.Aqua,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false),
                hit);

            if (result.ActualTarget.IsAlive)
            {
                var rawSlow = context.ScaleFromAttribute(
                    _skill.BaseSlow,
                    PachimonAttribute.Poison,
                    _skill.PoisonSlowRatio);
                var modifiedSlow = context.State.Passives.ModifyOutgoingStatusValue(
                    context.State,
                    context.User,
                    result.ActualTarget,
                    _skill.SlowStatus.StatusId,
                    BattleStatusCategory.Slow,
                    rawSlow);
                var slow = SignedStatMath.FloorNonNegative(modifiedSlow);
                if (slow > 0)
                {
                    hit.ApplyStatus(BattleStatusFactory.CreateSlow(
                        context.User,
                        slow,
                        _skill.SlowStatus));
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
    }
}
