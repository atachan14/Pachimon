using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "SecondWindSkill", menuName = "Pachimon/Skills/Second Wind Skill")]
    public sealed class SecondWindSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _windShieldRatio = 200;
        [SerializeField, Min(1)] private int _durationTicks = 200;
        [SerializeField] private StillAirStatusAsset _stillAirStatus;

        public int WindShieldRatio => _windShieldRatio;
        public int DurationTicks => _durationTicks;
        public StillAirStatusAsset StillAirStatus => _stillAirStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Wind)
                errors?.Add($"Skill {SkillId}: Second Wind must be Wind.");
            if (_stillAirStatus == null)
                errors?.Add($"Skill {SkillId}: Still Air Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId, string displayName, int baseRecoveryTicks,
            int baseCooldownTicks, int baseManaCost, string description,
            int windShieldRatio, int durationTicks,
            StillAirStatusAsset stillAirStatus)
        {
            base.ConfigureForEditor(skillId, displayName, AllocationType.Wind,
                true, baseRecoveryTicks, baseCooldownTicks, description,
                baseManaCost);
            _windShieldRatio = windShieldRatio;
            _durationTicks = durationTicks;
            _stillAirStatus = stillAirStatus;
        }
#endif
    }
}
