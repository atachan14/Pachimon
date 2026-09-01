using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "ChainVinesSkill", menuName = "Pachimon/Skills/Chain Vines Skill")]
    public sealed class ChainVinesSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseLeafDamage = 70;
        [SerializeField, Min(0)] private int _baseSlow = 25;
        [SerializeField, Min(0)] private int _baseChainCount = 2;
        [FormerlySerializedAs("_addChainGainUnits")]
        [SerializeField, Min(1)] private int _chainGain = 1;
        [SerializeField] private SlowStatusAsset _slowStatus;

        public int BaseLeafDamage => _baseLeafDamage;
        public int BaseSlow => _baseSlow;
        public int SlowLeafRatio => AttributeDamageRules.ScalingRatio;
        public int BaseChainCount => _baseChainCount;
        public int ChainGain => _chainGain;
        public SlowStatusAsset SlowStatus => _slowStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Leaf) errors.Add($"Skill {SkillId}: Chain Vines must be Leaf.");
            if (_slowStatus == null) errors.Add($"Skill {SkillId}: Slow Status is required.");
            if (_chainGain <= 0) errors.Add($"Skill {SkillId}: Chain gain must be positive.");
        }
    }
}
