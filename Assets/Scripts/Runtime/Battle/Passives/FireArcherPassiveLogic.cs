using System;
using Pachimon.Passives;
using Pachimon.Reward;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class FireArcherPassiveLogic : IPassiveLogic
    {
        private readonly FireArcherPassiveAsset _definition;

        public FireArcherPassiveLogic(
            BattleUnitState owner,
            FireArcherPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not DamageAppliedEvent damageEvent
                || !ReferenceEquals(damageEvent.Source, Owner)
                || damageEvent.ReceivedDamage <= 0
                || !damageEvent.Target.IsAlive
                || damageEvent.OriginKind != DamageOriginKind.Skill)
            {
                return;
            }

            var missingHp = damageEvent.Target.MaxHp
                - damageEvent.Target.CurrentHp;
            var fire = Owner.GetBattleStatValue(PachimonStatType.Fire);
            var fireRatio = damageEvent.State.ResolveAttributeRatio(
                PachimonAttribute.Fire,
                _definition.FireScalingPercent);
            var additionalBaseDamage = missingHp
                * _definition.MissingHpPercent
                / 100m
                * SignedStatMath.AmplificationMultiplier(
                    fire * fireRatio / 100m);
            if (additionalBaseDamage <= 0m)
            {
                return;
            }

            BattleAttributeDamageService.Apply(
                damageEvent.State,
                Owner,
                damageEvent.Target,
                new DamageContext(
                    DamageOriginKind.Passive,
                    _definition.PassiveId,
                    additionalBaseDamage,
                    Owner.GetBattleStats(),
                    damageEvent.Target.GetBattleStats(),
                    PachimonAttribute.Fire,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false,
                    applyDamageBonusMultiplier: true,
                    applyOutgoingModifiers: true));
        }
    }
}
