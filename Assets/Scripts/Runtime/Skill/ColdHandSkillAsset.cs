using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "ColdHandSkill", menuName = "Pachimon/Skills/Cold Hand")]
    public sealed class ColdHandSkillAsset : InitialAttributeDamageSkillAsset
    {
        [SerializeField, Min(0)] private int _chillBaseValue = 75;
        [SerializeField] private SlowStatusAsset _chillStatus;

        public int ChillBaseValue => _chillBaseValue;
        public int ChillRatio => AttributeDamageRules.ScalingRatio;
        public SlowStatusAsset ChillStatus => _chillStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_chillStatus == null)
                errors?.Add($"Skill {SkillId}: Chill Definition is required.");
        }
    }
}
