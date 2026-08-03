using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "ChargeSkill",
        menuName = "Pachimon/Skills/Charge Skill")]
    public sealed class ChargeSkillAsset : SkillAsset
    {
        [SerializeField, Min(1)] private int _chargingDurationPercent = 400;
        [SerializeField, Min(0)] private int _chargingResistBonusPercent = 40;
        [SerializeField, Min(0)] private int _chargingElectricPercent = 50;
        [SerializeField, Min(1)] private int _chargedDurationPercent = 200;
        [SerializeField, Min(0)] private int _chargedElectricPercent = 150;
        [SerializeField, Min(0)] private int _chargedSpeedPercent = 100;

        public int ChargingDurationPercent => _chargingDurationPercent;
        public int ChargingResistBonusPercent => _chargingResistBonusPercent;
        public int ChargingElectricPercent => _chargingElectricPercent;
        public int ChargedDurationPercent => _chargedDurationPercent;
        public int ChargedElectricPercent => _chargedElectricPercent;
        public int ChargedSpeedPercent => _chargedSpeedPercent;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Electric)
            {
                errors.Add($"Skill {SkillId}: Charge must be Electric.");
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
            int chargingDurationPercent,
            int chargingResistBonusPercent,
            int chargingElectricPercent,
            int chargedDurationPercent,
            int chargedElectricPercent,
            int chargedSpeedPercent)
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
            _chargingDurationPercent = chargingDurationPercent;
            _chargingResistBonusPercent = chargingResistBonusPercent;
            _chargingElectricPercent = chargingElectricPercent;
            _chargedDurationPercent = chargedDurationPercent;
            _chargedElectricPercent = chargedElectricPercent;
            _chargedSpeedPercent = chargedSpeedPercent;
        }
#endif
    }
}
