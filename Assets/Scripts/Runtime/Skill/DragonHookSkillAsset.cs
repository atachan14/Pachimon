using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "DragonHookSkill", menuName = "Pachimon/Skills/Dragon Hook Skill")]
    public sealed class DragonHookSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseDragonDamage = 100;
        [SerializeField, HideInInspector] private int _dragonDamageRatio = 100;
        [SerializeField, Min(0)] private int _baseCrankerValue = 30;
        [SerializeField, Min(0)] private int _crankerDragonRatio = 10;
        [SerializeField] private DragonCrankerStatusAsset _crankerStatus;

        public int BaseDragonDamage => _baseDragonDamage;
        public int DragonDamageRatio => AttributeDamageRules.ScalingRatio;
        public int BaseCrankerValue => _baseCrankerValue;
        public int CrankerDragonRatio => _crankerDragonRatio;
        public DragonCrankerStatusAsset CrankerStatus => _crankerStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Dragon)
                errors?.Add($"Skill {SkillId}: Dragon Hook must be Dragon.");
            if (_crankerStatus == null)
                errors?.Add($"Skill {SkillId}: Dragon Cranker Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId, string displayName, int baseRecoveryTicks,
            int baseCooldownTicks, int baseManaCost, string description,
            int baseDragonDamage, int dragonDamageRatio,
            int baseCrankerValue, int crankerDragonRatio,
            DragonCrankerStatusAsset crankerStatus)
        {
            base.ConfigureForEditor(
                skillId, displayName, AllocationType.Dragon, true,
                baseRecoveryTicks, baseCooldownTicks, description, baseManaCost);
            _baseDragonDamage = baseDragonDamage;
            _dragonDamageRatio = dragonDamageRatio;
            _baseCrankerValue = baseCrankerValue;
            _crankerDragonRatio = crankerDragonRatio;
            _crankerStatus = crankerStatus;
        }
#endif
    }
}
