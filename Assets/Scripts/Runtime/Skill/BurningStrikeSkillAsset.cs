using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "BurningStrikeSkill", menuName = "Pachimon/Skills/Burning Strike")]
    public sealed class BurningStrikeSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _selfBaseDamage = 100;
        [SerializeField, HideInInspector] private int _selfFireRatio = 100;
        [SerializeField, Min(0)] private int _enemyBaseDamage = 300;
        [SerializeField, HideInInspector] private int _enemyFireRatio = 100;
        [SerializeField, Min(0)] private int _baseBurnValue = 20;
        [SerializeField, HideInInspector] private int _burnFireRatio = 100;
        [SerializeField] private BurnStatusAsset _burnStatus;

        public int SelfBaseDamage => _selfBaseDamage;
        public int SelfFireRatio => AttributeDamageRules.ScalingRatio;
        public int EnemyBaseDamage => _enemyBaseDamage;
        public int EnemyFireRatio => AttributeDamageRules.ScalingRatio;
        public int BaseBurnValue => _baseBurnValue;
        public int BurnFireRatio => AttributeDamageRules.ScalingRatio;
        public BurnStatusAsset BurnStatus => _burnStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Fire)
                errors?.Add($"Skill {SkillId}: Burning Strike must be Fire.");
            if (_burnStatus == null)
                errors?.Add($"Skill {SkillId}: Burn Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            int startup,
            int recovery,
            int cooldown,
            int mana,
            int selfBaseDamage,
            int selfFireRatio,
            int enemyBaseDamage,
            int enemyFireRatio,
            int baseBurnValue,
            int burnFireRatio,
            BurnStatusAsset burnStatus,
            string description)
        {
            base.ConfigureForEditor(skillId, "燃える一撃", AllocationType.Fire,
                true, recovery, cooldown, description, mana, startup);
            _selfBaseDamage = selfBaseDamage;
            _selfFireRatio = selfFireRatio;
            _enemyBaseDamage = enemyBaseDamage;
            _enemyFireRatio = enemyFireRatio;
            _baseBurnValue = baseBurnValue;
            _burnFireRatio = burnFireRatio;
            _burnStatus = burnStatus;
        }
#endif
    }
}
