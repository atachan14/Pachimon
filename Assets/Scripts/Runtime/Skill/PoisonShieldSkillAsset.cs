using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "PoisonShieldSkill",
        menuName = "Pachimon/Skills/Poison Shield Skill")]
    public sealed class PoisonShieldSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseShieldValue = 300;
        [SerializeField, Min(0)] private int _shieldPoisonScalingPercent = 100;
        [SerializeField, Min(0)] private int _baseToxinReductionPercent = 50;
        [SerializeField, Min(0)] private int _reductionPoisonScalingPercent = 100;

        public int BaseShieldValue => _baseShieldValue;
        public int ShieldPoisonScalingPercent => _shieldPoisonScalingPercent;
        public int BaseToxinReductionPercent => _baseToxinReductionPercent;
        public int ReductionPoisonScalingPercent =>
            _reductionPoisonScalingPercent;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Poison)
            {
                errors.Add($"Skill {SkillId}: Poison Shield must be Poison.");
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
            int shieldPoisonScalingPercent,
            int baseToxinReductionPercent,
            int reductionPoisonScalingPercent)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Poison,
                isMapAssignable: true,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost);
            _baseShieldValue = baseShieldValue;
            _shieldPoisonScalingPercent = shieldPoisonScalingPercent;
            _baseToxinReductionPercent = baseToxinReductionPercent;
            _reductionPoisonScalingPercent = reductionPoisonScalingPercent;
        }
#endif
    }
}
