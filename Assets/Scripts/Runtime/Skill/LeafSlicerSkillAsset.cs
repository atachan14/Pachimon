using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "LeafSlicerSkill", menuName = "Pachimon/Skills/Leaf Slicer")]
    public sealed class LeafSlicerSkillAsset : InitialAttributeDamageSkillAsset
    {
        [SerializeField, Min(0)] private int _pollenBaseValue = 50;
        [SerializeField] private PollenStatusAsset _pollenStatus;

        public int PollenBaseValue => _pollenBaseValue;
        public int PollenWindRatio => AttributeDamageRules.ScalingRatio;
        public PollenStatusAsset PollenStatus => _pollenStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_pollenStatus == null)
                errors?.Add($"Skill {SkillId}: Pollen Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(int id, string name, int recovery,
            int cooldown, int mana, string description, int damage, int ratio)
        {
            ConfigureInitialSkillForEditor(id, name, AllocationType.Leaf,
                recovery, cooldown, mana, description, damage, ratio);
        }

        public void ConfigurePollenForEditor(
            PollenStatusAsset pollenStatus,
            int pollenBaseValue = 50)
        {
            _pollenStatus = pollenStatus;
            _pollenBaseValue = pollenBaseValue;
        }
#endif
    }
}
