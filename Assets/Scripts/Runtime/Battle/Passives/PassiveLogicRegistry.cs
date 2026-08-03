using System;
using System.Collections.Generic;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Passives;

namespace Pachimon.Battle
{
    public sealed class PassiveLogicRegistry
    {
        public const int FirstPlaceholderPassiveId = 1;
        public const int LastPlaceholderPassiveId = 151;

        private static readonly PachimonAttribute[] PlaceholderAttributes =
        {
            PachimonAttribute.Fire,
            PachimonAttribute.Aqua,
            PachimonAttribute.Leaf,
            PachimonAttribute.Electric,
            PachimonAttribute.Poison,
            PachimonAttribute.Ice,
            PachimonAttribute.Wind,
            PachimonAttribute.Dragon,
        };

        private readonly Dictionary<int, Func<BattleUnitState, IPassiveLogic>>
            _factoriesByPassiveId = new();

        public PassiveLogicRegistry(PassiveCatalog passiveCatalog = null)
        {
            for (var passiveId = FirstPlaceholderPassiveId;
                 passiveId <= LastPlaceholderPassiveId;
                 passiveId++)
            {
                var capturedId = passiveId;
                if (passiveCatalog?.Get(passiveId)
                    is StoredChargePassiveAsset storedCharge)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new StoredChargePassiveLogic(owner, storedCharge);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId)
                    is StaticElectricityPassiveAsset staticElectricity)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new StaticElectricityPassiveLogic(
                            owner,
                            staticElectricity);
                    continue;
                }

                if (passiveCatalog?.Get(passiveId) != null)
                {
                    _factoriesByPassiveId[passiveId] = owner =>
                        new StatOnlyPassiveLogic(owner);
                    continue;
                }

                var attribute = PlaceholderAttributes[
                    (passiveId - FirstPlaceholderPassiveId) % PlaceholderAttributes.Length];
                _factoriesByPassiveId[passiveId] = owner =>
                    new OutgoingAttributeDamagePassiveLogic(capturedId, owner, attribute);
            }
        }

        public static bool TryGetPlaceholderAttribute(
            int passiveId,
            PassiveCatalog passiveCatalog,
            out PachimonAttribute attribute)
        {
            if (passiveId < FirstPlaceholderPassiveId
                || passiveId > LastPlaceholderPassiveId
                || passiveCatalog?.Get(passiveId) != null)
            {
                attribute = default;
                return false;
            }

            attribute = PlaceholderAttributes[
                (passiveId - FirstPlaceholderPassiveId) % PlaceholderAttributes.Length];
            return true;
        }

        public IPassiveLogic Create(int passiveId, BattleUnitState owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (!_factoriesByPassiveId.TryGetValue(passiveId, out var factory))
            {
                throw new InvalidOperationException(
                    $"Passive {passiveId} has no registered Logic.");
            }

            return factory(owner);
        }

        public void RegisterOrReplace(
            int passiveId,
            Func<BattleUnitState, IPassiveLogic> factory)
        {
            if (passiveId <= 0) throw new ArgumentOutOfRangeException(nameof(passiveId));
            _factoriesByPassiveId[passiveId] = factory
                ?? throw new ArgumentNullException(nameof(factory));
        }

        private sealed class StatOnlyPassiveLogic : IPassiveLogic
        {
            public StatOnlyPassiveLogic(BattleUnitState owner)
            {
                Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public BattleUnitState Owner { get; }

            public void Handle(IBattleEvent battleEvent)
            {
            }
        }
    }
}
