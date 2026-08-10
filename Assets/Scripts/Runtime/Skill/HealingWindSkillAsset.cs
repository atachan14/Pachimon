using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "HealingWindSkill", menuName = "Pachimon/Skills/Healing Wind Skill")]
    public sealed class HealingWindSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseHealing = 100;
        [SerializeField, Min(0)] private int _baseWindBonus = 50;
        [SerializeField, Min(0)] private int _baseSpeedBonus = 50;
        [SerializeField, Min(0)] private int _windRatio = 100;
        [SerializeField, Min(1)] private int _durationTicks = 200;
        [SerializeField] private HealingWindStatusAsset _statusDefinition;

        public int BaseHealing => _baseHealing;
        public int BaseWindBonus => _baseWindBonus;
        public int BaseSpeedBonus => _baseSpeedBonus;
        public int WindRatio => _windRatio;
        public int DurationTicks => _durationTicks;
        public HealingWindStatusAsset StatusDefinition => _statusDefinition;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Wind)
                errors?.Add($"Skill {SkillId}: Healing Wind must be Wind.");
            if (_statusDefinition == null)
                errors?.Add($"Skill {SkillId}: Healing Wind Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId, string displayName, int baseRecoveryTicks,
            int baseCooldownTicks, int baseManaCost, string description,
            int baseHealing, int baseWindBonus, int baseSpeedBonus,
            int windRatio, int durationTicks,
            HealingWindStatusAsset statusDefinition)
        {
            base.ConfigureForEditor(skillId, displayName, AllocationType.Wind,
                true, baseRecoveryTicks, baseCooldownTicks, description,
                baseManaCost);
            _baseHealing = baseHealing;
            _baseWindBonus = baseWindBonus;
            _baseSpeedBonus = baseSpeedBonus;
            _windRatio = windRatio;
            _durationTicks = durationTicks;
            _statusDefinition = statusDefinition;
        }
#endif
    }
}
