using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "ElectricShockSkill", menuName = "Pachimon/Skills/Electric Shock")]
    public sealed class ElectricShockSkillAsset : InitialAttributeDamageSkillAsset
    {
        [SerializeField, Min(0)] private int _electricParalysisBaseValue = 50;
        [SerializeField, Min(0)] private int _iceParalysisBaseValue = 25;
        [SerializeField] private SlowStatusAsset _paralysisStatus;

        public int ElectricParalysisBaseValue => _electricParalysisBaseValue;
        public int ElectricParalysisRatio => AttributeDamageRules.ScalingRatio;
        public int IceParalysisBaseValue => _iceParalysisBaseValue;
        public int IceParalysisRatio => AttributeDamageRules.ScalingRatio;
        public SlowStatusAsset ParalysisStatus => _paralysisStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_paralysisStatus == null)
                errors?.Add($"Skill {SkillId}: Paralysis Definition is required.");
        }
    }
}
