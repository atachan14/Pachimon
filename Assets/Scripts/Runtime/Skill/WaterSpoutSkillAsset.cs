using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "WaterSpoutSkill",
        menuName = "Pachimon/Skills/Water Spout Skill")]
    public sealed class WaterSpoutSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseAquaDamage = 100;
        [SerializeField, Min(0)] private int _aquaDamageRatio = 100;
        [SerializeField, Min(1)] private int _currentHpDivisor = 2000;

        public int BaseAquaDamage => _baseAquaDamage;
        public int AquaDamageRatio => _aquaDamageRatio;
        public int CurrentHpDivisor => _currentHpDivisor;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Aqua)
                errors.Add($"Skill {SkillId}: Water Spout must be Aqua.");
            if (_currentHpDivisor <= 0)
                errors.Add($"Skill {SkillId}: Current HP Divisor must be positive.");
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
            int currentHpDivisor)
        {
            base.ConfigureForEditor(
                skillId, displayName, AllocationType.Aqua,
                isMapAssignable: true, baseRecoveryTicks, baseCooldownTicks,
                description, baseManaCost);
            _baseAquaDamage = baseAquaDamage;
            _aquaDamageRatio = aquaDamageRatio;
            _currentHpDivisor = currentHpDivisor;
        }
#endif
    }
}
