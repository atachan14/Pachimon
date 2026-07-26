using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pachimon.Trainer
{
    [Serializable]
    public sealed class TrainerNameDefinition
    {
        [SerializeField] private string _nameId;
        [SerializeField] private TrainerGender _gender;
        [SerializeField] private string _displayName;

        public TrainerNameDefinition(string nameId, TrainerGender gender, string displayName)
        {
            _nameId = nameId;
            _gender = gender;
            _displayName = displayName;
        }

        public string NameId => _nameId;
        public TrainerGender Gender => _gender;
        public string DisplayName => _displayName;
    }

    [CreateAssetMenu(fileName = "TrainerNameCatalog", menuName = "Pachimon/Trainer Name Catalog")]
    public sealed class TrainerNameCatalog : ScriptableObject
    {
        [SerializeField] private List<TrainerNameDefinition> _names = new();

        public IReadOnlyList<TrainerNameDefinition> Names => _names;
        public TrainerNameDefinition Get(string nameId) =>
            _names.FirstOrDefault(name => name != null && name.NameId == nameId);
        public IReadOnlyList<TrainerNameDefinition> GetByGender(TrainerGender gender) =>
            _names.Where(name => name != null && name.Gender == gender).ToArray();

#if UNITY_EDITOR
        public void SetNamesForEditor(IEnumerable<TrainerNameDefinition> names)
        {
            _names = new List<TrainerNameDefinition>(names);
        }
#endif
    }
}
