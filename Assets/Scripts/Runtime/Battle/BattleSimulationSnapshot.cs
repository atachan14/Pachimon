using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.Battle
{
    public sealed class BattleSimulationSnapshot
    {
        private readonly IReadOnlyDictionary<BattleUnitState, BattleUnitState>
            _originalToSimulation;
        private readonly IReadOnlyDictionary<BattleUnitState, BattleUnitState>
            _simulationToOriginal;

        private BattleSimulationSnapshot(
            BattleState state,
            IReadOnlyDictionary<BattleUnitState, BattleUnitState>
                originalToSimulation,
            IReadOnlyDictionary<BattleUnitState, BattleUnitState>
                simulationToOriginal)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            _originalToSimulation = originalToSimulation
                ?? throw new ArgumentNullException(nameof(originalToSimulation));
            _simulationToOriginal = simulationToOriginal
                ?? throw new ArgumentNullException(nameof(simulationToOriginal));
        }

        public BattleState State { get; }

        public BattleUnitState GetSimulationUnit(BattleUnitState original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            return _originalToSimulation.TryGetValue(original, out var simulation)
                ? simulation
                : throw new ArgumentException(
                    "The Unit does not belong to the source Battle.",
                    nameof(original));
        }

        public BattleUnitState GetOriginalUnit(BattleUnitState simulation)
        {
            if (simulation == null)
            {
                throw new ArgumentNullException(nameof(simulation));
            }

            return _simulationToOriginal.TryGetValue(simulation, out var original)
                ? original
                : throw new ArgumentException(
                    "The Unit does not belong to this Simulation.",
                    nameof(simulation));
        }

        public static BattleSimulationSnapshot Create(BattleState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var originalUnits = source.Player.Units
                .Concat(source.Enemy.Units)
                .ToArray();
            var unitMap = originalUnits.ToDictionary(
                unit => unit,
                unit => unit.CreateSimulationClone());
            var simulationState = new BattleState(
                source.BattleSeed,
                CreateSide(source.Player, unitMap),
                CreateSide(source.Enemy, unitMap),
                source.PassiveLogicRegistry,
                publishBattleStarted: false,
                environmentDefinitions: source.Weather.Definitions)
            {
                CurrentTick = source.CurrentTick,
            };
            simulationState.SetElectricDamageCountForSimulation(
                source.ElectricDamageCount);

            foreach (var original in originalUnits)
            {
                unitMap[original].CopyStatusesForSimulation(original, unitMap);
            }
            simulationState.Statuses.RefreshAllActionClockPauses();
            simulationState.Fields.CopyForSimulation(source.Fields, unitMap);
            simulationState.Weather.CopyForSimulation(source.Weather, unitMap);

            return new BattleSimulationSnapshot(
                simulationState,
                unitMap,
                unitMap.ToDictionary(pair => pair.Value, pair => pair.Key));
        }

        private static BattleSideState CreateSide(
            BattleSideState source,
            IReadOnlyDictionary<BattleUnitState, BattleUnitState> unitMap)
        {
            return new BattleSideState(
                source.Side,
                source.Units.Select(unit => unitMap[unit]));
        }
    }
}
