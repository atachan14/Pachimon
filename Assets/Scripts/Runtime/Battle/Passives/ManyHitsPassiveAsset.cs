using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "ManyHitsPassive", menuName = "Pachimon/Passives/Many Hits Passive")]
    public sealed class ManyHitsPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _damagePercent = 150;
        public int DamagePercent => _damagePercent;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId, string displayName, string description,
            int damagePercent)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _damagePercent = damagePercent;
        }
#endif
    }
}
