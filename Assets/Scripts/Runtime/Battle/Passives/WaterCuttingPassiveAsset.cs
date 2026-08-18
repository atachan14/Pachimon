using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "WaterCuttingPassive",
        menuName = "Pachimon/Passives/Water Cutting Passive")]
    public sealed class WaterCuttingPassiveAsset : PassiveAsset
    {
#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
        }
#endif
    }
}
