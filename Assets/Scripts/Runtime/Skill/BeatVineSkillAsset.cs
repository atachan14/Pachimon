using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "BeatVineSkill", menuName = "Pachimon/Skills/Beat Vine Skill")]
    public sealed class BeatVineSkillAsset : SkillAsset
    {
        [SerializeField] private BeatVineFieldEffectAsset _fieldEffect;
        public BeatVineFieldEffectAsset FieldEffect => _fieldEffect;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Leaf)
                errors.Add($"Skill {SkillId}: Beat Vine must be Leaf.");
            if (_fieldEffect == null)
                errors.Add($"Skill {SkillId}: Beat Vine Field Effect is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            int baseRecoveryTicks,
            int baseCooldownTicks,
            int baseManaCost,
            string description,
            BeatVineFieldEffectAsset fieldEffect)
        {
            base.ConfigureForEditor(skillId, displayName, AllocationType.Leaf,
                true, baseRecoveryTicks, baseCooldownTicks, description, baseManaCost);
            _fieldEffect = fieldEffect;
        }
#endif
    }
}
