using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using Pachimon.Run;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "PoisonMistSkill", menuName = "Pachimon/Skills/Poison Mist")]
    public sealed class PoisonMistSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseMistValue = 100;
        [SerializeField, HideInInspector] private int _poisonValueRatio = 100;
        [SerializeField, Min(1)] private int _baseDurationTicks = 75;
        [SerializeField, HideInInspector] private int _aquaDurationRatio = 100;
        [SerializeField, Min(0)] private int _baseMinimumValue = 20;
        [SerializeField, HideInInspector] private int _windMinimumValueRatio = 100;
        [SerializeField] private PoisonMistFieldEffectAsset _fieldEffect;

        public int BaseMistValue => _baseMistValue;
        public int PoisonValueRatio => AttributeDamageRules.ScalingRatio;
        public int BaseDurationTicks => _baseDurationTicks;
        public int AquaDurationRatio => AttributeDamageRules.ScalingRatio;
        public int BaseMinimumValue => _baseMinimumValue;
        public int WindMinimumValueRatio => AttributeDamageRules.ScalingRatio;
        public PoisonMistFieldEffectAsset FieldEffect => _fieldEffect;

        public int CalculateMistValue(int poison) =>
            Mathf.Max(1, SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    _baseMistValue, poison, PoisonValueRatio)));

        public int CalculateDurationTicks(int aqua) =>
            Mathf.Max(1, SignedStatMath.FloorNonNegative(
                SignedStatMath.ScaleFromBase(
                    _baseDurationTicks, aqua, AquaDurationRatio)));

        public int CalculateMinimumValue(int poison, int wind) =>
            Mathf.Min(
                CalculateMistValue(poison),
                Mathf.Max(0, SignedStatMath.FloorNonNegative(
                    SignedStatMath.ScaleFromBase(
                        _baseMinimumValue, wind, WindMinimumValueRatio))));

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Poison)
                errors?.Add($"Skill {SkillId}: Poison Mist must be Poison.");
            if (_fieldEffect == null)
                errors?.Add($"Skill {SkillId}: Poison Mist Definition is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            int baseRecoveryTicks,
            int baseCooldownTicks,
            int baseManaCost,
            string description,
            int baseMistValue,
            int poisonValueRatio,
            int baseDurationTicks,
            int aquaDurationRatio,
            int baseMinimumValue,
            int windMinimumValueRatio,
            PoisonMistFieldEffectAsset fieldEffect)
        {
            base.ConfigureForEditor(skillId, displayName, AllocationType.Poison,
                true, baseRecoveryTicks, baseCooldownTicks, description,
                baseManaCost);
            _baseMistValue = baseMistValue;
            _poisonValueRatio = poisonValueRatio;
            _baseDurationTicks = baseDurationTicks;
            _aquaDurationRatio = aquaDurationRatio;
            _baseMinimumValue = baseMinimumValue;
            _windMinimumValueRatio = windMinimumValueRatio;
            _fieldEffect = fieldEffect;
        }
#endif
    }
}
