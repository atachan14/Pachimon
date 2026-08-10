using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "HeavySnowSkill",
        menuName = "Pachimon/Skills/Heavy Snow Skill")]
    public sealed class HeavySnowSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseValue = 400;
        [SerializeField, Min(0)] private int _iceValueRatio = 100;
        [SerializeField] private SunnyWeatherAsset _temperatureDefinition;

        public int BaseValue => _baseValue;
        public int IceValueRatio => _iceValueRatio;
        public SunnyWeatherAsset TemperatureDefinition => _temperatureDefinition;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Ice)
            {
                errors?.Add($"Skill {SkillId}: Heavy Snow must be Ice.");
            }
            if (_temperatureDefinition == null)
            {
                errors?.Add($"Skill {SkillId}: Temperature Definition is required.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            int baseRecoveryTicks,
            int baseCooldownTicks,
            int baseManaCost,
            string description,
            int baseValue,
            int iceValueRatio,
            SunnyWeatherAsset temperatureDefinition)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Ice,
                isMapAssignable: true,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost);
            _baseValue = baseValue;
            _iceValueRatio = iceValueRatio;
            _temperatureDefinition = temperatureDefinition;
        }
#endif
    }
}
