using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Run;
using UnityEngine;

namespace Pachimon.Data
{
    public enum FixedSubStatBinding
    {
        Random = 0,
        DamageBonus = 1,
        GenerationPower = 2,
        Haste = 3,
        Speed = 4,
        ResistBonus = 5,
        SustainPower = 6,
        StatusMastery = 7,
        StatusResistance = 8,
    }

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

        [Header("Fixed Attribute / SubStat Bindings")]
        [SerializeField] private FixedSubStatBinding _fireSubStat;
        [SerializeField] private FixedSubStatBinding _aquaSubStat;
        [SerializeField] private FixedSubStatBinding _leafSubStat;
        [SerializeField] private FixedSubStatBinding _electricSubStat;
        [SerializeField] private FixedSubStatBinding _iceSubStat;
        [SerializeField] private FixedSubStatBinding _windSubStat;
        [SerializeField] private FixedSubStatBinding _poisonSubStat;
        [SerializeField] private FixedSubStatBinding _dragonSubStat;

        public bool TryGetFixedSubStat(
            PachimonStatType attribute,
            out PachimonStatType subStat)
        {
            var binding = attribute switch
            {
                PachimonStatType.Fire => _fireSubStat,
                PachimonStatType.Aqua => _aquaSubStat,
                PachimonStatType.Leaf => _leafSubStat,
                PachimonStatType.Electric => _electricSubStat,
                PachimonStatType.Ice => _iceSubStat,
                PachimonStatType.Wind => _windSubStat,
                PachimonStatType.Poison => _poisonSubStat,
                PachimonStatType.Dragon => _dragonSubStat,
                _ => throw new ArgumentOutOfRangeException(nameof(attribute)),
            };
            subStat = ToStatType(binding);
            return binding != FixedSubStatBinding.Random;
        }

        public IReadOnlyList<string> ValidateFixedSubStatBindings()
        {
            var fixedSubStats = PachimonSubStatBindings.Attributes
                .Select(attribute => TryGetFixedSubStat(attribute, out var subStat)
                    ? subStat
                    : (PachimonStatType?)null)
                .Where(value => value.HasValue)
                .Select(value => value.Value)
                .ToArray();
            return fixedSubStats
                .GroupBy(value => value)
                .Where(group => group.Count() > 1)
                .Select(group => $"Fixed SubStat {group.Key} is assigned more than once.")
                .ToArray();
        }

        private static PachimonStatType ToStatType(FixedSubStatBinding binding)
        {
            return binding switch
            {
                FixedSubStatBinding.Random => PachimonStatType.DamageBonus,
                FixedSubStatBinding.DamageBonus => PachimonStatType.DamageBonus,
                FixedSubStatBinding.GenerationPower => PachimonStatType.GenerationPower,
                FixedSubStatBinding.Haste => PachimonStatType.Haste,
                FixedSubStatBinding.Speed => PachimonStatType.Speed,
                FixedSubStatBinding.ResistBonus => PachimonStatType.ResistBonus,
                FixedSubStatBinding.SustainPower => PachimonStatType.SustainPower,
                FixedSubStatBinding.StatusMastery => PachimonStatType.StatusMastery,
                FixedSubStatBinding.StatusResistance => PachimonStatType.StatusResistance,
                _ => throw new ArgumentOutOfRangeException(nameof(binding)),
            };
        }

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
                PachimonStatType.Speed => 0,
                PachimonStatType.Haste => 0,
                PachimonStatType.DamageBonus => 0,
                PachimonStatType.ResistBonus => 0,
                PachimonStatType.GenerationPower => 0,
                PachimonStatType.StatusMastery => 0,
                PachimonStatType.SustainPower => 0,
                PachimonStatType.StatusResistance => 0,
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
            int dragon = 0)
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
        }

        public void ConfigureFixedSubStatsForEditor(
            FixedSubStatBinding fire = FixedSubStatBinding.Random,
            FixedSubStatBinding aqua = FixedSubStatBinding.Random,
            FixedSubStatBinding leaf = FixedSubStatBinding.Random,
            FixedSubStatBinding electric = FixedSubStatBinding.Random,
            FixedSubStatBinding ice = FixedSubStatBinding.Random,
            FixedSubStatBinding wind = FixedSubStatBinding.Random,
            FixedSubStatBinding poison = FixedSubStatBinding.Random,
            FixedSubStatBinding dragon = FixedSubStatBinding.Random)
        {
            _fireSubStat = fire;
            _aquaSubStat = aqua;
            _leafSubStat = leaf;
            _electricSubStat = electric;
            _iceSubStat = ice;
            _windSubStat = wind;
            _poisonSubStat = poison;
            _dragonSubStat = dragon;
        }
#endif
    }
}
