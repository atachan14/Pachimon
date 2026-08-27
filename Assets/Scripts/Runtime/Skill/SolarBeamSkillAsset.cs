using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "SolarBeamSkill", menuName = "Pachimon/Skills/Solar Beam Skill")]
    public sealed class SolarBeamSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseLeafDamage = 200;
        [SerializeField, Min(0)] private int _temperatureStartupRatio = 100;
        [SerializeField, Min(0)] private int _pollenBaseValue = 100;
        [SerializeField] private PollenStatusAsset _pollenStatus;

        public int BaseLeafDamage => _baseLeafDamage;
        public int TemperatureStartupRatio => _temperatureStartupRatio;
        public int PollenBaseValue => _pollenBaseValue;
        public int PollenWindRatio => AttributeDamageRules.ScalingRatio;
        public PollenStatusAsset PollenStatus => _pollenStatus;
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Leaf) errors.Add($"Skill {SkillId}: Solar Beam must be Leaf.");
            if (_pollenStatus == null) errors.Add($"Skill {SkillId}: Pollen Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigurePollenForEditor(
            PollenStatusAsset pollenStatus,
            int pollenBaseValue = 100)
        {
            _pollenStatus = pollenStatus;
            _pollenBaseValue = pollenBaseValue;
        }
#endif
    }
}
