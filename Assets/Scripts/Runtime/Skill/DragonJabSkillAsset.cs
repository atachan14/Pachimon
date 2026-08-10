using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "DragonJabSkill", menuName = "Pachimon/Skills/Dragon Jab Skill")]
    public sealed class DragonJabSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseDragonDamage = 100;
        [SerializeField, Min(0)] private int _dragonDamageRatio = 100;
        [SerializeField, Min(0)] private int _oneTwoValue = 30;
        [SerializeField] private OneTwoStatusAsset _oneTwoStatus;

        public int BaseDragonDamage => _baseDragonDamage;
        public int DragonDamageRatio => _dragonDamageRatio;
        public int OneTwoValue => _oneTwoValue;
        public OneTwoStatusAsset OneTwoStatus => _oneTwoStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Dragon)
            {
                errors?.Add($"Skill {SkillId}: Dragon Jab must be Dragon.");
            }
            if (_oneTwoStatus == null)
            {
                errors?.Add($"Skill {SkillId}: One Two Status is required.");
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
            int baseDragonDamage,
            int dragonDamageRatio,
            int oneTwoValue,
            OneTwoStatusAsset oneTwoStatus)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Dragon,
                isMapAssignable: true,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost);
            _baseDragonDamage = baseDragonDamage;
            _dragonDamageRatio = dragonDamageRatio;
            _oneTwoValue = oneTwoValue;
            _oneTwoStatus = oneTwoStatus;
        }
#endif
    }
}
