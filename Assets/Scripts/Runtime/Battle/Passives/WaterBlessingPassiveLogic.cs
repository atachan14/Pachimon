using System;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class WaterBlessingPassiveLogic :
        IPassiveLogic,
        IHealingModifierProvider
    {
        private readonly WaterBlessingPassiveAsset _definition;

        public WaterBlessingPassiveLogic(
            BattleUnitState owner,
            WaterBlessingPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
        }

        public decimal ModifyHealing(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            decimal value)
        {
            if (!Owner.IsAlive || target.Side != Owner.Side || value <= 0m)
            {
                return value;
            }

            var aqua = Owner.GetBattleStatValue(PachimonStatType.Aqua);
            var bonusPercent = Math.Max(
                0m,
                _definition.BaseHealingRatio
                + aqua * _definition.AquaHealingRatio / 100m);
            return value * (1m + bonusPercent / 100m);
        }
    }
}
