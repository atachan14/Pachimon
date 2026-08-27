using System;
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
            BattleUnitState destination)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Destination = destination
                ?? throw new ArgumentNullException(nameof(destination));
        }

        public BattleUnitState Source { get; }
        public BattleUnitState Destination { get; }
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
            var applied = ToxinTransferMath.CalculateApplication(
                removed,
                baseValue,
                _skill.ApplicationPercent);

            if (removed > 0)
            {
                context.State.AddLog(
                    $"{targets.Source.DisplayName}から{removed}の毒素を取り除いた！");
            }

            if (applied > 0)
            {
                ApplyToxin(context, targets.Destination, applied);
            }

            return new SkillResolution(
                context.User,
                context.Skill,
                Array.Empty<SkillEffectResult>());
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
            var destination = living.Length == 1
                ? source
                : living
                    .Where(unit => !ReferenceEquals(unit, source))
                    .OrderBy(GetToxinValue)
                    .ThenBy(unit => unit.SlotIndex)
                    .First();
            return new ToxinTransferTargets(source, destination);
        }

        private static int GetToxinValue(BattleUnitState unit)
        {
            return unit?.GetStatus(BattleStatusId.Toxin)?.Value ?? 0;
        }
    }
}
