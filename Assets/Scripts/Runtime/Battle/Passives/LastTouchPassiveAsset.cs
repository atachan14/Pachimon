using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "LastTouchPassive", menuName = "Pachimon/Passives/Last Touch")]
    public sealed class LastTouchPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _poisonExecutionRatio = 4;

        public int PoisonExecutionRatio => _poisonExecutionRatio;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int poisonExecutionRatio)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _poisonExecutionRatio = poisonExecutionRatio;
        }
#endif
    }
}
