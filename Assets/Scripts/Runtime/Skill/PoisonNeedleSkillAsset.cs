using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "PoisonNeedleSkill", menuName = "Pachimon/Skills/Poison Needle")]
    public sealed class PoisonNeedleSkillAsset : InitialAttributeDamageSkillAsset
    {
        [SerializeField, Min(0)] private int _toxinBaseValue = 150;
        [SerializeField] private ToxinStatusAsset _toxinStatus;

        public int ToxinBaseValue => _toxinBaseValue;
        public int ToxinRatio => AttributeDamageRules.ScalingRatio;
        public ToxinStatusAsset ToxinStatus => _toxinStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_toxinStatus == null)
                errors?.Add($"Skill {SkillId}: Toxin Definition is required.");
        }
    }
}
