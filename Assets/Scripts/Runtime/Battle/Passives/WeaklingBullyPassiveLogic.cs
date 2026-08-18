using System;
using Pachimon.Passives;

namespace Pachimon.Battle
{
    public sealed class WeaklingBullyPassiveLogic : IPassiveLogic
    {
        private readonly WeaklingBullyPassiveAsset _definition;

        public WeaklingBullyPassiveLogic(
            BattleUnitState owner,
            WeaklingBullyPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not BeforeAttributeDamageEvent damageEvent
                || !ReferenceEquals(damageEvent.Source, Owner)
                || damageEvent.Calculation?.Context.ApplyOutgoingModifiers
                    != true
                || damageEvent.WeaknessValue <= 0)
            {
                return;
            }

            damageEvent.MultiplyDamage(_definition.DamagePercent);
            if (damageEvent.Hit != null
                && !damageEvent.Hit.TryMarkPassiveTriggered(
                    _definition.PassiveId))
            {
                return;
            }
            Owner.ApplyOrReplaceStatus(new BattleStatusInstance(
                BattleStatusId.WeaklingBullySpeed,
                BattleStatusCategory.None,
                Owner,
                Math.Max(0, _definition.SpeedBonus),
                durationTicks: _definition.SpeedDurationTicks,
                definition: _definition.SpeedStatus));
            battleEvent.State.Presentation.RecordLog(
                $"{Owner.DisplayName}の{_definition.DisplayName}！");
        }
    }
}
