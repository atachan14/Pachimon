using System;
using Pachimon.Passives;

namespace Pachimon.Battle
{
    public sealed class ManyHitsPassiveLogic : IPassiveLogic
    {
        private readonly ManyHitsPassiveAsset _definition;

        public ManyHitsPassiveLogic(
            BattleUnitState owner,
            ManyHitsPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not BeforeAttributeDamageEvent damage
                || !ReferenceEquals(damage.Source, Owner)
                || !damage.Calculation.Context.ApplyOutgoingModifiers
                || damage.Target.GetStatus(BattleStatusId.DragonCranker) == null)
            {
                return;
            }

            damage.MultiplyDamage(_definition.DamagePercent);
        }
    }
}
