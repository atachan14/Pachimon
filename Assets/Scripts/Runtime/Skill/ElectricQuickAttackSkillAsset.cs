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
        [SerializeField, Min(0)] private int _electricBasePower = 25;
        [FormerlySerializedAs("_fireDamagePercent")]
        [SerializeField, Min(0)] private int _fireBasePower = 10;
        [SerializeField, Min(0)] private int _windTimingPercent = 100;

        public int ElectricBasePower => _electricBasePower;

        public int FireBasePower => _fireBasePower;

        public int WindTimingPercent => _windTimingPercent;

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
            int electricBasePower,
            int fireBasePower,
            int windTimingPercent)
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
            _electricBasePower = electricBasePower;
            _fireBasePower = fireBasePower;
            _windTimingPercent = windTimingPercent;
        }
#endif
    }
}
