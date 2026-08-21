using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "SunnyDaySkill",
        menuName = "Pachimon/Skills/Sunny Day Skill")]
    public sealed class SunnyDaySkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseValue = 100;
        [SerializeField, HideInInspector] private int _fireValueRatio = 100;
        [FormerlySerializedAs("_weather")]
        [SerializeField] private SunnyWeatherAsset _temperatureDefinition;

        public int BaseValue => _baseValue;
        public int FireValueRatio => AttributeDamageRules.ScalingRatio;
        public SunnyWeatherAsset TemperatureDefinition => _temperatureDefinition;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Fire)
            {
                errors?.Add($"Skill {SkillId}: Sunny Day must be Fire.");
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
            int fireValueRatio,
            SunnyWeatherAsset temperatureDefinition)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Fire,
                isMapAssignable: true,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost);
            _baseValue = baseValue;
            _fireValueRatio = fireValueRatio;
            _temperatureDefinition = temperatureDefinition;
        }
#endif
    }
}
