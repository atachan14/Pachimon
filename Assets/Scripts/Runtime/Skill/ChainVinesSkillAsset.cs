using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "ChainVinesSkill", menuName = "Pachimon/Skills/Chain Vines Skill")]
    public sealed class ChainVinesSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseLeafDamage = 70;
        [SerializeField, Min(0)] private int _baseSlow = 25;
        [SerializeField, Min(0)] private int _baseChainCount = 2;
        [SerializeField, Min(1)] private int _addChainGainUnits = 100;
        [SerializeField] private SlowStatusAsset _slowStatus;

        public int BaseLeafDamage => _baseLeafDamage;
        public int BaseSlow => _baseSlow;
        public int SlowLeafRatio => AttributeDamageRules.ScalingRatio;
        public int BaseChainCount => _baseChainCount;
        public int AddChainGainUnits => _addChainGainUnits;
        public SlowStatusAsset SlowStatus => _slowStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Leaf) errors.Add($"Skill {SkillId}: Chain Vines must be Leaf.");
            if (_slowStatus == null) errors.Add($"Skill {SkillId}: Slow Status is required.");
        }
    }
}
