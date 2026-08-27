using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "ElectromagneticCannonSkill",
        menuName = "Pachimon/Skills/Electromagnetic Cannon Skill")]
    public sealed class ElectromagneticCannonSkillAsset : SkillAsset
    {
        [FormerlySerializedAs("_basePower")]
        [SerializeField, Min(0)] private int _baseDamage = 400;

        public int BaseDamage => _baseDamage;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Electric)
            {
                errors.Add(
                    $"Skill {SkillId}: Electromagnetic Cannon must be Electric.");
            }

            if (BaseStartupTicks <= 0)
            {
                errors.Add(
                    $"Skill {SkillId}: Electromagnetic Cannon requires Startup.");
            }

            if (_baseDamage <= 0)
            {
                errors.Add(
                    $"Skill {SkillId}: Base Damage must be positive.");
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
            int baseDamage)
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
            SetBaseStartupTicksForEditor(baseStartupTicks);
            _baseDamage = baseDamage;
        }
#endif
    }
}
