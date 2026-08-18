using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "FirstTouchSkill", menuName = "Pachimon/Skills/First Touch")]
    public sealed class FirstTouchSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseDamage = 100;
        [SerializeField, Min(0)] private int _baseNormalToxinValue = 50;
        [SerializeField, Min(0)] private int _bonusBaseDamage = 150;
        [SerializeField, Min(0)] private int _baseToxinValue = 150;
        [SerializeField, Min(0)] private int _poisonRatio = 100;
        [SerializeField] private ToxinStatusAsset _toxinStatus;

        public int BaseDamage => _baseDamage;
        public int BaseNormalToxinValue => _baseNormalToxinValue;
        public int BonusBaseDamage => _bonusBaseDamage;
        public int BaseToxinValue => _baseToxinValue;
        public int PoisonRatio => _poisonRatio;
        public ToxinStatusAsset ToxinStatus => _toxinStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Poison)
                errors?.Add($"Skill {SkillId}: First Touch must be Poison.");
            if (_toxinStatus == null)
                errors?.Add($"Skill {SkillId}: Toxin Definition is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            int baseRecoveryTicks,
            int baseCooldownTicks,
            int baseManaCost,
            string description,
            int baseDamage,
            int baseNormalToxinValue,
            int bonusBaseDamage,
            int baseToxinValue,
            int poisonRatio,
            ToxinStatusAsset toxinStatus)
        {
            base.ConfigureForEditor(skillId, displayName, AllocationType.Poison,
                true, baseRecoveryTicks, baseCooldownTicks, description,
                baseManaCost);
            _baseDamage = baseDamage;
            _baseNormalToxinValue = baseNormalToxinValue;
            _bonusBaseDamage = bonusBaseDamage;
            _baseToxinValue = baseToxinValue;
            _poisonRatio = poisonRatio;
            _toxinStatus = toxinStatus;
        }
#endif
    }
}
