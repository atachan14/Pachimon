using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "LifeWaterPassive",
        menuName = "Pachimon/Passives/Life Water Passive")]
    public sealed class LifeWaterPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _baseRecoveryRatio = 20;
        [SerializeField, Min(0)] private int _aquaRecoveryRatio = 5;

        public int BaseRecoveryRatio => _baseRecoveryRatio;
        public int AquaRecoveryRatio => _aquaRecoveryRatio;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int baseRecoveryRatio,
            int aquaRecoveryRatio)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _baseRecoveryRatio = baseRecoveryRatio;
            _aquaRecoveryRatio = aquaRecoveryRatio;
        }
#endif
    }
}
