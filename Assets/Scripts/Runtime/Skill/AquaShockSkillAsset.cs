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
        [SerializeField, Min(0)] private int _electricBasePower = 10;
        [FormerlySerializedAs("_aquaDamagePercent")]
        [SerializeField, Min(0)] private int _aquaBasePower = 10;
        [FormerlySerializedAs("_leakValuePercent")]
        [SerializeField, Min(0)] private int _leakBaseValue = 10;

        public int ElectricBasePower => _electricBasePower;
        public int AquaBasePower => _aquaBasePower;
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
            int electricBasePower,
            int aquaBasePower,
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
            _electricBasePower = electricBasePower;
            _aquaBasePower = aquaBasePower;
            _leakBaseValue = leakBaseValue;
        }
#endif
    }
}
