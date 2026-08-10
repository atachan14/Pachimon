using System.Collections.Generic;
using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "TargetStatusDamagePassive",
        menuName = "Pachimon/Passives/Target Status Damage Passive")]
    public sealed class TargetStatusDamagePassiveAsset : PassiveAsset
    {
        [SerializeField] private BattleStatusCategory _targetCategory;
        [SerializeField, Min(0)] private int _damagePercent = 130;

        public BattleStatusCategory TargetCategory => _targetCategory;
        public int DamagePercent => _damagePercent;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_targetCategory == BattleStatusCategory.None)
            {
                errors.Add(
                    $"Passive {PassiveId}: Target Status Category is required.");
            }
            if (_damagePercent < 0)
            {
                errors.Add(
                    $"Passive {PassiveId}: Damage percent cannot be negative.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            BattleStatusCategory targetCategory,
            int damagePercent)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _targetCategory = targetCategory;
            _damagePercent = damagePercent;
        }
#endif
    }
}
