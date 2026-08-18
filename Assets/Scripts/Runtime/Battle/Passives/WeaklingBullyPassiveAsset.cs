using System.Collections.Generic;
using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "WeaklingBullyPassive",
        menuName = "Pachimon/Passives/Weakling Bully Passive")]
    public sealed class WeaklingBullyPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _damagePercent = 130;
        [SerializeField] private int _speedBonus = 30;
        [SerializeField, Min(1)] private int _speedDurationTicks = 100;
        [SerializeField] private WeaklingBullySpeedStatusAsset _speedStatus;

        public int DamagePercent => _damagePercent;
        public int SpeedBonus => _speedBonus;
        public int SpeedDurationTicks => _speedDurationTicks;
        public WeaklingBullySpeedStatusAsset SpeedStatus => _speedStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_damagePercent < 0)
                errors.Add($"Passive {PassiveId}: Damage Percent cannot be negative.");
            if (_speedDurationTicks <= 0)
                errors.Add($"Passive {PassiveId}: Speed Duration must be positive.");
            if (_speedStatus == null)
                errors.Add($"Passive {PassiveId}: Speed Status is required.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int damagePercent,
            int speedBonus,
            int speedDurationTicks,
            WeaklingBullySpeedStatusAsset speedStatus)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _damagePercent = damagePercent;
            _speedBonus = speedBonus;
            _speedDurationTicks = speedDurationTicks;
            _speedStatus = speedStatus;
        }
#endif
    }
}
