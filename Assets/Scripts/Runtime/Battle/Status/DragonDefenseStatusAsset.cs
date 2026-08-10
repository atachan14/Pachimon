using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "DragonDefenseStatus", menuName = "Pachimon/Battle/Status/Dragon Defense")]
    public sealed class DragonDefenseStatusAsset : BattleStatusAsset
    {
#if UNITY_EDITOR
        public void ConfigureForEditor(string displayName, string description)
        {
            ConfigureDefinitionForEditor(
                BattleStatusId.DragonDefense,
                displayName,
                description);
        }
#endif
    }
}
