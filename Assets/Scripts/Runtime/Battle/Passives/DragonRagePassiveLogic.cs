using System;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class DragonRagePassiveLogic :
        IPassiveLogic,
        IOutgoingPenetrationModifierProvider
    {
        private readonly DragonRagePassiveAsset _definition;

        public DragonRagePassiveLogic(
            BattleUnitState owner,
            DragonRagePassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }
        public void Handle(IBattleEvent battleEvent) { }

        public DamagePenetration ModifyPenetration(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            DamageContext context,
            DamagePenetration penetration)
        {
            var penetrationValue = decimal.Floor(
                Owner.GetBattleStatValue(PachimonStatType.Dragon)
                * _definition.PenetrationRatio / 100m);
            return penetration.WithAdditionalResistBonusPercentage(
                PenetrationMath.CalculateDiminishingPercentage(
                    penetrationValue));
        }
    }
}
