using System;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class HealthyPlantPassiveLogic : IPassiveLogic, IHealingModifierProvider
    {
        private readonly HealthyPlantPassiveAsset _definition;

        public HealthyPlantPassiveLogic(BattleUnitState owner, HealthyPlantPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }
        public void Handle(IBattleEvent battleEvent) { }

        public decimal ModifyHealing(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            decimal value)
        {
            if (!Owner.IsAlive || !ReferenceEquals(target, Owner) || value <= 0m)
            {
                return value;
            }
            var bonus = Math.Max(
                0m,
                _definition.BaseHealingRatio
                + Owner.GetBattleStatValue(PachimonStatType.Leaf)
                    * _definition.LeafHealingRatio / 100m);
            return value * (1m + bonus / 100m);
        }
    }
}
