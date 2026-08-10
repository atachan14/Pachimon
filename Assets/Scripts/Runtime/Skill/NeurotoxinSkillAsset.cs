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
        [SerializeField, Min(0)] private int _basePoisonStunTicks = 50;
        [SerializeField, Min(0)] private int _poisonStunScalingPercent = 100;
        [SerializeField, Min(0)] private int _baseElectricStunTicks = 50;
        [SerializeField, Min(0)] private int _electricStunScalingPercent = 100;
        [SerializeField, Min(0)] private int _baseToxinValue = 100;
        [SerializeField, Min(0)] private int _toxinScalingPercent = 100;
        [SerializeField] private ToxinStatusAsset _toxinStatus;
        [SerializeField] private StunStatusAsset _stunStatus;

        public int BasePoisonStunTicks => _basePoisonStunTicks;
        public int PoisonStunScalingPercent => _poisonStunScalingPercent;
        public int BaseElectricStunTicks => _baseElectricStunTicks;
        public int ElectricStunScalingPercent => _electricStunScalingPercent;
        public int BaseToxinValue => _baseToxinValue;
        public int ToxinScalingPercent => _toxinScalingPercent;
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
            int basePoisonStunTicks,
            int poisonStunScalingPercent,
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
            _basePoisonStunTicks = basePoisonStunTicks;
            _poisonStunScalingPercent = poisonStunScalingPercent;
            _baseElectricStunTicks = baseElectricStunTicks;
            _electricStunScalingPercent = electricStunScalingPercent;
            _baseToxinValue = baseToxinValue;
            _toxinScalingPercent = toxinScalingPercent;
            _toxinStatus = toxinStatus;
            _stunStatus = stunStatus;
        }
#endif
    }
}
