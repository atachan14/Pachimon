using System.Collections.Generic;
using Pachimon.Skills;
using UnityEngine;

namespace Pachimon.Items
{
    [CreateAssetMenu(
        fileName = "SkillMachineItem",
        menuName = "Pachimon/Items/Skill Machine Item")]
    public sealed class SkillMachineItemAsset : ItemAsset
    {
        [SerializeField] private SkillAsset _skill;

        public SkillAsset Skill => _skill;

        public int SkillId => _skill != null ? _skill.SkillId : 0;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_skill == null)
            {
                errors.Add($"{name}: Skill reference is missing.");
                return;
            }

            var expectedItemId = ItemIds.GetSkillMachineItemId(_skill.SkillId);
            if (ItemId != expectedItemId)
            {
                errors.Add(
                    $"{name}: Item ID must be {expectedItemId} "
                    + $"for Skill {_skill.SkillId}.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureSkillForEditor(SkillAsset skill)
        {
            _skill = skill;
        }
#endif
    }
}
