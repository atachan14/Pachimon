using System.Collections.Generic;
using Pachimon.Reward;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "PairedAttributeEnvironment",
        menuName = "Pachimon/Environment/Paired Attribute")]
    public sealed class PairedAttributeEnvironmentAsset : BattleWeatherAsset
    {
        [SerializeField] private string _negativeDisplayName;
        [SerializeField, TextArea] private string _negativeDescription;
        [SerializeField] private PachimonAttribute _positiveAttribute;
        [SerializeField] private PachimonAttribute _negativeAttribute;
        [SerializeField, Min(0)] private int _positiveAmplificationPercent = 100;
        [SerializeField, Min(0)] private int _positiveReductionPercent = 100;
        [SerializeField, Min(0)] private int _negativeAmplificationPercent = 100;
        [SerializeField, Min(0)] private int _negativeReductionPercent = 100;
        [SerializeField, Min(0)] private float _damageChangePercent = 0.5f;

        public string NegativeDisplayName => string.IsNullOrWhiteSpace(_negativeDisplayName)
            ? DisplayName
            : _negativeDisplayName;
        public string NegativeDescription => string.IsNullOrWhiteSpace(
            _negativeDescription)
                ? Description
                : _negativeDescription;
        public PachimonAttribute PositiveAttribute => _positiveAttribute;
        public PachimonAttribute NegativeAttribute => _negativeAttribute;
        public int PositiveAmplificationPercent => _positiveAmplificationPercent;
        public int PositiveReductionPercent => _positiveReductionPercent;
        public int NegativeAmplificationPercent => _negativeAmplificationPercent;
        public int NegativeReductionPercent => _negativeReductionPercent;
        public float DamageChangePercent => _damageChangePercent;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (WeatherId != BattleWeatherId.Moisture
                && WeatherId != BattleWeatherId.Plasma)
            {
                errors?.Add("Paired Attribute Environment must use Moisture or Plasma ID.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            BattleWeatherId weatherId,
            string displayName,
            string negativeDisplayName,
            string description,
            PachimonAttribute positiveAttribute,
            PachimonAttribute negativeAttribute,
            int positiveAmplificationPercent,
            int positiveReductionPercent,
            int negativeAmplificationPercent,
            int negativeReductionPercent,
            float damageChangePercent,
            Sprite icon = null,
            string negativeDescription = null)
        {
            ConfigureDefinitionForEditor(weatherId, displayName, description, icon);
            _negativeDisplayName = negativeDisplayName;
            _positiveAttribute = positiveAttribute;
            _negativeAttribute = negativeAttribute;
            _positiveAmplificationPercent = positiveAmplificationPercent;
            _positiveReductionPercent = positiveReductionPercent;
            _negativeAmplificationPercent = negativeAmplificationPercent;
            _negativeReductionPercent = negativeReductionPercent;
            _damageChangePercent = damageChangePercent;
            if (negativeDescription != null)
            {
                _negativeDescription = negativeDescription;
            }
        }
#endif
    }
}
