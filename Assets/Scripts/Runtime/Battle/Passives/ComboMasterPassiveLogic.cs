using System;
using Pachimon.Passives;

namespace Pachimon.Battle
{
    public sealed class ComboMasterPassiveLogic : IPassiveLogic
    {
        private readonly ComboMasterPassiveAsset _definition;

        public ComboMasterPassiveLogic(
            BattleUnitState owner,
            ComboMasterPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not ChainResolvedEvent chainEvent
                || !ReferenceEquals(chainEvent.Source, Owner)
                || chainEvent.CompletedAdditionalChainCount <= 0
                || _definition.DamageBonusPerChain == 0)
            {
                return;
            }

            var currentMaximum = Owner.GetStatus(
                BattleStatusId.ComboMasterBonus)?.StackCount ?? 0;
            if (chainEvent.CompletedAdditionalChainCount <= currentMaximum)
            {
                return;
            }

            Owner.TryConsumeStatus(BattleStatusId.ComboMasterBonus, out _);
            Owner.AddStatusStacks(
                BattleStatusId.ComboMasterBonus,
                BattleStatusCategory.None,
                Owner,
                _definition.DamageBonusPerChain,
                chainEvent.CompletedAdditionalChainCount);
        }
    }
}
