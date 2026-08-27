using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "NeurotoxinSkill",
        menuName = "Pachimon/Skills/Neurotoxin Skill")]
    public sealed class NeurotoxinSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseElectricStunTicks = 50;
        [SerializeField, Min(0)] private int _baseToxinValue = 100;
        [SerializeField] private ToxinStatusAsset _toxinStatus;
        [SerializeField] private StunStatusAsset _stunStatus;

        public int BaseElectricStunTicks => _baseElectricStunTicks;
        public int ElectricStunScalingPercent => AttributeDamageRules.ScalingRatio;
        public int BaseToxinValue => _baseToxinValue;
        public int ToxinScalingPercent => AttributeDamageRules.ScalingRatio;
        public ToxinStatusAsset ToxinStatus => _toxinStatus;
        public StunStatusAsset StunStatus => _stunStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Poison)
            {
                errors.Add($"Skill {SkillId}: Neurotoxin must be Poison.");
            }
            if (_toxinStatus == null)
            {
                errors.Add($"Skill {SkillId}: Toxin Definition is required.");
            }
            if (_stunStatus == null)
            {
                errors.Add($"Skill {SkillId}: Stun Definition is required.");
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
            int baseElectricStunTicks,
            int electricStunScalingPercent,
            int baseToxinValue,
            int toxinScalingPercent,
            ToxinStatusAsset toxinStatus = null,
            StunStatusAsset stunStatus = null)
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
            _baseElectricStunTicks = baseElectricStunTicks;
            _baseToxinValue = baseToxinValue;
            _toxinStatus = toxinStatus;
            _stunStatus = stunStatus;
        }
#endif
    }
}
