using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "SunnyManPassive",
        menuName = "Pachimon/Passives/Sunny Man Passive")]
    public sealed class SunnyManPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _speedPercent = 130;

        public int SpeedPercent => _speedPercent;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int speedPercent)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _speedPercent = speedPercent;
        }
#endif
    }
}
