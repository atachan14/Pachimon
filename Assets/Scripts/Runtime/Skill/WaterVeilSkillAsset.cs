using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "WaterVeilSkill",
        menuName = "Pachimon/Skills/Water Veil Skill")]
    public sealed class WaterVeilSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseFieldValue = 300;
        [SerializeField, HideInInspector] private int _aquaValueRatio = 100;
        [SerializeField] private WaterVeilFieldEffectAsset _fieldEffect;

        public int BaseFieldValue => _baseFieldValue;
        public int AquaValueRatio => AttributeDamageRules.ScalingRatio;
        public WaterVeilFieldEffectAsset FieldEffect => _fieldEffect;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Aqua)
            {
                errors?.Add($"Skill {SkillId}: Water Veil must be Aqua.");
            }
            if (_fieldEffect == null)
            {
                errors?.Add($"Skill {SkillId}: Field Effect is required.");
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
            int baseFieldValue,
            int aquaValueRatio,
            WaterVeilFieldEffectAsset fieldEffect)
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
            _baseFieldValue = baseFieldValue;
            _aquaValueRatio = aquaValueRatio;
            _fieldEffect = fieldEffect;
        }
#endif
    }
}
