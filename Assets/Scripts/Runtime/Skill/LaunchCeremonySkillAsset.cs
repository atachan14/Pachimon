using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "LaunchCeremonySkill",
        menuName = "Pachimon/Skills/Launch Ceremony Skill")]
    public sealed class LaunchCeremonySkillAsset : SkillAsset
    {
        [SerializeField] private LaunchCeremonyStatusAsset _statusDefinition;

        public LaunchCeremonyStatusAsset StatusDefinition => _statusDefinition;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Aqua)
            {
                errors?.Add($"Skill {SkillId}: Launch Ceremony must be Aqua.");
            }
            if (_statusDefinition == null)
            {
                errors?.Add($"Skill {SkillId}: Status Definition is required.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            int baseRecoveryTicks,
            int baseCooldownTicks,
            string description,
            LaunchCeremonyStatusAsset statusDefinition)
        {
            base.ConfigureForEditor(
                skillId,
                displayName,
                AllocationType.Aqua,
                isMapAssignable: true,
                baseRecoveryTicks,
                baseCooldownTicks,
                description,
                baseManaCost: 0);
            _statusDefinition = statusDefinition;
        }
#endif
    }
}
