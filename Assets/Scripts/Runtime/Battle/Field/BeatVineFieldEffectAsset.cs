using System.Collections.Generic;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "BeatVineFieldEffect",
        menuName = "Pachimon/Battle/Field Effect/Beat Vine")]
    public sealed class BeatVineFieldEffectAsset : BattleFieldEffectAsset
    {
        [SerializeField, Min(0)] private int _baseValue = 30;
        [SerializeField, Min(0)] private int _leafValueRatio = 100;
        [SerializeField, Min(1)] private int _attackIntervalTicks = 100;
        [SerializeField, Min(0)] private int _pollenValueRatio = 50;
        [SerializeField] private PollenStatusAsset _pollenStatus;

        public int BaseValue => _baseValue;
        public int LeafValueRatio => _leafValueRatio;
        public int AttackIntervalTicks => _attackIntervalTicks;
        public int PollenValueRatio => _pollenValueRatio;
        public PollenStatusAsset PollenStatus => _pollenStatus;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (EffectId != BattleFieldEffectId.BeatVine)
                errors?.Add("Beat Vine Definition must use BeatVine ID.");
            if ((Categories & BattleFieldEffectCategory.Plant) == 0)
                errors?.Add("Beat Vine must use the Plant category.");
            if (_attackIntervalTicks <= 0)
                errors?.Add("Beat Vine requires a positive attack interval.");
            if (_pollenStatus == null)
                errors?.Add("Beat Vine requires a Pollen Status.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string displayName,
            string description,
            int baseValue,
            int leafValueRatio,
            int attackIntervalTicks)
        {
            ConfigureDefinitionForEditor(
                BattleFieldEffectId.BeatVine,
                displayName,
                description,
                categories: BattleFieldEffectCategory.Plant);
            _baseValue = baseValue;
            _leafValueRatio = leafValueRatio;
            _attackIntervalTicks = attackIntervalTicks;
        }

        public void ConfigurePollenForEditor(
            PollenStatusAsset pollenStatus,
            int pollenValueRatio = 50)
        {
            _pollenStatus = pollenStatus;
            _pollenValueRatio = pollenValueRatio;
        }
#endif
    }
}
