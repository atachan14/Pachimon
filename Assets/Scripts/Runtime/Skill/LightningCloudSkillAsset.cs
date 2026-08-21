using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "LightningCloudSkill", menuName = "Pachimon/Skills/Lightning Cloud")]
    public sealed class LightningCloudSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseValue = 300;
        [SerializeField, HideInInspector] private int _electricValueRatio = 100;
        [SerializeField] private ThunderWeatherAsset _thunderDefinition;

        public int BaseValue => _baseValue;
        public int ElectricValueRatio => AttributeDamageRules.ScalingRatio;
        public ThunderWeatherAsset ThunderDefinition => _thunderDefinition;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Electric)
                errors?.Add($"Skill {SkillId}: Lightning Cloud must be Electric.");
            if (_thunderDefinition == null)
                errors?.Add($"Skill {SkillId}: Thunder Definition is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId, string displayName,
            int recovery, int cooldown, int mana, string description,
            int baseValue, int electricValueRatio,
            ThunderWeatherAsset thunderDefinition)
        {
            base.ConfigureForEditor(skillId, displayName, AllocationType.Electric,
                true, recovery, cooldown, description, mana);
            _baseValue = baseValue;
            _electricValueRatio = electricValueRatio;
            _thunderDefinition = thunderDefinition;
        }
#endif
    }
}
