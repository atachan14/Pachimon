using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "RainManPassive",
        menuName = "Pachimon/Passives/Rain Man Passive")]
    public sealed class RainManPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _baseSpeedPercent = 100;
        [SerializeField, Min(0)] private int _rainValueRatio = 3;

        public int BaseSpeedPercent => _baseSpeedPercent;
        public int RainValueRatio => _rainValueRatio;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int baseSpeedPercent,
            int rainValueRatio)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _baseSpeedPercent = baseSpeedPercent;
            _rainValueRatio = rainValueRatio;
        }
#endif
    }
}
