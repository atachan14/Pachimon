using System;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class WaterSpoutSkillLogic : ISkillLogic
    {
        private readonly WaterSpoutSkillAsset _skill;

        public WaterSpoutSkillLogic(WaterSpoutSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            var target = context.Targets.GetFrontEnemy()
                ?? throw new SkillTargetUnavailableException();
            var aqua = context.User.GetBattleStatValue(PachimonStatType.Aqua);
            var aquaMultiplier = SignedStatMath.AmplificationMultiplier(
                aqua * context.GetAttributeRatio(
                    PachimonAttribute.Aqua,
                    _skill.AquaDamageRatio) / 100m);
            var hpMultiplier = (decimal)context.User.CurrentHp
                / _skill.CurrentHpDivisor;
            var damage = _skill.BaseAquaDamage
                * (aquaMultiplier + hpMultiplier);
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
                    applyAttackerAttributeMultiplier: false));
            return new SkillResolution(
                context.User,
                _skill,
                new[]
                {
                    new SkillEffectResult(
                        result.ActualTarget,
                        result.AppliedDamage,
                        isTrueDamage: false,
                        hit: result.Hit),
                });
        }
    }
}
