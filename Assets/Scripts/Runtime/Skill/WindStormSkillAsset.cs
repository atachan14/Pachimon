using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "WindStormSkill",
        menuName = "Pachimon/Skills/Wind Storm Skill")]
    public sealed class WindStormSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseValue = 50;
        [SerializeField, HideInInspector] private int _windValueRatio = 100;
        [SerializeField] private WindWeatherAsset _windDefinition;

        public int BaseValue => _baseValue;
        public int WindValueRatio => AttributeDamageRules.ScalingRatio;
        public WindWeatherAsset WindDefinition => _windDefinition;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Wind)
            {
                errors?.Add($"Skill {SkillId}: Wind Storm must be Wind.");
            }
            if (_windDefinition == null)
            {
                errors?.Add($"Skill {SkillId}: Wind Definition is required.");
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
            int windValueRatio,
            WindWeatherAsset windDefinition)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Wind,
                isMapAssignable: true,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost);
            _baseValue = baseValue;
            _windValueRatio = windValueRatio;
            _windDefinition = windDefinition;
        }
#endif
    }
}
