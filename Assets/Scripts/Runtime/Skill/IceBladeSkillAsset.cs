using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "IceBladeSkill",
        menuName = "Pachimon/Skills/Ice Blade Skill")]
    public sealed class IceBladeSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseDurationTicks = 200;
        [SerializeField, Min(0)] private int _scalingDurationTicks = 100;
        [SerializeField, Min(0)] private int _iceDurationRatio = 100;
        [SerializeField] private IceBladeFieldEffectAsset _fieldEffect;

        public int BaseDurationTicks => _baseDurationTicks;
        public int ScalingDurationTicks => _scalingDurationTicks;
        public int IceDurationRatio => _iceDurationRatio;
        public IceBladeFieldEffectAsset FieldEffect => _fieldEffect;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Ice)
            {
                errors?.Add($"Skill {SkillId}: Ice Blade must be Ice.");
            }
            if (_fieldEffect == null)
            {
                errors?.Add($"Skill {SkillId}: Field Effect is required.");
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
            int baseDurationTicks,
            int scalingDurationTicks,
            int iceDurationRatio,
            IceBladeFieldEffectAsset fieldEffect)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Ice,
                isMapAssignable: true,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost);
            _baseDurationTicks = baseDurationTicks;
            _scalingDurationTicks = scalingDurationTicks;
            _iceDurationRatio = iceDurationRatio;
            _fieldEffect = fieldEffect;
        }
#endif
    }
}
