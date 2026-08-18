using System.Collections.Generic;
using Pachimon.Data;

namespace Pachimon.Skills
{
    public abstract class MachineExclusiveSkillAsset : SkillAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (SkillId < SkillIdRanges.FirstMachineExclusiveId
                || SkillId > SkillIdRanges.LastMachineExclusiveId)
            {
                errors?.Add($"Skill {SkillId}: Machine-exclusive ID must be 1000-1999.");
            }

            if (IsMapAssignable)
                errors?.Add($"Skill {SkillId}: Machine-exclusive Skill cannot be Map-assignable.");
        }

#if UNITY_EDITOR
        protected void ConfigureMachineForEditor(
            int skillId,
            string displayName,
            int startup,
            int recovery,
            int cooldown,
            int mana,
            string description,
            AllocationType allocationType = AllocationType.Unassigned)
        {
            ConfigureForEditor(
                skillId,
                displayName,
                allocationType,
                false,
                recovery,
                cooldown,
                description,
                mana,
                startup);
        }
#endif
    }
}
