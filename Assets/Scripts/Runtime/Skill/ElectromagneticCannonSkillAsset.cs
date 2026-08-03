using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "ElectromagneticCannonSkill",
        menuName = "Pachimon/Skills/Electromagnetic Cannon Skill")]
    public sealed class ElectromagneticCannonSkillAsset : SkillAsset
    {
        [SerializeField, Min(1)] private int _baseStartupTicks = 300;
        [SerializeField, Min(0)] private int _basePower = 400;

        public override int BaseStartupTicks => _baseStartupTicks;

        public int BasePower => _basePower;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Electric)
            {
                errors.Add(
                    $"Skill {SkillId}: Electromagnetic Cannon must be Electric.");
            }

            if (_baseStartupTicks <= 0)
            {
                errors.Add(
                    $"Skill {SkillId}: Electromagnetic Cannon requires Startup.");
            }

            if (_basePower <= 0)
            {
                errors.Add(
                    $"Skill {SkillId}: Base Power must be positive.");
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
            int basePower)
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
            _basePower = basePower;
        }
#endif
    }
}
