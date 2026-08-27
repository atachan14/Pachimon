using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "AquaShockSkill",
        menuName = "Pachimon/Skills/Aqua Shock Skill")]
    public sealed class AquaShockSkillAsset : SkillAsset
    {
        [FormerlySerializedAs("_electricDamagePercent")]
        [FormerlySerializedAs("_electricBasePower")]
        [SerializeField, Min(0)] private int _electricBaseDamage = 10;
        [FormerlySerializedAs("_aquaDamagePercent")]
        [FormerlySerializedAs("_aquaBasePower")]
        [SerializeField, Min(0)] private int _aquaBaseDamage = 10;
        [FormerlySerializedAs("_leakValuePercent")]
        [SerializeField, Min(0)] private int _leakBaseValue = 10;

        public int ElectricBaseDamage => _electricBaseDamage;
        public int AquaBaseDamage => _aquaBaseDamage;
        public int LeakBaseValue => _leakBaseValue;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Electric)
            {
                errors.Add($"Skill {SkillId}: Aqua Shock must be Electric.");
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
            int aquaBaseDamage,
            int leakBaseValue)
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
            _aquaBaseDamage = aquaBaseDamage;
            _leakBaseValue = leakBaseValue;
        }
#endif
    }
}
