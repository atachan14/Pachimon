using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "EntanglingVinePassive", menuName = "Pachimon/Passives/Entangling Vine Passive")]
    public sealed class EntanglingVinePassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _leafSlowRatio = 100;
        public int LeafSlowRatio => _leafSlowRatio;
    }
}
