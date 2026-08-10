using System.Collections.Generic;
using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "SweetSciencePassive", menuName = "Pachimon/Passives/Sweet Science Passive")]
    public sealed class SweetSciencePassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _speedGain = 20;
        [SerializeField] private SweetScienceStatusAsset _speedStatus;

        public int SpeedGain => _speedGain;
        public SweetScienceStatusAsset SpeedStatus => _speedStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_speedStatus == null)
                errors?.Add($"Passive {PassiveId}: Sweet Science Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int speedGain,
            SweetScienceStatusAsset speedStatus)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _speedGain = speedGain;
            _speedStatus = speedStatus;
        }
#endif
    }
}
