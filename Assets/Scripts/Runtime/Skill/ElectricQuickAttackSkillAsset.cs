using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "ElectricQuickAttackSkill",
        menuName = "Pachimon/Skills/Electric Quick Attack Skill")]
    public sealed class ElectricQuickAttackSkillAsset : SkillAsset
    {
        [FormerlySerializedAs("_electricDamagePercent")]
        [FormerlySerializedAs("_electricBasePower")]
        [SerializeField, Min(0)] private int _electricBaseDamage = 25;
        [FormerlySerializedAs("_windTimingPercent")]
        [FormerlySerializedAs("_fireRecoveryPercent")]
        [SerializeField, Min(0)] private int _fireTimingPercent = 100;

        public int ElectricBaseDamage => _electricBaseDamage;

        public int FireTimingPercent => _fireTimingPercent;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Electric)
            {
                errors.Add($"Skill {SkillId}: Electric Quick Attack must be Electric.");
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
            int electricBaseDamage,
            int fireTimingPercent)
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
            _electricBaseDamage = electricBaseDamage;
            _fireTimingPercent = fireTimingPercent;
        }
#endif
    }
}
