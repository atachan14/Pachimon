using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "WaterBlessingPassive",
        menuName = "Pachimon/Passives/Water Blessing Passive")]
    public sealed class WaterBlessingPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _baseHealingRatio = 15;
        [SerializeField, Min(0)] private int _aquaHealingRatio = 10;

        public int BaseHealingRatio => _baseHealingRatio;
        public int AquaHealingRatio => _aquaHealingRatio;
    }
}
