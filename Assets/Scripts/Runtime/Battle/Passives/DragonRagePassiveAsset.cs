using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "DragonRagePassive", menuName = "Pachimon/Passives/Dragon Rage Passive")]
    public sealed class DragonRagePassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _penetrationRatio = 20;
        public int PenetrationRatio => _penetrationRatio;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId, string displayName, string description,
            int penetrationRatio)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _penetrationRatio = penetrationRatio;
        }
#endif
    }
}
