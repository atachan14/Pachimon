using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "FireVineSkill", menuName = "Pachimon/Skills/Fire Vine Skill")]
    public sealed class FireVineSkillAsset : SkillAsset
    {
        [SerializeField] private FireVineFieldEffectAsset _fieldEffect;
        public FireVineFieldEffectAsset FieldEffect => _fieldEffect;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Leaf)
                errors.Add($"Skill {SkillId}: Fire Vine must be Leaf.");
            if (_fieldEffect == null)
                errors.Add($"Skill {SkillId}: Fire Vine Field Effect is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            int baseRecoveryTicks,
            int baseCooldownTicks,
            int baseManaCost,
            string description,
            FireVineFieldEffectAsset fieldEffect)
        {
            base.ConfigureForEditor(skillId, displayName, AllocationType.Leaf,
                true, baseRecoveryTicks, baseCooldownTicks, description, baseManaCost);
            _fieldEffect = fieldEffect;
        }
#endif
    }
}
