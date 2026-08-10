using System;
using Pachimon.Passives;

namespace Pachimon.Battle
{
    public sealed class FrozenGroundPassiveLogic : IPassiveLogic
    {
        private readonly FrozenGroundPassiveAsset _definition;

        public FrozenGroundPassiveLogic(
            BattleUnitState owner,
            FrozenGroundPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not BattleStartedEvent || !Owner.IsAlive)
            {
                return;
            }
            if (_definition.FieldEffect == null)
            {
                throw new InvalidOperationException(
                    "Frozen Ground Passive requires a Field Effect.");
            }

            battleEvent.State.Fields.CreateFrozenGround(
                Owner,
                _definition.FieldEffect);
        }
    }
}
