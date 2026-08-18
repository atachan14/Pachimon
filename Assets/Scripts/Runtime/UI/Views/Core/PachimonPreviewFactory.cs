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
                instance.SkillIds,
                instance.PassiveIds,
                skillCatalog,
                passiveCatalog,
                statusEffects: null);
        }

        public static PachimonPreviewContent FromBattleUnit(
            BattleUnitState unit,
            PachimonCatalog pachimonCatalog,
            SkillCatalog skillCatalog,
            PassiveCatalog passiveCatalog)
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
                unit.SkillIds,
                unit.PassiveIds,
                skillCatalog,
                passiveCatalog,
                unit.Statuses
                    .Where(status => status.IsVisible)
                    .Select(status => new PachimonStatusPreview(status)));
        }

        private static PachimonPreviewContent Create(
            int speciesId,
            PachimonSpeciesDefinition definition,
            int currentHp,
            int currentShield,
            int currentMn,
            EffectivePachimonStats stats,
            IEnumerable<int> skillIds,
            IEnumerable<int> passiveIds,
            SkillCatalog skillCatalog,
            PassiveCatalog passiveCatalog,
            IEnumerable<PachimonStatusPreview> statusEffects)
        {
            var skills = skillIds
                .Select(skillId =>
                {
                    var skill = skillCatalog?.Get(skillId);
                    return new PachimonAbilityPreview(
                        PachimonAbilityKind.Skill,
                        skillId,
                        skill?.DisplayName ?? $"Skill #{skillId}",
                        skill);
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
                BuildStatPreviews(stats),
                statusEffects?.ToArray() ?? Array.Empty<PachimonStatusPreview>(),
                skills,
                passiveIds.Select(passiveId => new PachimonAbilityPreview(
                    PachimonAbilityKind.Passive,
                    passiveId,
                    GetPassiveDisplayName(passiveId, passiveCatalog))).ToArray(),
                stats.Calculation);
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
            EffectivePachimonStats stats)
        {
            return new[]
            {
                Stat(PachimonDisplayStat.Fire, PachimonStatType.Fire),
                Stat(PachimonDisplayStat.Poison, PachimonStatType.Poison),
                Stat(PachimonDisplayStat.Aqua, PachimonStatType.Aqua),
                Stat(PachimonDisplayStat.Ice, PachimonStatType.Ice),
                Stat(PachimonDisplayStat.Leaf, PachimonStatType.Leaf),
                Stat(PachimonDisplayStat.Wind, PachimonStatType.Wind),
                Stat(PachimonDisplayStat.Electric, PachimonStatType.Electric),
                Stat(PachimonDisplayStat.Dragon, PachimonStatType.Dragon),
                Stat(PachimonDisplayStat.Speed, PachimonStatType.Speed),
                Stat(PachimonDisplayStat.Haste, PachimonStatType.Haste),
                Stat(PachimonDisplayStat.DamageBonus, PachimonStatType.DamageBonus),
                Stat(PachimonDisplayStat.ResistBonus, PachimonStatType.ResistBonus),
            };

            PachimonStatPreview Stat(PachimonDisplayStat displayStat, PachimonStatType statType)
            {
                return new PachimonStatPreview(displayStat, stats.GetValue(statType));
            }
        }
    }
}
