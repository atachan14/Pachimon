using System;
using System.Linq;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class FrostArrowSkillLogic : ISkillLogic
    {
        private readonly FrostArrowSkillAsset _skill;
        public FrostArrowSkillLogic(FrostArrowSkillAsset skill) =>
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var target = context.State.GetOpposingSide(context.User.Side)
                .GetAllLiving()
                .OrderBy(unit => unit.CurrentHp).ThenBy(unit => unit.SlotIndex)
                .FirstOrDefault() ?? throw new SkillTargetUnavailableException();
            int Scale(int value) => SignedStatMath.FloorNonNegative(
                context.ScaleFromAttribute(value, PachimonAttribute.Ice,
                    _skill.IceRatio));
            var hit = context.BeginAttackHit(target);
            var result = BattleAttributeDamageService.Apply(context.State,
                context.User, target, new DamageContext(DamageOriginKind.Skill,
                    _skill.SkillId, Scale(_skill.BaseDamage),
                    context.User.GetBattleStats(), target.GetBattleStats(),
                    PachimonAttribute.Ice, true,
                    applyAttackerAttributeMultiplier: false), hit);
            var chill = Scale(_skill.BaseChill);
            if (chill > 0)
                hit.ApplyStatus(BattleStatusFactory.CreateSlow(context.User,
                    chill, _skill.ChillStatus));
            var resolution = new SkillResolution(context.User, _skill, new[]
            {
                new SkillEffectResult(result.ActualTarget,
                    result.AppliedDamage, false, hit: hit),
            });
            if (target.IsDefeated)
            {
                context.RefundSpentMana();
                resolution = resolution.WithCooldownRefund();
            }
            return resolution;
        }
    }
}
