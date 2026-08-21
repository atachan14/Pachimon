using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "FireBarrierSkill",
        menuName = "Pachimon/Skills/Fire Barrier Skill")]
    public sealed class FireBarrierSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseValue = 100;
        [SerializeField, HideInInspector] private int _fireValueRatio = 100;
        [SerializeField] private FireBarrierFieldEffectAsset _fieldEffect;

        public int BaseValue => _baseValue;
        public int FireValueRatio => AttributeDamageRules.ScalingRatio;
        public FireBarrierFieldEffectAsset FieldEffect => _fieldEffect;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Fire)
            {
                errors.Add($"Skill {SkillId}: Fire Barrier must be Fire.");
            }
            if (_fieldEffect == null)
            {
                errors.Add($"Skill {SkillId}: Fire Barrier Field Effect is required.");
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
            int baseValue,
            int fireValueRatio,
            FireBarrierFieldEffectAsset fieldEffect)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Fire,
                isMapAssignable: true,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost);
            _baseValue = baseValue;
            _fireValueRatio = fireValueRatio;
            _fieldEffect = fieldEffect;
        }
#endif
    }
}
