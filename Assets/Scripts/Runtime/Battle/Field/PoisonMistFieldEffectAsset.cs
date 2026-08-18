using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "PoisonMistFieldEffect",
        menuName = "Pachimon/Battle/Field/Poison Mist")]
    public sealed class PoisonMistFieldEffectAsset : BattleFieldEffectAsset
    {
        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (EffectId != BattleFieldEffectId.PoisonMist)
                errors?.Add("Poison Mist must use the Poison Mist Effect ID.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(string displayName, string description)
        {
            ConfigureDefinitionForEditor(
                BattleFieldEffectId.PoisonMist,
                displayName,
                description);
        }
#endif
    }
}
