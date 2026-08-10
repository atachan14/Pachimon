using System;
using System.Linq;
using Pachimon.Passives;
using Pachimon.Reward;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class IceWitchPassiveLogic : IPassiveLogic
    {
        private readonly IceWitchPassiveAsset _definition;

        public IceWitchPassiveLogic(
            BattleUnitState owner,
            IceWitchPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not UnitDefeatedEvent defeatedEvent
                || !Owner.IsAlive
                || defeatedEvent.DefeatedUnit.Side == Owner.Side)
            {
                return;
            }

            var targets = defeatedEvent.State
                .GetOpposingSide(Owner.Side)
                .GetAllLiving()
                .OrderBy(unit => unit.SlotIndex)
                .ToArray();
            if (targets.Length == 0)
            {
                return;
            }

            var ice = Owner.GetBattleStatValue(PachimonStatType.Ice);
            var iceRatio = defeatedEvent.State.ResolveAttributeRatio(
                PachimonAttribute.Ice,
                _definition.IceDamageRatio);
            var totalDamage = _definition.BaseIceDamage
                * SignedStatMath.AmplificationMultiplier(
                    ice * iceRatio / 100m);
            var damagePerTarget = totalDamage / targets.Length;
            if (damagePerTarget <= 0m)
            {
                return;
            }

            defeatedEvent.State.AddLog($"{Owner.DisplayName}の氷の魔女！");
            foreach (var target in targets)
            {
                var wasAlive = target.IsAlive;
                BattleAttributeDamageService.Apply(
                    defeatedEvent.State,
                    Owner,
                    target,
                    new DamageContext(
                        DamageOriginKind.Passive,
                        _definition.PassiveId,
                        damagePerTarget,
                        Owner.GetBattleStats(),
                        target.GetBattleStats(),
                        PachimonAttribute.Ice,
                        isAttack: false,
                        applyAttackerAttributeMultiplier: false,
                        applyDamageBonusMultiplier: false,
                        applyOutgoingModifiers: false));
                if (wasAlive && target.IsDefeated)
                {
                    defeatedEvent.State.Events.Publish(new UnitDefeatedEvent(
                        defeatedEvent.State,
                        Owner,
                        target));
                }
            }
        }
    }
}
