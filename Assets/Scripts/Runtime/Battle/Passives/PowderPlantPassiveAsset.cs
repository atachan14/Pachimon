using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "PowderPlantPassive", menuName = "Pachimon/Passives/Powder Plant Passive")]
    public sealed class PowderPlantPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _leafIncreasePerApplication = 10;
        public int LeafIncreasePerApplication => _leafIncreasePerApplication;
    }
}
