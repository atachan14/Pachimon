using System;
using Pachimon.Run;
using UnityEngine;

namespace Pachimon.Data
{
    [Serializable]
    public sealed class PachimonInitialStats
    {
        [Header("Resources (display values)")]
        [SerializeField, Min(0)] private int _maxHp;
        [SerializeField, Min(0)] private int _maxMn;

        [Header("Attributes")]
        [SerializeField, Min(0)] private int _fire;
        [SerializeField, Min(0)] private int _poison;
        [SerializeField, Min(0)] private int _aqua;
        [SerializeField, Min(0)] private int _ice;
        [SerializeField, Min(0)] private int _leaf;
        [SerializeField, Min(0)] private int _wind;
        [SerializeField, Min(0)] private int _electric;
        [SerializeField, Min(0)] private int _dragon;

        [Header("Common Stats")]
        [SerializeField, Min(0)] private int _speed;
        [SerializeField, Min(0)] private int _haste;
        [SerializeField, Min(0)] private int _damageBonus;
        [SerializeField, Min(0)] private int _resistBonus;

        public int GetDisplayedValue(PachimonStatType statType)
        {
            return statType switch
            {
                PachimonStatType.MaxHp => _maxHp,
                PachimonStatType.MaxMn => _maxMn,
                PachimonStatType.Fire => _fire,
                PachimonStatType.Aqua => _aqua,
                PachimonStatType.Leaf => _leaf,
                PachimonStatType.Electric => _electric,
                PachimonStatType.Poison => _poison,
                PachimonStatType.Ice => _ice,
                PachimonStatType.Wind => _wind,
                PachimonStatType.Dragon => _dragon,
                PachimonStatType.Speed => _speed,
                PachimonStatType.Haste => _haste,
                PachimonStatType.DamageBonus => _damageBonus,
                PachimonStatType.ResistBonus => _resistBonus,
                _ => throw new ArgumentOutOfRangeException(nameof(statType), statType, null),
            };
        }

        public int GetValueUnits(
            PachimonStatType statType,
            int resourceDisplayMultiplier)
        {
            if (resourceDisplayMultiplier < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resourceDisplayMultiplier));
            }

            var value = GetDisplayedValue(statType);
            if (!PachimonStatTypeUtility.IsResource(statType))
            {
                return value;
            }

            if (value % resourceDisplayMultiplier != 0)
            {
                throw new InvalidOperationException(
                    $"Initial {statType} {value} must be divisible by "
                    + $"the resource multiplier {resourceDisplayMultiplier}.");
            }

            return value / resourceDisplayMultiplier;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            int maxHp = 0,
            int maxMn = 0,
            int fire = 0,
            int aqua = 0,
            int leaf = 0,
            int electric = 0,
            int poison = 0,
            int ice = 0,
            int wind = 0,
            int dragon = 0,
            int speed = 0,
            int haste = 0,
            int damageBonus = 0,
            int resistBonus = 0)
        {
            _maxHp = maxHp;
            _maxMn = maxMn;
            _fire = fire;
            _aqua = aqua;
            _leaf = leaf;
            _electric = electric;
            _poison = poison;
            _ice = ice;
            _wind = wind;
            _dragon = dragon;
            _speed = speed;
            _haste = haste;
            _damageBonus = damageBonus;
            _resistBonus = resistBonus;
        }
#endif
    }
}
