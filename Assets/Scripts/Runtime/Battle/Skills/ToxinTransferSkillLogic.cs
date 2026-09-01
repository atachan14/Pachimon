using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public readonly struct ToxinTransferTargets
    {
        public ToxinTransferTargets(
            BattleUnitState source,
            IReadOnlyList<BattleUnitState> destinations)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Destinations = destinations
                ?? throw new ArgumentNullException(nameof(destinations));
            if (Destinations.Count == 0)
            {
                throw new ArgumentException(
                    "At least one destination is required.",
                    nameof(destinations));
            }
        }

        public BattleUnitState Source { get; }
        public IReadOnlyList<BattleUnitState> Destinations { get; }
    }

    public static class ToxinTransferMath
    {
        public static int CalculateRemoval(
            int toxinValue,
            int removalPercent)
        {
            return SignedStatMath.FloorNonNegative(
                toxinValue * removalPercent / 100m);
        }

        public static int CalculateApplication(
            int removedValue,
            int baseValue,
            int applicationPercent)
        {
            return SignedStatMath.FloorNonNegative(
                (removedValue + baseValue) * applicationPercent / 100m);
        }

        public static int CalculateApplicationPercent(
            ToxinTransferSkillAsset skill,
            decimal poison,
            decimal? poisonScalingPercent = null)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return SignedStatMath.FloorNonNegative(
                skill.BaseApplicationPercent
                + SignedStatMath.ScaleFromBase(
                    skill.ScaledApplicationBasePercent,
                    poison,
                    poisonScalingPercent
                    ?? skill.ApplicationPoisonScalingPercent));
        }

        public static int CalculateBaseValue(
            ToxinTransferSkillAsset skill,
            decimal poison,
            decimal? poisonScalingPercent = null)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    skill.BaseToxinValue,
                    poison,
                    poisonScalingPercent ?? skill.PoisonScalingPercent));
        }
    }

    public sealed class ToxinTransferSkillLogic : ISkillLogic
    {
        private readonly ToxinTransferSkillAsset _skill;

        public ToxinTransferSkillLogic(ToxinTransferSkillAsset skill)
        {
            _skill = skill ?? throw new ArgumentNullException(nameof(skill));
        }

        public SkillResolution Resolve(SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!ReferenceEquals(context.Skill, _skill))
            {
                throw new ArgumentException(
                    "Toxin Transfer Logic received another Skill Asset.",
                    nameof(context));
            }

            var targets = SelectTargets(context);
            var sourceValue = GetToxinValue(targets.Source);
            var baseValue = ToxinTransferMath.CalculateBaseValue(
                _skill,
                context.GetAttributeValue(PachimonAttribute.Poison),
                context.GetAttributeRatio(
                    PachimonAttribute.Poison,
                    _skill.PoisonScalingPercent));
            if (sourceValue <= 0)
            {
                ApplyToxin(context, targets.Source, baseValue);
                return new SkillResolution(
                    context.User,
                    context.Skill,
                    Array.Empty<SkillEffectResult>());
            }

            var requestedRemoval = ToxinTransferMath.CalculateRemoval(
                sourceValue,
                _skill.RemovalPercent);
            var removed = context.State.Statuses.ReduceStatusValue(
                targets.Source,
                BattleStatusId.Toxin,
                requestedRemoval);
            var applicationPercent = ToxinTransferMath.CalculateApplicationPercent(
                _skill,
                context.GetAttributeValue(PachimonAttribute.Poison),
                context.GetAttributeRatio(
                    PachimonAttribute.Poison,
                    _skill.ApplicationPoisonScalingPercent));
            var applied = ToxinTransferMath.CalculateApplication(
                removed,
                baseValue,
                applicationPercent);

            if (removed > 0)
            {
                context.State.AddLog(
                    $"{targets.Source.DisplayName}から{removed}の毒素を取り除いた！");
            }

            if (applied > 0)
            {
                ApplyDistributedToxin(context, targets.Destinations, applied);
            }

            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
        }

        private void ApplyDistributedToxin(
            SkillExecutionContext context,
            IReadOnlyList<BattleUnitState> targets,
            int totalValue)
        {
            var valuePerTarget = totalValue / targets.Count;
            var remainder = totalValue % targets.Count;
            for (var index = 0; index < targets.Count; index++)
            {
                ApplyToxin(
                    context,
                    targets[index],
                    valuePerTarget + (index < remainder ? 1 : 0));
            }
        }

        private void ApplyToxin(
            SkillExecutionContext context,
            BattleUnitState target,
            int value)
        {
            if (value <= 0) return;
            context.BeginStatusHit(target).ApplyStatus(
                BattleStatusFactory.CreateToxin(
                    context.User,
                    value,
                    _skill.ToxinStatus ?? throw new InvalidOperationException(
                        "Toxin Transfer requires a Toxin Definition.")));
        }

        public static ToxinTransferTargets SelectTargets(
            SkillExecutionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var living = context.Targets.GetAllEnemies()
                .OrderBy(unit => unit.SlotIndex)
                .ToArray();
            if (living.Length == 0)
            {
                throw new InvalidOperationException(
                    "No living Enemy target was found.");
            }

            var source = living
                .OrderByDescending(GetToxinValue)
                .ThenBy(unit => unit.SlotIndex)
                .First();
            var destinations = living.Length == 1
                ? new[] { source }
                : living
                    .Where(unit => !ReferenceEquals(unit, source))
                    .OrderBy(unit => unit.SlotIndex)
                    .Take(2)
                    .ToArray();
            return new ToxinTransferTargets(source, destinations);
        }

        private static int GetToxinValue(BattleUnitState unit)
        {
            return unit?.GetStatus(BattleStatusId.Toxin)?.Value ?? 0;
        }
    }
}
