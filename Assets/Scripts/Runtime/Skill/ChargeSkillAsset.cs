using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "ChargeSkill",
        menuName = "Pachimon/Skills/Charge Skill")]
    public sealed class ChargeSkillAsset : SkillAsset
    {
        [SerializeField, Min(1)] private int _baseStartupTicks = 300;
        [SerializeField] private ChargeStatusAsset _chargeStatus;

        public override int BaseStartupTicks => _baseStartupTicks;
        public ChargeStatusAsset ChargeStatus => _chargeStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Electric)
            {
                errors.Add($"Skill {SkillId}: Charge must be Electric.");
            }
            if (_chargeStatus == null)
            {
                errors.Add($"Skill {SkillId}: Charge Definition is required.");
            }
            if (_baseStartupTicks <= 0)
            {
                errors.Add($"Skill {SkillId}: Charge requires Startup.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            int baseStartupTicks,
            int baseRecoveryTicks,
            int baseCooldownTicks,
            int baseManaCost,
            string description,
            ChargeStatusAsset chargeStatus)
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
            _baseStartupTicks = baseStartupTicks;
            _chargeStatus = chargeStatus;
        }
#endif
    }
}
