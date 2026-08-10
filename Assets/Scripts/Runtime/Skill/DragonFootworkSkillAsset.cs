using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "DragonFootworkSkill", menuName = "Pachimon/Skills/Dragon Footwork Skill")]
    public sealed class DragonFootworkSkillAsset : SkillAsset
    {
        [SerializeField] private FootworkStatusAsset _footworkStatus;

        public FootworkStatusAsset FootworkStatus => _footworkStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Dragon)
                errors?.Add($"Skill {SkillId}: Dragon Footwork must be Dragon.");
            if (_footworkStatus == null)
                errors?.Add($"Skill {SkillId}: Footwork Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            int baseRecoveryTicks,
            int baseCooldownTicks,
            int baseManaCost,
            string description,
            FootworkStatusAsset footworkStatus)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Dragon,
                true,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost);
            _footworkStatus = footworkStatus;
        }
#endif
    }
}
