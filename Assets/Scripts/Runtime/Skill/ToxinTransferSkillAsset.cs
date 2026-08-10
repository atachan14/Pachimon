using System.Collections.Generic;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "ToxinTransferSkill",
        menuName = "Pachimon/Skills/Toxin Transfer Skill")]
    public sealed class ToxinTransferSkillAsset : SkillAsset
    {
        [SerializeField, Range(0, 100)] private int _removalPercent = 50;
        [SerializeField, Min(0)] private int _applicationPercent = 200;

        public int RemovalPercent => _removalPercent;
        public int ApplicationPercent => _applicationPercent;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Poison)
            {
                errors.Add($"Skill {SkillId}: Toxin Transfer must be Poison.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            int baseRecoveryTicks,
            int baseCooldownTicks,
            int baseManaCost,
            string description,
            int removalPercent,
            int applicationPercent)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Poison,
                isMapAssignable: true,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost);
            _removalPercent = removalPercent;
            _applicationPercent = applicationPercent;
        }
#endif
    }
}
