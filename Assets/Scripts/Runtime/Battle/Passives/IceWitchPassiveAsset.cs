using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Passives
{
    [CreateAssetMenu(
        fileName = "IceWitchPassive",
        menuName = "Pachimon/Passives/Ice Witch Passive")]
    public sealed class IceWitchPassiveAsset : PassiveAsset
    {
        [SerializeField, Min(0)] private int _baseIceDamage = 200;
        [SerializeField, Min(0)] private int _iceDamageRatio = 100;

        public int BaseIceDamage => _baseIceDamage;
        public int IceDamageRatio => _iceDamageRatio;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (_baseIceDamage < 0)
            {
                errors?.Add($"Passive {PassiveId}: Base Ice Damage cannot be negative.");
            }
            if (_iceDamageRatio < 0)
            {
                errors?.Add($"Passive {PassiveId}: Ice Damage Ratio cannot be negative.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int passiveId,
            string displayName,
            string description,
            int baseIceDamage,
            int iceDamageRatio)
        {
            ConfigureBaseForEditor(passiveId, displayName, description);
            _baseIceDamage = baseIceDamage;
            _iceDamageRatio = iceDamageRatio;
        }
#endif
    }
}
