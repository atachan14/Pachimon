using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "DragonCrankerStatus", menuName = "Pachimon/Battle/Status/Dragon Cranker")]
    public sealed class DragonCrankerStatusAsset : BattleStatusAsset
    {
#if UNITY_EDITOR
        public void ConfigureForEditor(string displayName, string description)
        {
            ConfigureDefinitionForEditor(
                BattleStatusId.DragonCranker,
                displayName,
                description);
        }
#endif
    }
}
