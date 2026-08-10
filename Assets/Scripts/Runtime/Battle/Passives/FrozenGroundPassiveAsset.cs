using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "FrozenGroundPassive",
        menuName = "Pachimon/Passives/Frozen Ground Passive")]
    public sealed class FrozenGroundPassiveAsset : PassiveAsset
    {
        [SerializeField] private Battle.FrozenGroundFieldEffectAsset _fieldEffect;

        public Battle.FrozenGroundFieldEffectAsset FieldEffect => _fieldEffect;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_fieldEffect == null)
            {
                errors?.Add($"Passive {PassiveId}: Field Effect is required.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            Battle.FrozenGroundFieldEffectAsset fieldEffect)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _fieldEffect = fieldEffect;
        }
#endif
    }
}
