using System.Collections.Generic;
using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "IntangibilitySkill", menuName = "Pachimon/Skills/Machine/Intangibility")]
    public sealed class IntangibilitySkillAsset : MachineExclusiveSkillAsset
    {
        [SerializeField] private IntangibleStatusAsset _status;

        public IntangibleStatusAsset Status => _status;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_status == null)
                errors?.Add($"Skill {SkillId}: Intangible Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int id,
            IntangibleStatusAsset status,
            string description)
        {
            ConfigureMachineForEditor(id, "無形化", 200, 0, 600, 0, description);
            _status = status;
        }
#endif
    }
}
