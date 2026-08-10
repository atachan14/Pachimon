using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "TemperatureEnvironment",
        menuName = "Pachimon/Environment/Temperature")]
    public sealed class SunnyWeatherAsset : BattleWeatherAsset
    {
        [SerializeField, Min(0)] private int _fireRatioScalingPercent = 10;
        [SerializeField, Min(0)] private int _aquaRatioScalingPercent = 20;
        [SerializeField, Min(0)] private int _iceRatioScalingPercent = 20;
        [SerializeField, Min(0)] private int _coldFireRatioScalingPercent = 20;
        [SerializeField, Min(0)] private int _coldIceRatioScalingPercent = 10;

        public int FireRatioScalingPercent => _fireRatioScalingPercent;
        public int AquaRatioScalingPercent => _aquaRatioScalingPercent;
        public int IceRatioScalingPercent => _iceRatioScalingPercent;
        public int ColdFireRatioScalingPercent => _coldFireRatioScalingPercent;
        public int ColdIceRatioScalingPercent => _coldIceRatioScalingPercent;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (WeatherId != BattleWeatherId.Temperature)
            {
                errors?.Add("Temperature Definition must use Temperature ID.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            int fireRatioScalingPercent,
            int aquaRatioScalingPercent,
            int iceRatioScalingPercent,
            int coldFireRatioScalingPercent,
            int coldIceRatioScalingPercent,
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleWeatherId.Temperature,
                displayName,
                description,
                icon);
            _fireRatioScalingPercent = fireRatioScalingPercent;
            _aquaRatioScalingPercent = aquaRatioScalingPercent;
            _iceRatioScalingPercent = iceRatioScalingPercent;
            _coldFireRatioScalingPercent = coldFireRatioScalingPercent;
            _coldIceRatioScalingPercent = coldIceRatioScalingPercent;
        }
#endif
    }
}
