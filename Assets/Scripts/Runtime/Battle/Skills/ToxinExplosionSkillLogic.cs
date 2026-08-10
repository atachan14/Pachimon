using System;
using System.Linq;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public static class ToxinExplosionMath
    {
        public static decimal CalculateBaseDamage(
            ToxinExplosionSkillAsset skill,
            int consumedToxin,
            int poison,
            int fire,
            decimal? poisonScalingPercent = null,
            decimal? fireScalingPercent = null)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            var toxinDamage = consumedToxin
                * skill.ToxinConversionPercent / 100m;
            var poisonDamage = SignedStatMath.ScaleFromBase(
                skill.BasePoisonPower,
                poison,
                poisonScalingPercent ?? skill.PoisonScalingPercent);
            var fireDamage = SignedStatMath.ScaleFromBase(
                skill.BaseFirePower,
                fire,
                fireScalingPercent ?? skill.FireScalingPercent);
            return toxinDamage + poisonDamage + fireDamage;
        }
    }

    public sealed class ToxinExplosionSkillLogic : ISkillLogic
    {
        private readonly ToxinExplosionSkillAsset _skill;

        public ToxinExplosionSkillLogic(ToxinExplosionSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Toxin Explosion Logic received another Skill Asset.",
                    nameof(context));
            }

            var targets = context.Targets.GetAllEnemies().ToArray();
            if (targets.Length == 0)
            {
                throw new InvalidOperationException(
                    "No living Enemy target was found.");
            }

            var toxinTarget = targets
                .OrderByDescending(GetToxinValue)
                .ThenBy(unit => unit.SlotIndex)
                .First();
            var consumedToxin = context.State.Statuses.TryConsumeStatus(
                toxinTarget,
                BattleStatusId.Toxin,
                out var consumedStatus)
                ? consumedStatus.Value
                : 0;
            if (consumedToxin > 0)
            {
                context.State.AddLog(
                    $"{toxinTarget.DisplayName}の毒素{consumedToxin}を消費した！");
            }

            var baseDamage = ToxinExplosionMath.CalculateBaseDamage(
                _skill,
                consumedToxin,
                context.GetAttributeValue(PachimonAttribute.Poison),
                context.GetAttributeValue(PachimonAttribute.Fire),
                context.GetAttributeRatio(
                    PachimonAttribute.Poison,
                    _skill.PoisonScalingPercent),
                context.GetAttributeRatio(
                    PachimonAttribute.Fire,
                    _skill.FireScalingPercent));
            var effects = targets
                .Select(target => ResolveDamage(context, target, baseDamage))
                .ToArray();
            return new SkillResolution(
                context.User,
                context.Skill,
                effects);
        }

        private SkillEffectResult ResolveDamage(
            SkillExecutionContext context,
            BattleUnitState target,
            decimal baseDamage)
        {
            var result = BattleAttributeDamageService.Apply(
                context.State,
                context.User,
                target,
                new DamageContext(
                    DamageOriginKind.Skill,
                    _skill.SkillId,
                    baseDamage,
                    context.User.GetBattleStats(),
                    target.GetBattleStats(),
                    PachimonAttribute.Poison,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false));
            return new SkillEffectResult(
                result.ActualTarget,
                result.AppliedDamage,
                isTrueDamage: false);
        }

        private static int GetToxinValue(BattleUnitState unit)
        {
            return unit?.GetStatus(BattleStatusId.Toxin)?.Value ?? 0;
        }
    }
}
