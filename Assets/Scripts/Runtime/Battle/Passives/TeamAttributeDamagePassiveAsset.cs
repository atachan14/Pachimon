using System.Collections.Generic;
using Pachimon.Reward;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "TeamAttributeDamagePassive",
        menuName = "Pachimon/Passives/Team Attribute Damage Passive")]
    public sealed class TeamAttributeDamagePassiveAsset : PassiveAsset
    {
        [SerializeField] private PachimonAttribute _attribute;
        [SerializeField, Min(0)] private int _damagePercent = 115;

        public PachimonAttribute Attribute => _attribute;
        public int DamagePercent => _damagePercent;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
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
            PachimonAttribute attribute,
            int damagePercent)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _attribute = attribute;
            _damagePercent = damagePercent;
        }
#endif
    }
}
