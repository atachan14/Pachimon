using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "IcePebbleSkill", menuName = "Pachimon/Skills/Ice Pebble")]
    public sealed class IcePebbleSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseDamage = 70;
        [SerializeField, Min(0)] private int _baseChill = 35;
        [SerializeField, Min(0)] private int _baseShield = 70;
        [SerializeField, HideInInspector] private int _iceRatio = 100;
        [SerializeField, Min(1)] private int _shieldDurationTicks = 100;
        [SerializeField] private SlowStatusAsset _chillStatus;
        public int BaseDamage => _baseDamage;
        public int BaseChill => _baseChill;
        public int BaseShield => _baseShield;
        public int IceRatio => AttributeDamageRules.ScalingRatio;
        public int ShieldDurationTicks => _shieldDurationTicks;
        public SlowStatusAsset ChillStatus => _chillStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Ice)
                errors?.Add($"Skill {SkillId}: Ice Pebble must be Ice.");
            if (_chillStatus == null)
                errors?.Add($"Skill {SkillId}: Chill Definition is required.");
        }
#if UNITY_EDITOR
        public void ConfigureForEditor(int skillId, string displayName,
            int recovery, int cooldown, int mana, string description,
            int baseDamage, int baseChill, int baseShield, int iceRatio,
            int shieldDurationTicks, SlowStatusAsset chillStatus)
        {
            base.ConfigureForEditor(skillId, displayName, AllocationType.Ice,
                true, recovery, cooldown, description, mana);
            _baseDamage = baseDamage; _baseChill = baseChill;
            _baseShield = baseShield; _iceRatio = iceRatio;
            _shieldDurationTicks = shieldDurationTicks;
            _chillStatus = chillStatus;
        }
#endif
    }
}
