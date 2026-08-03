using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "PassiveCatalog",
        menuName = "Pachimon/Passives/Passive Catalog")]
    public sealed class PassiveCatalog : ScriptableObject
    {
        [SerializeField] private List<PassiveAsset> _passives = new();

        public IReadOnlyList<PassiveAsset> Passives => _passives;

        public PassiveAsset Get(int passiveId)
        {
            return _passives.FirstOrDefault(
                passive => passive != null && passive.PassiveId == passiveId);
        }

        public IReadOnlyList<string> ValidateContent()
        {
            var errors = new List<string>();
            var validPassives = _passives.Where(passive => passive != null).ToArray();
            if (validPassives.Length != _passives.Count)
            {
                errors.Add("PassiveCatalog contains a null entry.");
            }

            foreach (var duplicateId in validPassives
                         .GroupBy(passive => passive.PassiveId)
                         .Where(group => group.Count() > 1)
                         .Select(group => group.Key))
            {
                errors.Add($"Duplicate Passive ID: {duplicateId}");
            }

            foreach (var passive in validPassives)
            {
                passive.CollectValidationErrors(errors);
            }

            return errors;
        }

#if UNITY_EDITOR
        public void SetPassivesForEditor(IEnumerable<PassiveAsset> passives)
        {
            _passives = new List<PassiveAsset>(passives);
        }
#endif
    }
}
