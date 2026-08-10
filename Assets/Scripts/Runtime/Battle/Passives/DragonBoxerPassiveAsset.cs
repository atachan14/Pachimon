using System.Collections.Generic;
using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(fileName = "DragonBoxerPassive", menuName = "Pachimon/Passives/Dragon Boxer Passive")]
    public sealed class DragonBoxerPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(1)] private int _stackGain = 10;
        [SerializeField, Min(0)] private int _damagePercentPerStack = 1;
        [SerializeField] private DragonBoxerStatusAsset _stackStatus;

        public int StackGain => _stackGain;
        public int DamagePercentPerStack => _damagePercentPerStack;
        public DragonBoxerStatusAsset StackStatus => _stackStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_stackGain <= 0)
            {
                errors?.Add($"Passive {PassiveId}: Stack Gain must be positive.");
            }
            if (_stackStatus == null)
            {
                errors?.Add($"Passive {PassiveId}: Stack Status is required.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int stackGain,
            int damagePercentPerStack,
            DragonBoxerStatusAsset stackStatus)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _stackGain = stackGain;
            _damagePercentPerStack = damagePercentPerStack;
            _stackStatus = stackStatus;
        }
#endif
    }
}
