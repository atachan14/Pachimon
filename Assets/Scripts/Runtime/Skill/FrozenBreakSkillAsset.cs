using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "FrozenBreakSkill",
        menuName = "Pachimon/Skills/Frozen Break Skill")]
    public sealed class FrozenBreakSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _lowHpRecoveryTicks = 1;
        [SerializeField, Min(0)] private int _baseIceDamage = 100;
        [SerializeField, Min(0)] private int _iceDamageRatio = 100;
        [SerializeField, Min(1)] private int _baseDuration = 70;
        [SerializeField, Min(0)] private int _durationIceRatio = 40;
        [SerializeField, Min(0)] private int _baseHealPerTick = 1;
        [SerializeField, Min(0)] private int _healIceRatio = 50;
        [SerializeField] private FreezeStatusAsset _freezeStatus;
        [SerializeField] private FrozenBreakStatusAsset _selfStatus;

        public int LowHpRecoveryTicks => _lowHpRecoveryTicks;
        public int BaseIceDamage => _baseIceDamage;
        public int IceDamageRatio => _iceDamageRatio;
        public int BaseDuration => _baseDuration;
        public int DurationIceRatio => _durationIceRatio;
        public int BaseHealPerTick => _baseHealPerTick;
        public int HealIceRatio => _healIceRatio;
        public FreezeStatusAsset FreezeStatus => _freezeStatus;
        public FrozenBreakStatusAsset SelfStatus => _selfStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Ice)
            {
                errors.Add($"Skill {SkillId}: Frozen Break must be Ice.");
            }
            if (_freezeStatus == null || _selfStatus == null)
            {
                errors.Add($"Skill {SkillId}: both Status Definitions are required.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            int highHpRecoveryTicks,
            int lowHpRecoveryTicks,
            int baseCooldownTicks,
            int baseManaCost,
            string description,
            int baseIceDamage,
            int iceDamageRatio,
            int baseDuration,
            int durationIceRatio,
            int baseHealPerTick,
            int healIceRatio,
            FreezeStatusAsset freezeStatus,
            FrozenBreakStatusAsset selfStatus)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Ice,
                isMapAssignable: true,
                highHpRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost);
            _lowHpRecoveryTicks = lowHpRecoveryTicks;
            _baseIceDamage = baseIceDamage;
            _iceDamageRatio = iceDamageRatio;
            _baseDuration = baseDuration;
            _durationIceRatio = durationIceRatio;
            _baseHealPerTick = baseHealPerTick;
            _healIceRatio = healIceRatio;
            _freezeStatus = freezeStatus;
            _selfStatus = selfStatus;
        }
#endif
    }
}
