using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "ChainBurnSkill",
        menuName = "Pachimon/Skills/Chain Burn Skill")]
    public sealed class ChainBurnSkillAsset : SkillAsset
    {
        [FormerlySerializedAs("_basePower")]
        [SerializeField, Min(0)] private int _baseDamage = 80;
        [SerializeField, HideInInspector] private int _fireScalingPercent = 100;
        [SerializeField, Min(0)] private int _baseChainCount = 1;
        [FormerlySerializedAs("_addChainGainUnits")]
        [SerializeField, Min(1)] private int _chainGain = 1;

        public int BaseDamage => _baseDamage;
        public int FireScalingPercent => AttributeDamageRules.ScalingRatio;
        public int BaseChainCount => _baseChainCount;
        public int ChainGain => _chainGain;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Fire)
            {
                errors.Add($"Skill {SkillId}: Chain Burn must be Fire.");
            }
            if (_chainGain <= 0)
            {
                errors.Add($"Skill {SkillId}: Chain gain must be positive.");
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
            int baseDamage,
            int fireScalingPercent,
            int baseChainCount,
            int chainGain)
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
            _baseDamage = baseDamage;
            _fireScalingPercent = fireScalingPercent;
            _baseChainCount = baseChainCount;
            _chainGain = chainGain;
        }
#endif
    }
}
