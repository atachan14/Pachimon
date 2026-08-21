using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "IceShieldSkill",
        menuName = "Pachimon/Skills/Ice Shield Skill")]
    public sealed class IceShieldSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseShieldValue = 300;
        [SerializeField, HideInInspector] private int _iceShieldRatio = 100;

        public int BaseShieldValue => _baseShieldValue;
        public int IceShieldRatio => AttributeDamageRules.ScalingRatio;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Ice)
            {
                errors.Add($"Skill {SkillId}: Ice Shield must be Ice.");
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
            int baseShieldValue,
            int iceShieldRatio)
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
            _baseShieldValue = baseShieldValue;
            _iceShieldRatio = iceShieldRatio;
        }
#endif
    }
}
