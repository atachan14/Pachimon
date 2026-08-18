using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "IceArmorPassive", menuName = "Pachimon/Passives/Ice Armor")]
    public sealed class IceArmorPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _iceScalingPercent = 20;
        public int IceScalingPercent => _iceScalingPercent;
#if UNITY_EDITOR
        public void ConfigureForEditor(int id, string displayName,
            string description, int iceScalingPercent)
        {
            ConfigureBaseForEditor(id, displayName, description);
            _iceScalingPercent = iceScalingPercent;
        }
#endif
    }
}
