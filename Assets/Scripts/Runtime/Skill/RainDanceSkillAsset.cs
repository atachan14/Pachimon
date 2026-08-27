using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "RainDanceSkill",
        menuName = "Pachimon/Skills/Rain Dance Skill")]
    public sealed class RainDanceSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseValue = 50;
        [SerializeField, HideInInspector] private int _aquaValueRatio = 100;
        [SerializeField] private RainWeatherAsset _rainDefinition;

        public int BaseValue => _baseValue;
        public int AquaValueRatio => AttributeDamageRules.ScalingRatio;
        public RainWeatherAsset RainDefinition => _rainDefinition;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Aqua)
            {
                errors?.Add($"Skill {SkillId}: Rain Dance must be Aqua.");
            }
            if (_rainDefinition == null)
            {
                errors?.Add($"Skill {SkillId}: Rain Definition is required.");
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
            int aquaValueRatio,
            RainWeatherAsset rainDefinition)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Aqua,
                isMapAssignable: true,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost);
            _baseValue = baseValue;
            _aquaValueRatio = aquaValueRatio;
            _rainDefinition = rainDefinition;
        }
#endif
    }
}
