using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "FlyingAttackSkill", menuName = "Pachimon/Skills/Flying Attack Skill")]
    public sealed class FlyingAttackSkillAsset : SkillAsset
    {
        [SerializeField, Min(1)] private int _baseStartupTicks = 100;
        [SerializeField, Min(0)] private int _baseWindDamage = 120;
        [SerializeField, Min(0)] private int _windDamageRatio = 100;
        [SerializeField] private FlyingStatusAsset _flyingStatus;

        public override int BaseStartupTicks => _baseStartupTicks;
        public int BaseWindDamage => _baseWindDamage;
        public int WindDamageRatio => _windDamageRatio;
        public FlyingStatusAsset FlyingStatus => _flyingStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Wind)
                errors?.Add($"Skill {SkillId}: Flying Attack must be Wind.");
            if (_flyingStatus == null)
                errors?.Add($"Skill {SkillId}: Flying Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId, string displayName, int baseStartupTicks,
            int baseRecoveryTicks, int baseCooldownTicks, int baseManaCost,
            string description, int baseWindDamage, int windDamageRatio,
            FlyingStatusAsset flyingStatus)
        {
            base.ConfigureForEditor(skillId, displayName, AllocationType.Wind,
                true, baseRecoveryTicks, baseCooldownTicks, description,
                baseManaCost);
            _baseStartupTicks = baseStartupTicks;
            _baseWindDamage = baseWindDamage;
            _windDamageRatio = windDamageRatio;
            _flyingStatus = flyingStatus;
        }
#endif
    }
}
