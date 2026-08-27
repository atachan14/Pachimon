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
        [FormerlySerializedAs("_basePower")]
        [SerializeField, Min(0)] private int _baseDamage = 50;
        [SerializeField, HideInInspector] private int _electricScalingPercent = 100;
        [FormerlySerializedAs("_penetrationPercentAtFire100")]
        [SerializeField, Min(0)] private int _firePenetrationRatio = 25;

        public int BaseDamage => _baseDamage;

        public int ElectricScalingPercent => AttributeDamageRules.ScalingRatio;

        public int FirePenetrationRatio => _firePenetrationRatio;

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
            int baseDamage,
            int electricScalingPercent,
            int firePenetrationRatio)
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
            _baseDamage = baseDamage;
            _electricScalingPercent = electricScalingPercent;
            _firePenetrationRatio = firePenetrationRatio;
        }
#endif
    }
}
