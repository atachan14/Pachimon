using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    public enum BattleWeatherId
    {
        Temperature = 1,
        Rain = 2,
        Thunder = 3,
        Wind = 4,
        Moisture = 5,
        Plasma = 6,
    }

    public abstract class BattleWeatherAsset : ScriptableObject
    {
        [SerializeField] private BattleWeatherId _weatherId;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;

        public BattleWeatherId WeatherId => _weatherId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;

        public virtual void CollectValidationErrors(ICollection<string> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            if (string.IsNullOrWhiteSpace(_displayName))
            {
                errors.Add($"Weather {_weatherId}: display name is missing.");
            }
        }

#if UNITY_EDITOR
        protected void ConfigureDefinitionForEditor(
            BattleWeatherId weatherId,
            string displayName,
            string description,
            Sprite icon = null)
        {
            _weatherId = weatherId;
            _displayName = displayName;
            _description = description;
            _icon = icon;
        }
#endif
    }

    public sealed class BattleEnvironmentDefinitions
    {
        public BattleEnvironmentDefinitions(
            SunnyWeatherAsset temperature,
            RainWeatherAsset precipitation,
            WindWeatherAsset wind,
            PairedAttributeEnvironmentAsset moisture,
            PairedAttributeEnvironmentAsset plasma)
        {
            Temperature = temperature;
            Precipitation = precipitation;
            Wind = wind;
            Moisture = moisture;
            Plasma = plasma;
        }

        public SunnyWeatherAsset Temperature { get; }
        public RainWeatherAsset Precipitation { get; }
        public WindWeatherAsset Wind { get; }
        public PairedAttributeEnvironmentAsset Moisture { get; }
        public PairedAttributeEnvironmentAsset Plasma { get; }
    }
}
