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
        [SerializeField, Min(0)] private int _decayPerTick = 1;
        [SerializeField, Min(0)] private int _speedReductionScale;
        [SerializeField] private bool _usesAttributeDefense;
        [SerializeField] private PachimonAttribute _defenseAttribute;

        public int DecayPerTick => _decayPerTick;
        public int SpeedReductionScale => _speedReductionScale;
        public bool UsesAttributeDefense => _usesAttributeDefense;
        public PachimonAttribute DefenseAttribute => _defenseAttribute;
        public PachimonStatType? DefenseStat => _usesAttributeDefense
            ? PachimonStatTypeUtility.FromAttribute(_defenseAttribute)
            : null;

        public int CalculateSpeedReduction(int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (value == 0 || _speedReductionScale <= 0)
            {
                return value;
            }

            return SignedStatMath.FloorNonNegative(
                (decimal)Math.Sqrt((double)value * _speedReductionScale));
        }

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
            if (_decayPerTick < 0)
            {
                errors?.Add("Slow Definition cannot have negative Decay Per Tick.");
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
            Sprite icon = null,
            int speedReductionScale = 0)
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
            _speedReductionScale = speedReductionScale;
            _usesAttributeDefense = usesAttributeDefense;
            _defenseAttribute = defenseAttribute;
        }
#endif
    }
}
