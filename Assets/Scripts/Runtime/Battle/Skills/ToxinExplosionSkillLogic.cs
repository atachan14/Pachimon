using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public static class ToxinExplosionMath
    {
        public static decimal CalculateMainBaseDamage(
            ToxinExplosionSkillAsset skill,
            int consumedToxin,
            int poison,
            decimal? poisonScalingPercent = null)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            if (consumedToxin <= 0) return 0m;
            var toxinBase = consumedToxin
                * skill.ToxinConversionPercent / 100m;
            var scaledPoison = poison
                * (poisonScalingPercent ?? skill.PoisonScalingPercent) / 100m;
            return toxinBase
                * SignedStatMath.AmplificationMultiplier(scaledPoison);
        }

        public static decimal CalculateAoeBaseDamage(
            ToxinExplosionSkillAsset skill,
            decimal mainBaseDamage,
            int fire,
            decimal? fireScalingPercent = null)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            if (mainBaseDamage <= 0m) return 0m;
            var scaledFire = fire
                * (fireScalingPercent ?? skill.FireScalingPercent) / 100m;
            return mainBaseDamage
                * skill.AoeFirePercent / 100m
                * SignedStatMath.AmplificationMultiplier(scaledFire);
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

            // Consume all Toxin before damage reactions can change the field.
            var explosions = targets
                .Select(target => new ToxinExplosionSource(
                    target,
                    ConsumeToxin(context, target)))
                .Where(explosion => explosion.ConsumedToxin > 0)
                .ToArray();
            var poison = context.GetAttributeValue(PachimonAttribute.Poison);
            var fire = context.GetAttributeValue(PachimonAttribute.Fire);
            var poisonRatio = context.GetAttributeRatio(
                PachimonAttribute.Poison,
                _skill.PoisonScalingPercent);
            var fireRatio = context.GetAttributeRatio(
                PachimonAttribute.Fire,
                _skill.FireScalingPercent);
            var effects = new List<SkillEffectResult>();

            foreach (var explosion in explosions)
            {
                var mainBaseDamage = ToxinExplosionMath.CalculateMainBaseDamage(
                    _skill,
                    explosion.ConsumedToxin,
                    poison,
                    poisonRatio);
                AddDamageEffect(
                    context,
                    explosion.Target,
                    mainBaseDamage,
                    PachimonAttribute.Poison,
                    effects);

                var aoeBaseDamage = ToxinExplosionMath.CalculateAoeBaseDamage(
                    _skill,
                    mainBaseDamage,
                    fire,
                    fireRatio);
                foreach (var target in targets.Where(target => target.IsAlive))
                {
                    AddDamageEffect(
                        context,
                        target,
                        aoeBaseDamage,
                        PachimonAttribute.Fire,
                        effects);
                }
            }

            return new SkillResolution(
                context.User,
                context.Skill,
                effects);
        }

        private int ConsumeToxin(
            SkillExecutionContext context,
            BattleUnitState target)
        {
            if (!context.State.Statuses.TryConsumeStatus(
                    target,
                    BattleStatusId.Toxin,
                    out var consumedStatus))
            {
                return 0;
            }

            context.State.AddLog(
                $"{target.DisplayName}の毒素{consumedStatus.Value}を消費した！");
            return consumedStatus.Value;
        }

        private void AddDamageEffect(
            SkillExecutionContext context,
            BattleUnitState target,
            decimal baseDamage,
            PachimonAttribute attribute,
            ICollection<SkillEffectResult> effects)
        {
            if (baseDamage <= 0m || !target.IsAlive) return;
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
                    attribute,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false));
            effects.Add(new SkillEffectResult(
                result.ActualTarget,
                result.AppliedDamage,
                isTrueDamage: false));
        }

        private readonly struct ToxinExplosionSource
        {
            public ToxinExplosionSource(
                BattleUnitState target,
                int consumedToxin)
            {
                Target = target;
                ConsumedToxin = consumedToxin;
            }

            public BattleUnitState Target { get; }
            public int ConsumedToxin { get; }
        }
    }
}
