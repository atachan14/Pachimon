using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "DragonDanceSkill", menuName = "Pachimon/Skills/Dragon Dance Skill")]
    public sealed class DragonDanceSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _dragonBonus = 50;
        [SerializeField, Min(0)] private int _speedBonus = 20;
        [SerializeField] private DragonDanceStatusAsset _statusDefinition;

        public int DragonBonus => _dragonBonus;
        public int SpeedBonus => _speedBonus;
        public DragonDanceStatusAsset StatusDefinition => _statusDefinition;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Dragon)
                errors?.Add($"Skill {SkillId}: Dragon Dance must be Dragon.");
            if (_statusDefinition == null)
                errors?.Add($"Skill {SkillId}: Dragon Dance Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            int baseRecoveryTicks,
            int baseCooldownTicks,
            int baseManaCost,
            string description,
            int dragonBonus,
            int speedBonus,
            DragonDanceStatusAsset statusDefinition)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Dragon,
                true,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost);
            _dragonBonus = dragonBonus;
            _speedBonus = speedBonus;
            _statusDefinition = statusDefinition;
        }
#endif
    }
}
