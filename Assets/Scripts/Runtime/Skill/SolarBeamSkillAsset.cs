using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "SolarBeamSkill", menuName = "Pachimon/Skills/Solar Beam Skill")]
    public sealed class SolarBeamSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseLeafDamage = 200;
        [SerializeField, Min(0)] private int _leafDamageRatio = 100;
        [SerializeField, Min(0)] private int _temperatureStartupRatio = 100;
        public int BaseLeafDamage => _baseLeafDamage;
        public int LeafDamageRatio => _leafDamageRatio;
        public int TemperatureStartupRatio => _temperatureStartupRatio;
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Leaf) errors.Add($"Skill {SkillId}: Solar Beam must be Leaf.");
        }
    }
}
