using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "ElectricExplosionSkill",
        menuName = "Pachimon/Skills/Electric Explosion Skill")]
    public sealed class ElectricExplosionSkillAsset : SkillAsset
    {
        [FormerlySerializedAs("_electricDamagePercent")]
        [SerializeField, Min(0)] private int _basePower = 50;
        [SerializeField, HideInInspector] private int _electricScalingPercent = 100;
        [SerializeField, HideInInspector] private int _fireScalingPercent = 100;
        [SerializeField, Min(0)] private int _penetrationPercentAtFire100 = 20;

        public int BasePower => _basePower;

        public int ElectricScalingPercent => AttributeDamageRules.ScalingRatio;

        public int FireScalingPercent => AttributeDamageRules.ScalingRatio;

        public int PenetrationPercentAtFire100 => _penetrationPercentAtFire100;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Electric)
            {
                errors.Add($"Skill {SkillId}: Electric Explosion must be Electric.");
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
            int electricScalingPercent,
            int fireScalingPercent,
            int penetrationPercentAtFire100)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Electric,
                isMapAssignable: true,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost);
            _basePower = basePower;
            _electricScalingPercent = electricScalingPercent;
            _fireScalingPercent = fireScalingPercent;
            _penetrationPercentAtFire100 = penetrationPercentAtFire100;
        }
#endif
    }
}
