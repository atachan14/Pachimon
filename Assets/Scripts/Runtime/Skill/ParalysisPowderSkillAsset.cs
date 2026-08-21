using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "ParalysisPowderSkill", menuName = "Pachimon/Skills/Paralysis Powder Skill")]
    public sealed class ParalysisPowderSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseLeafParalysis = 50;
        [SerializeField, Min(0)] private int _basePoisonParalysis = 50;
        [SerializeField] private SlowStatusAsset _paralysisStatus;
        public int BaseLeafParalysis => _baseLeafParalysis;
        public int LeafRatio => AttributeDamageRules.ScalingRatio;
        public int BasePoisonParalysis => _basePoisonParalysis;
        public int PoisonRatio => AttributeDamageRules.ScalingRatio;
        public SlowStatusAsset ParalysisStatus => _paralysisStatus;
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Leaf) errors.Add($"Skill {SkillId}: Paralysis Powder must be Leaf.");
            if (_paralysisStatus == null) errors.Add($"Skill {SkillId}: Paralysis Status is required.");
        }
    }
}
