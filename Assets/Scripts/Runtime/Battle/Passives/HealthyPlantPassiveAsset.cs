using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "HealthyPlantPassive", menuName = "Pachimon/Passives/Healthy Plant")]
    public sealed class HealthyPlantPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _baseHealingRatio = 15;
        [SerializeField, Min(0)] private int _leafHealingRatio = 10;

        public int BaseHealingRatio => _baseHealingRatio;
        public int LeafHealingRatio => _leafHealingRatio;
    }
}
