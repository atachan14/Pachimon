using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "SturdyPlantPassive", menuName = "Pachimon/Passives/Sturdy Plant Passive")]
    public sealed class SturdyPlantPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _leafResistBonusRatio = 60;
        public int LeafResistBonusRatio => _leafResistBonusRatio;
    }
}
