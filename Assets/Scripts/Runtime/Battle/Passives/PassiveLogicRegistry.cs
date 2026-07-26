using System;
using System.Collections.Generic;
using Pachimon.Reward;

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

        public PassiveLogicRegistry()
        {
            for (var passiveId = FirstPlaceholderPassiveId;
                 passiveId <= LastPlaceholderPassiveId;
                 passiveId++)
            {
                var capturedId = passiveId;
                var attribute = PlaceholderAttributes[
                    (passiveId - FirstPlaceholderPassiveId) % PlaceholderAttributes.Length];
                _factoriesByPassiveId[passiveId] = owner =>
                    new OutgoingAttributeDamagePassiveLogic(capturedId, owner, attribute);
            }
        }

        public static bool TryGetPlaceholderAttribute(
            int passiveId,
            out PachimonAttribute attribute)
        {
            if (passiveId < FirstPlaceholderPassiveId
                || passiveId > LastPlaceholderPassiveId)
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
    }
}
