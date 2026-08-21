using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "WaterCutterSkill",
        menuName = "Pachimon/Skills/Water Cutter Skill")]
    public sealed class WaterCutterSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseAquaDamage = 100;
        [SerializeField, HideInInspector] private int _aquaDamageRatio = 100;
        [SerializeField, Min(0)] private int _basePenetrationPercent = 20;
        [SerializeField, Min(0)] private int _windPenetrationRatio = 100;

        public int BaseAquaDamage => _baseAquaDamage;
        public int AquaDamageRatio => AttributeDamageRules.ScalingRatio;
        public int BasePenetrationPercent => _basePenetrationPercent;
        public int WindPenetrationRatio => _windPenetrationRatio;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Aqua)
                errors.Add($"Skill {SkillId}: Water Cutter must be Aqua.");
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
            int basePenetrationPercent,
            int windPenetrationRatio)
        {
            base.ConfigureForEditor(
                skillId, displayName, AllocationType.Aqua,
                isMapAssignable: true, baseRecoveryTicks, baseCooldownTicks,
                description, baseManaCost);
            _baseAquaDamage = baseAquaDamage;
            _aquaDamageRatio = aquaDamageRatio;
            _basePenetrationPercent = basePenetrationPercent;
            _windPenetrationRatio = windPenetrationRatio;
        }
#endif
    }
}
