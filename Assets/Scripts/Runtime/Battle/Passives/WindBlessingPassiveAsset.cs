using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "WindBlessingPassive", menuName = "Pachimon/Passives/Wind Blessing")]
    public sealed class WindBlessingPassiveAsset : PassiveAsset
    {
        [SerializeField, Range(0, 100)] private int _sharedShieldPercent = 20;
        [SerializeField, Range(0, 100)] private int _durationPercent = 100;

        public int SharedShieldPercent => _sharedShieldPercent;
        public int DurationPercent => _durationPercent;
    }
}
