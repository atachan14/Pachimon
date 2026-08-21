using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "MuddyWaterSkill",
        menuName = "Pachimon/Skills/Muddy Water Skill")]
    public sealed class MuddyWaterSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseAquaDamage = 100;
        [SerializeField, HideInInspector] private int _aquaDamageRatio = 100;
        [SerializeField, Min(0)] private int _baseSlow = 100;
        [SerializeField, HideInInspector] private int _poisonSlowRatio = 100;
        [SerializeField] private SlowStatusAsset _slowStatus;

        public int BaseAquaDamage => _baseAquaDamage;
        public int AquaDamageRatio => AttributeDamageRules.ScalingRatio;
        public int BaseSlow => _baseSlow;
        public int PoisonSlowRatio => AttributeDamageRules.ScalingRatio;
        public SlowStatusAsset SlowStatus => _slowStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Aqua)
                errors.Add($"Skill {SkillId}: Muddy Water must be Aqua.");
            if (_slowStatus == null)
                errors.Add($"Skill {SkillId}: Slow Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            int baseRecoveryTicks,
            int baseCooldownTicks,
            int baseManaCost,
            string description,
            int baseAquaDamage,
            int aquaDamageRatio,
            int baseSlow,
            int poisonSlowRatio,
            SlowStatusAsset slowStatus)
        {
            base.ConfigureForEditor(
                skillId, displayName, AllocationType.Aqua,
                isMapAssignable: true, baseRecoveryTicks, baseCooldownTicks,
                description, baseManaCost);
            _baseAquaDamage = baseAquaDamage;
            _aquaDamageRatio = aquaDamageRatio;
            _baseSlow = baseSlow;
            _poisonSlowRatio = poisonSlowRatio;
            _slowStatus = slowStatus;
        }
#endif
    }
}
