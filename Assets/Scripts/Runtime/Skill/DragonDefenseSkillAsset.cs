using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "DragonDefenseSkill", menuName = "Pachimon/Skills/Dragon Defense Skill")]
    public sealed class DragonDefenseSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseShieldValue = 300;
        [SerializeField, Min(0)] private int _dragonShieldRatio = 100;
        [SerializeField, Min(1)] private int _durationTicks = 500;
        [SerializeField] private DragonDefenseStatusAsset _defenseStatus;

        public int BaseShieldValue => _baseShieldValue;
        public int DragonShieldRatio => _dragonShieldRatio;
        public int DurationTicks => _durationTicks;
        public DragonDefenseStatusAsset DefenseStatus => _defenseStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Dragon)
                errors?.Add($"Skill {SkillId}: Dragon Defense must be Dragon.");
            if (_defenseStatus == null)
                errors?.Add($"Skill {SkillId}: Dragon Defense Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId, string displayName, int baseRecoveryTicks,
            int baseCooldownTicks, int baseManaCost, string description,
            int baseShieldValue, int dragonShieldRatio, int durationTicks,
            DragonDefenseStatusAsset defenseStatus)
        {
            base.ConfigureForEditor(
                skillId, displayName, AllocationType.Dragon, true,
                baseRecoveryTicks, baseCooldownTicks, description, baseManaCost);
            _baseShieldValue = baseShieldValue;
            _dragonShieldRatio = dragonShieldRatio;
            _durationTicks = durationTicks;
            _defenseStatus = defenseStatus;
        }
#endif
    }
}
