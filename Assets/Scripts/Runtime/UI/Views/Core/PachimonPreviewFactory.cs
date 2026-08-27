using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Battle;
using Pachimon.Data;
using Pachimon.Run;
using Pachimon.Skills;
using Pachimon.Passives;

namespace Pachimon.UI
{
    public static class PachimonPreviewFactory
    {
        public static PachimonPreviewContent FromRunInstance(
            PachimonInstance instance,
            TrainerModifierSet modifiers,
            PachimonCatalog pachimonCatalog,
            SkillCatalog skillCatalog,
            PassiveCatalog passiveCatalog,
            PassiveStatModifierRegistry passiveStatModifierRegistry,
            bool includeTrainerResourceIncrease = false)
        {
            if (instance?.Stats == null)
            {
                return PachimonPreviewContent.Hidden;
            }

            var definition = pachimonCatalog?.Get(instance.SpeciesId);
            var stats = PachimonStatService.Calculate(
                instance,
                modifiers,
                passiveStatModifierRegistry);
            var currentHp = Math.Min(instance.CurrentHp, stats.MaxHp);
            var currentMn = Math.Min(instance.CurrentMn, stats.MaxMn);
            if (includeTrainerResourceIncrease)
            {
                var unmodifiedStats = PachimonStatService.Calculate(
                    instance,
                    null,
                    passiveStatModifierRegistry);
                currentHp = EnemyTrainerScalingService.PreserveMissingResource(
                    instance.CurrentHp,
                    unmodifiedStats.MaxHp,
                    stats.MaxHp);
                currentMn = EnemyTrainerScalingService.PreserveMissingResource(
                    instance.CurrentMn,
                    unmodifiedStats.MaxMn,
                    stats.MaxMn);
            }
            return Create(
                instance.SpeciesId,
                definition,
                currentHp,
                0,
                currentMn,
                stats,
                instance.SkillSlots,
                instance.PassiveIds,
                skillCatalog,
                passiveCatalog,
                statusEffects: null,
                subStatBindings: instance.SubStatBindings,
                sourceInstance: instance);
        }

        public static PachimonPreviewContent FromBattleUnit(
            BattleUnitState unit,
            PachimonCatalog pachimonCatalog,
            SkillCatalog skillCatalog,
            PassiveCatalog passiveCatalog,
            PachimonInstance sourceInstance = null)
        {
            if (unit == null)
            {
                return PachimonPreviewContent.Hidden;
            }

            return Create(
                unit.SpeciesId,
                pachimonCatalog?.Get(unit.SpeciesId),
                unit.CurrentHp,
                unit.TotalShield,
                unit.CurrentMn,
                unit.GetBattleStats(),
                unit.SkillSlots,
                unit.PassiveIds,
                skillCatalog,
                passiveCatalog,
                unit.Statuses
                    .Where(status => status.IsVisible)
                    .Select(status => new PachimonStatusPreview(status)),
                unit.SubStatBindings,
                sourceInstance);
        }

        private static PachimonPreviewContent Create(
            int speciesId,
            PachimonSpeciesAsset definition,
            int currentHp,
            int currentShield,
            int currentMn,
            EffectivePachimonStats stats,
            IEnumerable<PachimonSkillSlot> skillSlots,
            IEnumerable<int> passiveIds,
            SkillCatalog skillCatalog,
            PassiveCatalog passiveCatalog,
            IEnumerable<PachimonStatusPreview> statusEffects,
            PachimonSubStatBindings subStatBindings,
            PachimonInstance sourceInstance)
        {
            var skills = skillSlots
                .Select(slot =>
                {
                    var skillId = slot.SkillId;
                    var skill = skillCatalog?.Get(skillId);
                    return new PachimonAbilityPreview(
                        PachimonAbilityKind.Skill,
                        skillId,
                        SkillUpgradeMath.FormatDisplayName(
                            skill?.DisplayName ?? $"Skill #{skillId}",
                            slot.UpgradeLevel),
                        skill,
                        slot.UpgradeLevel);
                })
                .ToArray();
            return new PachimonPreviewContent(
                definition?.FrontSprite,
                definition?.DisplayName ?? $"Pachimon #{speciesId}",
                currentHp,
                stats.MaxHp,
                currentShield,
                currentMn,
                stats.MaxMn,
                BuildStatPreviews(stats, subStatBindings),
                statusEffects?.ToArray() ?? Array.Empty<PachimonStatusPreview>(),
                skills,
                passiveIds.Select(passiveId => new PachimonAbilityPreview(
                    PachimonAbilityKind.Passive,
                    passiveId,
                    GetPassiveDisplayName(passiveId, passiveCatalog))).ToArray(),
                stats.Calculation,
                BuildEquipmentPreviews(sourceInstance),
                BuildEngravingPreviews(sourceInstance));
        }

        private static IReadOnlyList<PachimonEquipmentPreview> BuildEquipmentPreviews(
            PachimonInstance instance)
        {
            return instance?.Equipment
                .OrderBy(entry => entry.Key)
                .Select(entry => new PachimonEquipmentPreview(
                    entry.Key,
                    entry.Value.DisplayName,
                    entry.Value.GeneratedData))
                .ToArray()
                ?? Array.Empty<PachimonEquipmentPreview>();
        }

        private static IReadOnlyList<PachimonEngravingPreview> BuildEngravingPreviews(
            PachimonInstance instance)
        {
            return instance?.Engravings
                .Select(entry => new PachimonEngravingPreview(
                    entry.DisplayName,
                    entry.GeneratedData))
                .ToArray()
                ?? Array.Empty<PachimonEngravingPreview>();
        }

        private static string GetPassiveDisplayName(
            int passiveId,
            PassiveCatalog passiveCatalog)
        {
            return passiveCatalog?.Get(passiveId) is { } definition
                ? definition.DisplayName
                : AttributePlaceholderName.FromCyclicId(passiveId);
        }

        private static IReadOnlyList<PachimonStatPreview> BuildStatPreviews(
            EffectivePachimonStats stats,
            PachimonSubStatBindings subStatBindings)
        {
            return new[]
            {
                Stat(PachimonDisplayStat.Fire, PachimonStatType.Fire),
                Stat(PachimonDisplayStat.Aqua, PachimonStatType.Aqua),
                Stat(PachimonDisplayStat.Leaf, PachimonStatType.Leaf),
                Stat(PachimonDisplayStat.Electric, PachimonStatType.Electric),
                Stat(PachimonDisplayStat.Ice, PachimonStatType.Ice),
                Stat(PachimonDisplayStat.Wind, PachimonStatType.Wind),
                Stat(PachimonDisplayStat.Poison, PachimonStatType.Poison),
                Stat(PachimonDisplayStat.Dragon, PachimonStatType.Dragon),
                Stat(PachimonDisplayStat.DamageBonus, PachimonStatType.DamageBonus),
                Stat(PachimonDisplayStat.ResistBonus, PachimonStatType.ResistBonus),
                Stat(PachimonDisplayStat.Speed, PachimonStatType.Speed),
                Stat(PachimonDisplayStat.Haste, PachimonStatType.Haste),
                Stat(PachimonDisplayStat.GenerationPower, PachimonStatType.GenerationPower),
                Stat(PachimonDisplayStat.StatusMastery, PachimonStatType.StatusMastery),
                Stat(PachimonDisplayStat.SustainPower, PachimonStatType.SustainPower),
                Stat(PachimonDisplayStat.StatusResistance, PachimonStatType.StatusResistance),
            };

            PachimonStatPreview Stat(PachimonDisplayStat displayStat, PachimonStatType statType)
            {
                return new PachimonStatPreview(
                    displayStat,
                    stats.GetValue(statType),
                    PachimonSubStatBindings.IsAttribute(statType)
                        ? ToDisplayStat(subStatBindings.GetSubStat(statType))
                        : null);
            }
        }

        private static PachimonDisplayStat ToDisplayStat(PachimonStatType statType)
        {
            return statType switch
            {
                PachimonStatType.Speed => PachimonDisplayStat.Speed,
                PachimonStatType.Haste => PachimonDisplayStat.Haste,
                PachimonStatType.DamageBonus => PachimonDisplayStat.DamageBonus,
                PachimonStatType.ResistBonus => PachimonDisplayStat.ResistBonus,
                PachimonStatType.GenerationPower => PachimonDisplayStat.GenerationPower,
                PachimonStatType.StatusMastery => PachimonDisplayStat.StatusMastery,
                PachimonStatType.SustainPower => PachimonDisplayStat.SustainPower,
                PachimonStatType.StatusResistance => PachimonDisplayStat.StatusResistance,
                _ => throw new ArgumentOutOfRangeException(nameof(statType)),
            };
        }
    }
}
