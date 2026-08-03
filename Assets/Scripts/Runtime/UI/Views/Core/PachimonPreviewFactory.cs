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
            PassiveStatModifierRegistry passiveStatModifierRegistry)
        {
            if (instance?.Stats == null)
            {
                return PachimonPreviewContent.Hidden;
            }

            var definition = pachimonCatalog?.Get(instance.SpeciesId);
            var stats = PachimonStatService.Calculate(
                instance.Stats,
                modifiers,
                instance.PassiveIds,
                passiveStatModifierRegistry);
            return Create(
                instance.SpeciesId,
                definition,
                Math.Min(instance.CurrentHp, stats.MaxHp),
                Math.Min(instance.CurrentMn, stats.MaxMn),
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
                unit.CurrentMn,
                unit.GetBattleStats(),
                unit.SkillIds,
                unit.PassiveIds,
                skillCatalog,
                passiveCatalog,
                unit.Statuses.Select(status => status.DisplayName));
        }

        private static PachimonPreviewContent Create(
            int speciesId,
            PachimonSpeciesDefinition definition,
            int currentHp,
            int currentMn,
            EffectivePachimonStats stats,
            IEnumerable<int> skillIds,
            IEnumerable<int> passiveIds,
            SkillCatalog skillCatalog,
            PassiveCatalog passiveCatalog,
            IEnumerable<string> statusEffects)
        {
            var skills = skillIds
                .Select(skillId => new PachimonAbilityPreview(
                    PachimonAbilityKind.Skill,
                    skillId,
                    skillCatalog?.Get(skillId)?.DisplayName ?? $"Skill #{skillId}"))
                .ToArray();
            return new PachimonPreviewContent(
                definition?.FrontSprite,
                definition?.DisplayName ?? $"Pachimon #{speciesId}",
                currentHp,
                stats.MaxHp,
                currentMn,
                stats.MaxMn,
                BuildStatPreviews(stats),
                statusEffects?.ToArray() ?? Array.Empty<string>(),
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
