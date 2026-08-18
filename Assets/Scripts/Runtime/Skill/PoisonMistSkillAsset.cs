using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;

namespace Pachimon.Skills
{
    [CreateAssetMenu(fileName = "PoisonMistSkill", menuName = "Pachimon/Skills/Poison Mist")]
    public sealed class PoisonMistSkillAsset : SkillAsset
    {
        [SerializeField, Min(0)] private int _baseMistValue = 100;
        [SerializeField, Min(0)] private int _poisonValueRatio = 100;
        [SerializeField, Min(0)] private int _aquaDurationRatio = 75;
        [SerializeField, Min(0)] private int _windDurationRatio = 25;
        [SerializeField] private PoisonMistFieldEffectAsset _fieldEffect;

        public int BaseMistValue => _baseMistValue;
        public int PoisonValueRatio => _poisonValueRatio;
        public int AquaDurationRatio => _aquaDurationRatio;
        public int WindDurationRatio => _windDurationRatio;
        public PoisonMistFieldEffectAsset FieldEffect => _fieldEffect;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Poison)
                errors?.Add($"Skill {SkillId}: Poison Mist must be Poison.");
            if (_fieldEffect == null)
                errors?.Add($"Skill {SkillId}: Poison Mist Definition is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int skillId,
            string displayName,
            int baseRecoveryTicks,
            int baseCooldownTicks,
            int baseManaCost,
            string description,
            int baseMistValue,
            int poisonValueRatio,
            int aquaDurationRatio,
            int windDurationRatio,
            PoisonMistFieldEffectAsset fieldEffect)
        {
            base.ConfigureForEditor(skillId, displayName, AllocationType.Poison,
                true, baseRecoveryTicks, baseCooldownTicks, description,
                baseManaCost);
            _baseMistValue = baseMistValue;
            _poisonValueRatio = poisonValueRatio;
            _aquaDurationRatio = aquaDurationRatio;
            _windDurationRatio = windDurationRatio;
            _fieldEffect = fieldEffect;
        }
#endif
    }
}
