using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "FrostArrowSkill", menuName = "Pachimon/Skills/Frost Arrow")]
    public sealed class FrostArrowSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseDamage = 100;
        [SerializeField, Min(0)] private int _baseChill = 30;
        [SerializeField, HideInInspector] private int _iceRatio = 100;
        [SerializeField] private SlowStatusAsset _chillStatus;
        public int BaseDamage => _baseDamage;
        public int BaseChill => _baseChill;
        public int IceRatio => AttributeDamageRules.ScalingRatio;
        public SlowStatusAsset ChillStatus => _chillStatus;
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_chillStatus == null) errors?.Add($"Skill {SkillId}: Chill is required.");
        }
#if UNITY_EDITOR
        public void ConfigureForEditor(int id, string displayName, int recovery,
            int cooldown, int mana, string description, int baseDamage,
            int baseChill, int iceRatio, SlowStatusAsset chill)
        {
            base.ConfigureForEditor(id, displayName, AllocationType.Ice, true,
                recovery, cooldown, description, mana);
            _baseDamage = baseDamage; _baseChill = baseChill;
            _iceRatio = iceRatio; _chillStatus = chill;
        }
#endif
    }
}
