using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "IceShardSkill",
        menuName = "Pachimon/Skills/Ice Shard Skill")]
    public sealed class IceShardSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _frontBaseDamage = 100;
        [SerializeField, HideInInspector] private int _frontDamageIceRatio = 100;
        [SerializeField, Min(0)] private int _frontBaseChill = 75;
        [SerializeField, HideInInspector] private int _frontChillIceRatio = 100;
        [SerializeField, Min(0)] private int _otherBaseDamage = 50;
        [SerializeField, HideInInspector] private int _otherDamageIceRatio = 100;
        [SerializeField, Min(0)] private int _otherBaseChill = 50;
        [SerializeField, HideInInspector] private int _otherChillIceRatio = 100;
        [SerializeField] private SlowStatusAsset _chillStatus;

        public int FrontBaseDamage => _frontBaseDamage;
        public int FrontDamageIceRatio => AttributeDamageRules.ScalingRatio;
        public int FrontBaseChill => _frontBaseChill;
        public int FrontChillIceRatio => AttributeDamageRules.ScalingRatio;
        public int OtherBaseDamage => _otherBaseDamage;
        public int OtherDamageIceRatio => AttributeDamageRules.ScalingRatio;
        public int OtherBaseChill => _otherBaseChill;
        public int OtherChillIceRatio => AttributeDamageRules.ScalingRatio;
        public SlowStatusAsset ChillStatus => _chillStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Ice)
            {
                errors.Add($"Skill {SkillId}: Ice Shard must be Ice.");
            }
            if (_chillStatus == null)
            {
                errors.Add($"Skill {SkillId}: Chill Status is required.");
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
            int frontBaseDamage,
            int frontDamageIceRatio,
            int frontBaseChill,
            int frontChillIceRatio,
            int otherBaseDamage,
            int otherDamageIceRatio,
            int otherBaseChill,
            int otherChillIceRatio,
            SlowStatusAsset chillStatus)
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
            _frontBaseDamage = frontBaseDamage;
            _frontDamageIceRatio = frontDamageIceRatio;
            _frontBaseChill = frontBaseChill;
            _frontChillIceRatio = frontChillIceRatio;
            _otherBaseDamage = otherBaseDamage;
            _otherDamageIceRatio = otherDamageIceRatio;
            _otherBaseChill = otherBaseChill;
            _otherChillIceRatio = otherChillIceRatio;
            _chillStatus = chillStatus;
        }
#endif
    }
}
