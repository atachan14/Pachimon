using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "SmogSkill",
        menuName = "Pachimon/Skills/Smog Skill")]
    public sealed class SmogSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseFieldValue = 300;
        [SerializeField, HideInInspector] private int _poisonScalingPercent = 100;
        [SerializeField] private SmogFieldEffectAsset _fieldEffect;

        public int BaseFieldValue => _baseFieldValue;
        public int PoisonScalingPercent => AttributeDamageRules.ScalingRatio;
        public SmogFieldEffectAsset FieldEffect => _fieldEffect;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Poison)
            {
                errors.Add($"Skill {SkillId}: Smog must be Poison.");
            }
            if (_fieldEffect == null)
            {
                errors.Add($"Skill {SkillId}: Smog Field Effect is required.");
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
            int baseFieldValue,
            int poisonScalingPercent,
            SmogFieldEffectAsset fieldEffect = null)
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
            _baseFieldValue = baseFieldValue;
            _poisonScalingPercent = poisonScalingPercent;
            _fieldEffect = fieldEffect;
        }
#endif
    }
}
