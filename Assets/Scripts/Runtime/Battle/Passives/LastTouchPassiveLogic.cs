using System;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class LastTouchPassiveLogic : IPassiveLogic
    {
        private readonly LastTouchPassiveAsset _definition;

        public LastTouchPassiveLogic(
            BattleUnitState owner,
            LastTouchPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not DamageAppliedEvent damage
                || !ReferenceEquals(damage.Source, Owner)
                || damage.OriginKind != DamageOriginKind.Skill
                || damage.ReceivedDamage <= 0
                || !damage.Target.IsAlive
                || damage.Target.Side == Owner.Side)
            {
                return;
            }

            var poison = Owner.GetBattleStatValue(PachimonStatType.Poison);
            var thresholdPercent = Math.Max(
                0m,
                poison * _definition.PoisonExecutionRatio / 100m);
            if (damage.Target.CurrentHp * 100m
                > damage.Target.MaxHp * thresholdPercent)
            {
                return;
            }

            BattleExecutionDamageService.Execute(
                damage.State,
                Owner,
                damage.Target,
                _definition.PassiveId);
        }
    }
}
