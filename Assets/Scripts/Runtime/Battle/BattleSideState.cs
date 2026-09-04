using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.Battle
{
    public sealed class BattleSideState
    {
        public const int MaxPartySize = 3;

        private readonly BattleUnitState[] _units;

        public BattleSideState(BattleSide side, IEnumerable<BattleUnitState> units)
        {
            Side = side;
            var suppliedUnits = units?.ToArray()
                ?? throw new ArgumentNullException(nameof(units));
            if (suppliedUnits.Length < 1 || suppliedUnits.Length > MaxPartySize)
            {
                throw new ArgumentException(
                    $"A Battle Side requires between 1 and {MaxPartySize} units.",
                    nameof(units));
            }

            if (suppliedUnits.Any(unit => unit == null))
            {
                throw new ArgumentException(
                    "A Battle Side cannot contain a null Unit.",
                    nameof(units));
            }

            _units = suppliedUnits.OrderBy(unit => unit.SlotIndex).ToArray();
            if (_units.Any(unit => unit.Side != side)
                || _units.Select(unit => unit.SlotIndex).Distinct().Count() != _units.Length
                || _units.Where((unit, index) => unit.SlotIndex != index).Any())
            {
                throw new ArgumentException(
                    "Battle Units must match the Side and occupy contiguous slots from 0.",
                    nameof(units));
            }
        }

        public BattleSide Side { get; }
        public IReadOnlyList<BattleUnitState> Units => _units;
        public bool IsDefeated => _units.All(unit => unit.IsDefeated);

        public BattleUnitState GetUnitAt(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < _units.Length
                ? _units[slotIndex]
                : null;
        }

        public BattleUnitState GetFrontLiving()
        {
            return _units.FirstOrDefault(unit => unit.IsAlive);
        }

        public BattleUnitState GetBackLiving()
        {
            return _units.LastOrDefault(unit => unit.IsAlive);
        }

        public IReadOnlyList<BattleUnitState> GetAllLiving()
        {
            return _units.Where(unit => unit.IsAlive).ToArray();
        }

        public IReadOnlyList<BattleUnitState> GetAllTargetable()
        {
            return _units.Where(unit => unit.IsTargetable).ToArray();
        }

        public IReadOnlyList<BattleUnitState> GetLivingAheadOf(BattleUnitState source)
        {
            ValidateMember(source);
            return _units
                .Where(unit => unit.IsAlive && unit.SlotIndex < source.SlotIndex)
                .ToArray();
        }

        public IReadOnlyList<BattleUnitState> GetLivingBehind(BattleUnitState source)
        {
            ValidateMember(source);
            return _units
                .Where(unit => unit.IsAlive && unit.SlotIndex > source.SlotIndex)
                .ToArray();
        }

        private void ValidateMember(BattleUnitState source)
        {
            if (source == null || !_units.Contains(source))
            {
                throw new ArgumentException(
                    "The source Unit does not belong to this Battle Side.",
                    nameof(source));
            }
        }
    }
}
