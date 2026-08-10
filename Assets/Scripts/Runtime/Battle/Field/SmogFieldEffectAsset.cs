using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "SmogFieldEffect",
        menuName = "Pachimon/Battle/Field Effect/Smog")]
    public sealed class SmogFieldEffectAsset : BattleFieldEffectAsset
    {
        [SerializeField, Min(0)] private int _toxinApplicationRatio = 1;
        [SerializeField, Min(0)] private int _decayPerTickRatio = 1;
        [SerializeField] private ToxinStatusAsset _toxinStatus;

        public int ToxinApplicationRatio => _toxinApplicationRatio;
        public int DecayPerTickRatio => _decayPerTickRatio;
        public ToxinStatusAsset ToxinStatus => _toxinStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (EffectId != BattleFieldEffectId.Smog)
            {
                errors?.Add("Smog Definition must use Smog ID.");
            }
            if (_toxinStatus == null)
            {
                errors?.Add("Smog Definition requires a Toxin Definition.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            int toxinApplicationRatio,
            int decayPerTickRatio,
            ToxinStatusAsset toxinStatus,
            Sprite icon = null)
        {
            ConfigureDefinitionForEditor(
                BattleFieldEffectId.Smog,
                displayName,
                description,
                icon);
            _toxinApplicationRatio = toxinApplicationRatio;
            _decayPerTickRatio = decayPerTickRatio;
            _toxinStatus = toxinStatus;
        }
#endif
    }
}
