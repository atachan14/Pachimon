using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "WindWeather",
        menuName = "Pachimon/Weather/Wind")]
    public sealed class WindWeatherAsset : BattleWeatherAsset
    {
        [SerializeField, Min(0)] private int _windRatioScalingPercent = 10;
        [SerializeField, Min(0)] private int _speedFromWindRatio = 20;
        [SerializeField, Min(0)] private int _rainEffectRatioScalingPercent = 10;

        public int WindRatioScalingPercent => _windRatioScalingPercent;
        public int SpeedFromWindRatio => _speedFromWindRatio;
        public int RainEffectRatioScalingPercent =>
            _rainEffectRatioScalingPercent;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (WeatherId != BattleWeatherId.Wind)
            {
                errors?.Add("Wind Definition must use Wind ID.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            int windRatioScalingPercent,
            int speedFromWindRatio,
            int rainEffectRatioScalingPercent,
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleWeatherId.Wind,
                displayName,
                description,
                icon);
            _windRatioScalingPercent = windRatioScalingPercent;
            _speedFromWindRatio = speedFromWindRatio;
            _rainEffectRatioScalingPercent = rainEffectRatioScalingPercent;
        }
#endif
    }
}
