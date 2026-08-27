using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "BackfireSkill",
        menuName = "Pachimon/Skills/Backfire Skill")]
    public sealed class BackfireSkillAsset : SkillAsset
    {
        [FormerlySerializedAs("_basePower")]
        [SerializeField, Min(0)] private int _baseDamage = 100;
        [SerializeField, HideInInspector] private int _fireScalingPercent = 100;
        [FormerlySerializedAs("_basePenetrationPercent")]
        [SerializeField, Min(0)] private int _baseAttributeFixedPenetration = 10;
        [FormerlySerializedAs("_poisonScalingPercent")]
        [SerializeField, Min(0)] private int _poisonPenetrationRatio = 100;

        public int BaseDamage => _baseDamage;
        public int FireScalingPercent => AttributeDamageRules.ScalingRatio;
        public int BaseAttributeFixedPenetration => _baseAttributeFixedPenetration;
        public int PoisonPenetrationRatio => _poisonPenetrationRatio;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Fire)
            {
                errors.Add($"Skill {SkillId}: Backfire must be Fire.");
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
            int baseAttributeFixedPenetration,
            int poisonPenetrationRatio)
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
            _baseAttributeFixedPenetration = baseAttributeFixedPenetration;
            _poisonPenetrationRatio = poisonPenetrationRatio;
        }
#endif
    }
}
