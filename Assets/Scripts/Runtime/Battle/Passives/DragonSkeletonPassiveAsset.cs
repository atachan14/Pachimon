using Pachimon.Run;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "DragonSkeletonPassive", menuName = "Pachimon/Passives/Dragon Skeleton Passive")]
    public sealed class DragonSkeletonPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _dragonFromSpeedRatio = 20;
        [SerializeField, Min(0)] private int _speedFromDragonRatio = 20;

        public int DragonFromSpeedRatio => _dragonFromSpeedRatio;
        public int SpeedFromDragonRatio => _speedFromDragonRatio;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int dragonFromSpeedRatio,
            int speedFromDragonRatio)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _dragonFromSpeedRatio = dragonFromSpeedRatio;
            _speedFromDragonRatio = speedFromDragonRatio;
        }
#endif
    }
}
