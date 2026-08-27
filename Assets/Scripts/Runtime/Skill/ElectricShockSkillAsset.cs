using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "ElectricShockSkill", menuName = "Pachimon/Skills/Electric Shock")]
    public sealed class ElectricShockSkillAsset : InitialAttributeDamageSkillAsset
    {
        [FormerlySerializedAs("_electricParalysisBaseValue")]
        [SerializeField, Min(0)] private int _paralysisBaseValue = 80;
        [FormerlySerializedAs("_iceParalysisBaseValue")]
        [SerializeField, Min(1)] private int _paralysisBaseDurationTicks = 50;
        [SerializeField] private SlowStatusAsset _paralysisStatus;

        public int ParalysisBaseValue => _paralysisBaseValue;
        public int ParalysisValueRatio => AttributeDamageRules.ScalingRatio;
        public int ParalysisBaseDurationTicks => _paralysisBaseDurationTicks;
        public int ParalysisDurationRatio => AttributeDamageRules.ScalingRatio;
        public SlowStatusAsset ParalysisStatus => _paralysisStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_paralysisStatus == null)
                errors?.Add($"Skill {SkillId}: Paralysis Definition is required.");
        }
    }
}
