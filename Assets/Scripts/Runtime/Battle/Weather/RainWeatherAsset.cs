using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "RainWeather",
        menuName = "Pachimon/Weather/Rain")]
    public sealed class RainWeatherAsset : BattleWeatherAsset
    {
        [SerializeField, Min(0)] private int _aquaRatioScalingPercent = 10;
        [SerializeField, Min(0)] private int _fireRatioScalingPercent = 20;
        [FormerlySerializedAs("_rainLeakValueRatio")]
        [FormerlySerializedAs("_rainLeakValueRatioPerTick")]
        [SerializeField, Min(0)] private int _leakValueRatioPerTick = 7;
        [SerializeField, Min(0)] private int _snowChillBaseValue = 20;
        [SerializeField, Min(0)] private int _snowChillTemperatureRatio = 100;
        [SerializeField] private SlowStatusAsset _chillStatus;
        [SerializeField] private string _snowDisplayName = "雪";

        public int AquaRatioScalingPercent => _aquaRatioScalingPercent;
        public int FireRatioScalingPercent => _fireRatioScalingPercent;
        public int LeakValueRatioPerTick => _leakValueRatioPerTick;
        public int SnowChillBaseValue => _snowChillBaseValue;
        public int SnowChillTemperatureRatio => _snowChillTemperatureRatio;
        public SlowStatusAsset ChillStatus => _chillStatus;
        public string SnowDisplayName => string.IsNullOrWhiteSpace(_snowDisplayName)
            ? "Snow"
            : _snowDisplayName;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (WeatherId != BattleWeatherId.Rain)
            {
                errors?.Add("Rain Definition must use Rain ID.");
            }
            if (_chillStatus == null)
            {
                errors?.Add("Rain Definition requires a Chill Status.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            int aquaRatioScalingPercent,
            int fireRatioScalingPercent,
            int leakValueRatioPerTick,
            int snowChillBaseValue,
            int snowChillTemperatureRatio,
            SlowStatusAsset chillStatus,
            string snowDisplayName = "雪",
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleWeatherId.Rain,
                displayName,
                description,
                icon);
            _aquaRatioScalingPercent = aquaRatioScalingPercent;
            _fireRatioScalingPercent = fireRatioScalingPercent;
            _leakValueRatioPerTick = leakValueRatioPerTick;
            _snowChillBaseValue = snowChillBaseValue;
            _snowChillTemperatureRatio = snowChillTemperatureRatio;
            _chillStatus = chillStatus;
            _snowDisplayName = snowDisplayName;
        }
#endif
    }
}
