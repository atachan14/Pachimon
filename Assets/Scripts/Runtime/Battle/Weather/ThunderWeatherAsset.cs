using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "ThunderWeather", menuName = "Pachimon/Weather/Thunder")]
    public sealed class ThunderWeatherAsset : BattleWeatherAsset
    {
        [SerializeField, Min(0)] private int _electricRatioScalingPercent = 10;
        [SerializeField, Min(0)] private int _speedFromElectricRatio = 10;
        [SerializeField, Min(1)] private int _attackIntervalTicks = 150;
        [SerializeField, Min(1)] private int _damageDivisor = 3;

        public int ElectricRatioScalingPercent => _electricRatioScalingPercent;
        public int SpeedFromElectricRatio => _speedFromElectricRatio;
        public int AttackIntervalTicks => _attackIntervalTicks;
        public int DamageDivisor => _damageDivisor;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (WeatherId != BattleWeatherId.Thunder)
                errors?.Add("Thunder Definition must use Thunder ID.");
            if (_attackIntervalTicks <= 0 || _damageDivisor <= 0)
                errors?.Add("Thunder timing and divisor must be positive.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            int electricRatioScalingPercent,
            int speedFromElectricRatio,
            int attackIntervalTicks,
            int damageDivisor,
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleWeatherId.Thunder,
                displayName,
                description,
                icon);
            _electricRatioScalingPercent = electricRatioScalingPercent;
            _speedFromElectricRatio = speedFromElectricRatio;
            _attackIntervalTicks = attackIntervalTicks;
            _damageDivisor = damageDivisor;
        }
#endif
    }
}
