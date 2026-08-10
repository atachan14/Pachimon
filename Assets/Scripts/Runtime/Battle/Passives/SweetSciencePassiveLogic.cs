using System;
using Pachimon.Passives;

namespace Pachimon.Battle
{
    public sealed class SweetSciencePassiveLogic : IPassiveLogic
    {
        private readonly SweetSciencePassiveAsset _definition;

        public SweetSciencePassiveLogic(
            BattleUnitState owner,
            SweetSciencePassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not AttackEvadedEvent evaded
                || !ReferenceEquals(evaded.Target, Owner)
                || _definition.SpeedGain <= 0)
            {
                return;
            }

            var existing = Owner.GetStatus(BattleStatusId.SweetScience);
            Owner.ApplyOrReplaceStatus(new BattleStatusInstance(
                BattleStatusId.SweetScience,
                BattleStatusCategory.None,
                Owner,
                checked((existing?.Value ?? 0) + _definition.SpeedGain),
                definition: _definition.SpeedStatus));
            evaded.State.Presentation.RecordLog(
                $"{Owner.DisplayName}のスイートサイエンス！");
        }
    }
}
