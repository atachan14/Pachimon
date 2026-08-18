using UnityEngine;
using Pachimon.Battle;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "ChillSpreadPassive", menuName = "Pachimon/Passives/Chill Spread")]
    public sealed class ChillSpreadPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _spreadPercent = 150;
        [SerializeField] private SlowStatusAsset _chillStatus;
        public int SpreadPercent => _spreadPercent;
        public SlowStatusAsset ChillStatus => _chillStatus;
#if UNITY_EDITOR
        public void ConfigureForEditor(int id, string displayName,
            string description, int spreadPercent, SlowStatusAsset chillStatus)
        {
            ConfigureBaseForEditor(id, displayName, description);
            _spreadPercent = spreadPercent;
            _chillStatus = chillStatus;
        }
#endif
    }
}
