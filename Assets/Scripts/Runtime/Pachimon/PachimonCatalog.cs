using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pachimon.Data
{
    [CreateAssetMenu(fileName = "PachimonCatalog", menuName = "Pachimon/Pachimon Catalog")]
    public sealed class PachimonCatalog : ScriptableObject
    {
        public const int RequiredSpeciesCount = 151;

        [SerializeField] private List<PachimonSpeciesDefinition> _species = new();

        public IReadOnlyList<PachimonSpeciesDefinition> Species => _species;

        public PachimonSpeciesDefinition Get(int speciesId)
        {
            return _species.FirstOrDefault(definition => definition != null
                && definition.SpeciesId == speciesId);
        }

        public IReadOnlyList<string> ValidateContent()
        {
            var errors = new List<string>();
            var validSpecies = _species.Where(definition => definition != null).ToArray();

            if (validSpecies.Length != RequiredSpeciesCount)
            {
                errors.Add(
                    $"PachimonCatalog requires {RequiredSpeciesCount} species, "
                    + $"but contains {validSpecies.Length}.");
            }

            foreach (var duplicateId in validSpecies
                         .GroupBy(definition => definition.SpeciesId)
                         .Where(group => group.Count() > 1)
                         .Select(group => group.Key))
            {
                errors.Add($"Duplicate Pachimon species ID: {duplicateId}");
            }

            for (var speciesId = 1; speciesId <= RequiredSpeciesCount; speciesId++)
            {
                var definition = validSpecies.FirstOrDefault(item => item.SpeciesId == speciesId);
                if (definition == null)
                {
                    errors.Add($"Pachimon species ID {speciesId} is missing.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.DisplayName))
                {
                    errors.Add($"Pachimon species ID {speciesId} has no display name.");
                }
                else if (definition.DisplayName.Length
                         > PachimonSpeciesDefinition.MaxDisplayNameLength)
                {
                    errors.Add(
                        $"Pachimon species ID {speciesId} display name exceeds "
                        + $"{PachimonSpeciesDefinition.MaxDisplayNameLength} characters.");
                }

                if (definition.FrontSprite == null || definition.BackSprite == null)
                {
                    errors.Add($"Pachimon species ID {speciesId} is missing a graphic.");
                }

                if (definition.FixedSkillId <= 0)
                {
                    errors.Add($"Pachimon species ID {speciesId} has no fixed Skill ID.");
                }

                if (definition.PassiveId <= 0)
                {
                    errors.Add($"Pachimon species ID {speciesId} has no Passive ID.");
                }

                if (definition.AllocationType == AllocationType.Unassigned)
                {
                    errors.Add($"Pachimon species ID {speciesId} has no Allocation Type.");
                }
            }

            return errors;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            foreach (var definition in _species)
            {
                definition?.EnforceDisplayNameLengthForEditor();
            }
        }

        public void SetSpeciesForEditor(IEnumerable<PachimonSpeciesDefinition> species)
        {
            _species = new List<PachimonSpeciesDefinition>(species);
        }

        public bool SetSpeciesPresentationForEditor(
            int speciesId,
            string displayName,
            Sprite frontSprite,
            Sprite backSprite)
        {
            var definition = Get(speciesId);
            return definition != null
                && definition.SetPresentationForEditor(displayName, frontSprite, backSprite);
        }

        public bool SetAllSpeciesGraphicsForEditor(Sprite frontSprite, Sprite backSprite)
        {
            var changed = false;
            foreach (var definition in _species)
            {
                if (definition != null)
                {
                    changed |= definition.SetGraphicsForEditor(frontSprite, backSprite);
                }
            }

            return changed;
        }

        public bool SetGraphicsByAllocationTypeForEditor(
            AllocationType allocationType,
            Sprite frontSprite,
            Sprite backSprite)
        {
            var changed = false;
            foreach (var definition in _species)
            {
                if (definition != null && definition.AllocationType == allocationType)
                {
                    changed |= definition.SetGraphicsForEditor(frontSprite, backSprite);
                }
            }

            return changed;
        }

        public bool ResetDefaultDisplayNamesForEditor()
        {
            return ApplyDefaultDisplayNamesForEditor(false);
        }

        public bool MigrateGeneratedDisplayNamesForEditor()
        {
            return ApplyDefaultDisplayNamesForEditor(true);
        }

        private bool ApplyDefaultDisplayNamesForEditor(bool preserveCustomNames)
        {
            var changed = false;
            var nextNumberByType = new Dictionary<AllocationType, int>();
            foreach (var definition in _species
                         .Where(definition => definition != null)
                         .OrderBy(definition => definition.SpeciesId))
            {
                var allocationType = definition.AllocationType;
                nextNumberByType.TryGetValue(allocationType, out var currentNumber);
                var nextNumber = currentNumber + 1;
                nextNumberByType[allocationType] = nextNumber;
                if (preserveCustomNames
                    && !IsGeneratedDisplayName(definition.DisplayName))
                {
                    continue;
                }

                changed |= definition.SetDisplayNameForEditor(GetDefaultDisplayName(
                    definition.SpeciesId,
                    allocationType,
                    nextNumber));
            }

            return changed;
        }

        private static string GetDefaultDisplayName(
            int speciesId,
            AllocationType allocationType,
            int number)
        {
            return speciesId switch
            {
                1 => "パチカゲ",
                2 => "パチガメ",
                3 => "パチギダネ",
                4 => "パチチュウ",
                5 => "パチムシ",
                6 => "パチゴオリ",
                7 => "パチカゼ",
                8 => "パチリュウ",
                _ => AttributePlaceholderName.Format(allocationType, number),
            };
        }

        private static bool IsGeneratedDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)
                || displayName.StartsWith("パチモン")
                || displayName == "パチドラゴン")
            {
                return true;
            }

            if (displayName.Length != 4
                || "炎水草電毒氷風竜無".IndexOf(displayName[0]) < 0)
            {
                return false;
            }

            return char.IsDigit(displayName[1])
                && char.IsDigit(displayName[2])
                && char.IsDigit(displayName[3]);
        }

        public bool PopulateMissingLogicIdsForEditor()
        {
            var changed = false;
            foreach (var definition in _species)
            {
                if (definition != null)
                {
                    changed |= definition.PopulateMissingLogicIdsForEditor();
                }
            }

            return changed;
        }

        public bool PopulateMissingAllocationTypesForEditor()
        {
            var changed = false;
            foreach (var definition in _species)
            {
                if (definition != null)
                {
                    changed |= definition.PopulateMissingAllocationTypeForEditor();
                }
            }

            return changed;
        }
#endif
    }
}
