using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(fileName = "KnockoutStatus", menuName = "Pachimon/Battle/Status/Knockout")]
    public sealed class KnockoutStatusAsset : BattleStatusAsset
    {
        [SerializeField, Min(0)] private int _damageDurationRatio = 10;
        public int DamageDurationRatio => _damageDurationRatio;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            int damageDurationRatio)
        {
            ConfigureDefinitionForEditor(
                BattleStatusId.Knockout,
                displayName,
                description);
            _damageDurationRatio = damageDurationRatio;
        }
#endif
    }
}
