using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "PlaceholderSkill", menuName = "Pachimon/Skills/Placeholder Skill")]
    public sealed class PlaceholderSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseDamage = 100;
        [SerializeField, Min(0)] private int _statusBaseValue = 100;
        [SerializeField, Min(0)] private int _statusScalingPercent = 100;
        [SerializeField] private ToxinStatusAsset _toxinStatus;
        [SerializeField] private SlowStatusAsset _paralysisStatus;
        [SerializeField] private SlowStatusAsset _chillStatus;

        public int BaseDamage => _baseDamage;
        public int StatusBaseValue => _statusBaseValue;
        public int StatusScalingPercent => _statusScalingPercent;
        public ToxinStatusAsset ToxinStatus => _toxinStatus;
        public SlowStatusAsset ParalysisStatus => _paralysisStatus;
        public SlowStatusAsset ChillStatus => _chillStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType == AllocationType.Poison
                && _statusBaseValue > 0
                && _toxinStatus == null)
            {
                errors.Add($"Skill {SkillId}: Toxin Definition is required.");
            }
            if (AllocationType == AllocationType.Electric
                && _paralysisStatus == null)
            {
                errors.Add($"Skill {SkillId}: Paralysis Definition is required.");
            }
            if (AllocationType == AllocationType.Ice && _chillStatus == null)
            {
                errors.Add($"Skill {SkillId}: Chill Definition is required.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureBaseDamageForEditor(int baseDamage)
        {
            _baseDamage = baseDamage;
        }

        public void ConfigureStatusForEditor(
            int statusBaseValue,
            int statusScalingPercent,
            ToxinStatusAsset toxinStatus = null,
            SlowStatusAsset paralysisStatus = null,
            SlowStatusAsset chillStatus = null)
        {
            _statusBaseValue = statusBaseValue;
            _statusScalingPercent = statusScalingPercent;
            _toxinStatus = toxinStatus;
            _paralysisStatus = paralysisStatus;
            _chillStatus = chillStatus;
        }
#endif
    }
}
