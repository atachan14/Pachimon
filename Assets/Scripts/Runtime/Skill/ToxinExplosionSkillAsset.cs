using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "ToxinExplosionSkill",
        menuName = "Pachimon/Skills/Toxin Explosion Skill")]
    public sealed class ToxinExplosionSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _toxinConversionPercent = 100;
        [SerializeField, Min(0)] private int _basePoisonPower = 50;
        [SerializeField, Min(0)] private int _poisonScalingPercent = 100;
        [SerializeField, Min(0)] private int _baseFirePower = 50;
        [SerializeField, Min(0)] private int _fireScalingPercent = 100;

        public int ToxinConversionPercent => _toxinConversionPercent;
        public int BasePoisonPower => _basePoisonPower;
        public int PoisonScalingPercent => _poisonScalingPercent;
        public int BaseFirePower => _baseFirePower;
        public int FireScalingPercent => _fireScalingPercent;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Poison)
            {
                errors.Add($"Skill {SkillId}: Toxin Explosion must be Poison.");
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
            int toxinConversionPercent,
            int basePoisonPower,
            int poisonScalingPercent,
            int baseFirePower,
            int fireScalingPercent)
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
            _toxinConversionPercent = toxinConversionPercent;
            _basePoisonPower = basePoisonPower;
            _poisonScalingPercent = poisonScalingPercent;
            _baseFirePower = baseFirePower;
            _fireScalingPercent = fireScalingPercent;
        }
#endif
    }
}
