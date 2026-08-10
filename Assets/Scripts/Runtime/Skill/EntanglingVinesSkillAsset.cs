using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "EntanglingVinesSkill", menuName = "Pachimon/Skills/Entangling Vines Skill")]
    public sealed class EntanglingVinesSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseStun = 100;
        [SerializeField, Min(0)] private int _stunLeafRatio = 100;
        [SerializeField] private StunStatusAsset _stunStatus;
        public int BaseStun => _baseStun;
        public int StunLeafRatio => _stunLeafRatio;
        public StunStatusAsset StunStatus => _stunStatus;
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Leaf) errors.Add($"Skill {SkillId}: Entangling Vines must be Leaf.");
            if (_stunStatus == null) errors.Add($"Skill {SkillId}: Stun Status is required.");
        }
    }
}
