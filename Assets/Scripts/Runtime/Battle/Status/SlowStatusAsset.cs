using System;
using System.Collections.Generic;
using Pachimon.Reward;
using Pachimon.Run;
using UnityEngine;

namespace Pachimon.Battle
{
    [CreateAssetMenu(
        fileName = "SlowStatus",
        menuName = "Pachimon/Battle/Status/Slow")]
    public sealed class SlowStatusAsset : BattleStatusAsset
    {
        [SerializeField, Min(1)] private int _decayPerTick = 1;
        [SerializeField] private bool _usesAttributeDefense;
        [SerializeField] private PachimonAttribute _defenseAttribute;

        public int DecayPerTick => _decayPerTick;
        public bool UsesAttributeDefense => _usesAttributeDefense;
        public PachimonAttribute DefenseAttribute => _defenseAttribute;
        public PachimonStatType? DefenseStat => _usesAttributeDefense
            ? PachimonStatTypeUtility.FromAttribute(_defenseAttribute)
            : null;

        public override void CollectValidationErrors(ICollection<string> errors)
        {
            base.CollectValidationErrors(errors);
            if (StatusId != BattleStatusId.Slow
                && StatusId != BattleStatusId.Paralysis
                && StatusId != BattleStatusId.Chill)
            {
                errors?.Add(
                    "Slow Definition must use Slow, Paralysis, or Chill ID.");
            }
            if (_decayPerTick <= 0)
            {
                errors?.Add("Slow Definition requires positive Decay Per Tick.");
            }
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            BattleStatusId statusId,
            string displayName,
            string description,
            int decayPerTick,
            bool usesAttributeDefense,
            PachimonAttribute defenseAttribute = PachimonAttribute.Fire,
            Sprite icon = null)
        {
            if (statusId != BattleStatusId.Slow
                && statusId != BattleStatusId.Paralysis
                && statusId != BattleStatusId.Chill)
            {
                throw new ArgumentOutOfRangeException(nameof(statusId));
            }
            ConfigureDefinitionForEditor(
                statusId,
                displayName,
                description,
                icon);
            _decayPerTick = decayPerTick;
            _usesAttributeDefense = usesAttributeDefense;
            _defenseAttribute = defenseAttribute;
        }
#endif
    }
}
