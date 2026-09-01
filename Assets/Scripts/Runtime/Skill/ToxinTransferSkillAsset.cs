using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pachimon.Skills
{
    [CreateAssetMenu(
        fileName = "ToxinTransferSkill",
        menuName = "Pachimon/Skills/Toxin Transfer Skill")]
    public sealed class ToxinTransferSkillAsset : SkillAsset
    {
        [SerializeField, Range(0, 100)] private int _removalPercent = 50;
        [SerializeField, Min(0)] private int _baseToxinValue = 150;
        [SerializeField, HideInInspector] private int _poisonScalingPercent = 100;
        [FormerlySerializedAs("_applicationPercent")]
        [SerializeField, Min(0)] private int _baseApplicationPercent = 100;
        [SerializeField, Min(0)] private int _scaledApplicationBasePercent = 20;
        [SerializeField, HideInInspector]
        private int _applicationPoisonScalingPercent = 100;
        [SerializeField] private ToxinStatusAsset _toxinStatus;

        public int RemovalPercent => _removalPercent;
        public int BaseToxinValue => _baseToxinValue;
        public int PoisonScalingPercent => AttributeDamageRules.ScalingRatio;
        public int BaseApplicationPercent => _baseApplicationPercent;
        public int ScaledApplicationBasePercent => _scaledApplicationBasePercent;
        public int ApplicationPoisonScalingPercent =>
            AttributeDamageRules.ScalingRatio;
        public ToxinStatusAsset ToxinStatus => _toxinStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (AllocationType != AllocationType.Poison)
            {
                errors.Add($"Skill {SkillId}: Toxin Transfer must be Poison.");
            }
            if (_toxinStatus == null)
            {
                errors.Add($"Skill {SkillId}: Toxin Status is required.");
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
            int baseToxinValue,
            int poisonScalingPercent,
            int baseApplicationPercent,
            int scaledApplicationBasePercent,
            int applicationPoisonScalingPercent,
            ToxinStatusAsset toxinStatus)
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
            _baseToxinValue = baseToxinValue;
            _poisonScalingPercent = poisonScalingPercent;
            _baseApplicationPercent = baseApplicationPercent;
            _scaledApplicationBasePercent = scaledApplicationBasePercent;
            _applicationPoisonScalingPercent = applicationPoisonScalingPercent;
            _toxinStatus = toxinStatus;
        }
#endif
    }
}
