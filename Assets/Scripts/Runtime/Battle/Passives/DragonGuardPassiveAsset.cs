using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "DragonGuardPassive", menuName = "Pachimon/Passives/Dragon Guard Passive")]
    public sealed class DragonGuardPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _resistFromDragonRatio = 20;
        public int ResistFromDragonRatio => _resistFromDragonRatio;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId, string displayName, string description,
            int resistFromDragonRatio)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _resistFromDragonRatio = resistFromDragonRatio;
        }
#endif
    }
}
