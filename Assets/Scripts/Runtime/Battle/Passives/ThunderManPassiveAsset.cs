using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "ThunderManPassive", menuName = "Pachimon/Passives/Thunder Man")]
    public sealed class ThunderManPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _speedBonus = 40;
        public int SpeedBonus => _speedBonus;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId, string displayName, string description, int speedBonus)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _speedBonus = speedBonus;
        }
#endif
    }
}
