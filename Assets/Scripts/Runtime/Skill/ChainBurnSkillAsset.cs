using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "ChainBurnSkill",
        menuName = "Pachimon/Skills/Chain Burn Skill")]
    public sealed class ChainBurnSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _basePower = 80;
        [SerializeField, HideInInspector] private int _fireScalingPercent = 100;
        [SerializeField, Min(0)] private int _baseChainCount = 1;
        [SerializeField, Min(1)] private int _addChainGainUnits = 50;

        public int BasePower => _basePower;
        public int FireScalingPercent => AttributeDamageRules.ScalingRatio;
        public int BaseChainCount => _baseChainCount;
        public int AddChainGainUnits => _addChainGainUnits;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Fire)
            {
                errors.Add($"Skill {SkillId}: Chain Burn must be Fire.");
            }
            if (_addChainGainUnits <= 0)
            {
                errors.Add(
                    $"Skill {SkillId}: Add Chain gain units must be positive.");
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
            int basePower,
            int fireScalingPercent,
            int baseChainCount,
            int addChainGainUnits)
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
            _basePower = basePower;
            _fireScalingPercent = fireScalingPercent;
            _baseChainCount = baseChainCount;
            _addChainGainUnits = addChainGainUnits;
        }
#endif
    }
}
