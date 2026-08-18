using System.Collections.Generic;
using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "CloneTechniqueSkill", menuName = "Pachimon/Skills/Machine/Clone Technique")]
    public sealed class CloneTechniqueSkillAsset : MachineExclusiveSkillAsset
    {
        [SerializeField, Min(1)] private int _stacks = 1;
        [SerializeField] private CloneStatusAsset _status;

        public int Stacks => _stacks;
        public CloneStatusAsset Status => _status;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_status == null) errors?.Add($"Skill {SkillId}: Clone Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int id,
            int stacks,
            CloneStatusAsset status,
            string description)
        {
            ConfigureMachineForEditor(id, "分身の術", 100, 50, 200, 40, description);
            _stacks = stacks;
            _status = status;
        }
#endif
    }
}
