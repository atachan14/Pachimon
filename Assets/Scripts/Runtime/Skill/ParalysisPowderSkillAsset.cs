using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "ParalysisPowderSkill", menuName = "Pachimon/Skills/Paralysis Powder Skill")]
    public sealed class ParalysisPowderSkillAsset : SkillAsset
    {
        [SerializeField, Min(1)] private int _baseDurationTicks = 50;
        [SerializeField, Min(0)] private int _baseElectricValue = 60;
        [SerializeField, Min(0)] private int _basePoisonValue = 40;
        [SerializeField] private SlowStatusAsset _paralysisStatus;
        [SerializeField, Min(0)] private int _pollenBaseValue = 50;
        [SerializeField] private PollenStatusAsset _pollenStatus;
        public int BaseDurationTicks => _baseDurationTicks;
        public int DurationLeafRatio => AttributeDamageRules.ScalingRatio;
        public int BaseElectricValue => _baseElectricValue;
        public int ElectricValueRatio => AttributeDamageRules.ScalingRatio;
        public int BasePoisonValue => _basePoisonValue;
        public int PoisonValueRatio => AttributeDamageRules.ScalingRatio;
        public SlowStatusAsset ParalysisStatus => _paralysisStatus;
        public int PollenBaseValue => _pollenBaseValue;
        public int PollenPoisonRatio => AttributeDamageRules.ScalingRatio;
        public PollenStatusAsset PollenStatus => _pollenStatus;
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Leaf) errors.Add($"Skill {SkillId}: Paralysis Powder must be Leaf.");
            if (_paralysisStatus == null) errors.Add($"Skill {SkillId}: Paralysis Status is required.");
            if (_pollenStatus == null) errors.Add($"Skill {SkillId}: Pollen Status is required.");
        }

#if UNITY_EDITOR
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
