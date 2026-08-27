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
        [SerializeField] private string _sunnyDisplayName = "晴天";
        [SerializeField, TextArea] private string _snowDescription;
        [SerializeField, TextArea] private string _sunnyDescription;
        [SerializeField, Min(0)] private int _snowIceRatioScalingPercent = 10;
        [SerializeField, Min(0)] private int _snowFireRatioScalingPercent = 20;
        [SerializeField, Min(0)] private int _sunnyFireRatioScalingPercent = 10;
        [SerializeField, Min(0)] private int _sunnyAquaRatioScalingPercent = 20;
        [SerializeField, Min(1)] private int _environmentIntervalTicks = 10;
        [SerializeField, Min(0)] private int _environmentChangePercent = 1;

        public int AquaRatioScalingPercent => _aquaRatioScalingPercent;
        public int FireRatioScalingPercent => _fireRatioScalingPercent;
        public int LeakValueRatioPerTick => _leakValueRatioPerTick;
        public int SnowChillBaseValue => _snowChillBaseValue;
        public int SnowChillTemperatureRatio => _snowChillTemperatureRatio;
        public SlowStatusAsset ChillStatus => _chillStatus;
        public string SnowDisplayName => string.IsNullOrWhiteSpace(_snowDisplayName)
            ? "Snow"
            : _snowDisplayName;
        public string SunnyDisplayName => string.IsNullOrWhiteSpace(_sunnyDisplayName)
            ? "Sunny"
            : _sunnyDisplayName;
        public string SnowDescription => string.IsNullOrWhiteSpace(
            _snowDescription)
                ? Description
                : _snowDescription;
        public string SunnyDescription => string.IsNullOrWhiteSpace(
            _sunnyDescription)
                ? Description
                : _sunnyDescription;
        public int SnowIceRatioScalingPercent => _snowIceRatioScalingPercent;
        public int SnowFireRatioScalingPercent => _snowFireRatioScalingPercent;
        public int SunnyFireRatioScalingPercent => _sunnyFireRatioScalingPercent;
        public int SunnyAquaRatioScalingPercent => _sunnyAquaRatioScalingPercent;
        public int EnvironmentIntervalTicks => _environmentIntervalTicks;
        public int EnvironmentChangePercent => _environmentChangePercent;

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
            Sprite icon = null,
            string snowDescription = null,
            string sunnyDescription = null)
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
            if (snowDescription != null)
            {
                _snowDescription = snowDescription;
            }
            if (sunnyDescription != null)
            {
                _sunnyDescription = sunnyDescription;
            }
        }
#endif
    }
}
