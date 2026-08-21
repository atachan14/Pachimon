using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "WindErosionSkill", menuName = "Pachimon/Skills/Wind Erosion Skill")]
    public sealed class WindErosionSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseErosionValue = 20;
        [SerializeField, HideInInspector] private int _windValueRatio = 100;
        [SerializeField] private WindErosionStatusAsset _statusDefinition;

        public int BaseErosionValue => _baseErosionValue;
        public int WindValueRatio => AttributeDamageRules.ScalingRatio;
        public WindErosionStatusAsset StatusDefinition => _statusDefinition;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Wind)
                errors?.Add($"Skill {SkillId}: Wind Erosion must be Wind.");
            if (_statusDefinition == null)
                errors?.Add($"Skill {SkillId}: Wind Erosion Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId, string displayName, int baseRecoveryTicks,
            int baseCooldownTicks, int baseManaCost, string description,
            int baseErosionValue, int windValueRatio,
            WindErosionStatusAsset statusDefinition)
        {
            base.ConfigureForEditor(skillId, displayName, AllocationType.Wind,
                true, baseRecoveryTicks, baseCooldownTicks, description,
                baseManaCost);
            _baseErosionValue = baseErosionValue;
            _windValueRatio = windValueRatio;
            _statusDefinition = statusDefinition;
        }
#endif
    }
}
