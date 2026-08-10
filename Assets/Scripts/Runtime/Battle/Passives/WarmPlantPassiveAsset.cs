using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "WarmPlantPassive", menuName = "Pachimon/Passives/Warm Plant Passive")]
    public sealed class WarmPlantPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _temperatureSpeedRatio = 30;
        public int TemperatureSpeedRatio => _temperatureSpeedRatio;
    }
}
