using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    public abstract class InitialAttributeDamageSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseDamage = 100;
        [SerializeField, HideInInspector] private int _damageRatio = 100;

        public int BaseDamage => _baseDamage;
        public int DamageRatio => AttributeDamageRules.ScalingRatio;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_baseDamage <= 0)
            {
                errors?.Add($"Skill {SkillId}: Base Damage must be positive.");
            }
        }

#if UNITY_EDITOR
        protected void ConfigureInitialSkillForEditor(
            int skillId,
            string displayName,
            AllocationType allocationType,
            int recoveryTicks,
            int cooldownTicks,
            int manaCost,
            string description,
            int baseDamage,
            int damageRatio)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                allocationType,
                true,
                recoveryTicks,
                cooldownTicks,
                description,
                manaCost);
            _baseDamage = baseDamage;
            _damageRatio = damageRatio;
        }
#endif
    }
}
