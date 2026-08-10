using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "BurnStatus",
        menuName = "Pachimon/Battle Status/Burn")]
    public sealed class BurnStatusAsset : BattleStatusAsset
    {
#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleStatusId.Burn,
                displayName,
                description,
                icon);
        }
#endif
    }
}
