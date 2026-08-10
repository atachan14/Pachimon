using System;
using Pachimon.Passives;
using Pachimon.Reward;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class DarkFlamePassiveLogic : IPassiveLogic
    {
        private readonly DarkFlamePassiveAsset _definition;

        public DarkFlamePassiveLogic(
            BattleUnitState owner,
            DarkFlamePassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }

        public void Handle(IBattleEvent battleEvent)
        {
            if (battleEvent is not AttributeDamageAppliedEvent damageEvent
                || !ReferenceEquals(damageEvent.Source, Owner)
                || damageEvent.Attribute != PachimonAttribute.Fire
                || damageEvent.Calculation.Context.ApplyOutgoingModifiers == false
                || damageEvent.PreDefenseDamage <= 0m
                || !damageEvent.Target.IsAlive)
            {
                return;
            }

            var poison = Owner.GetBattleStatValue(PachimonStatType.Poison);
            var additionalBaseDamage = damageEvent.PreDefenseDamage
                * _definition.BaseConversionPercent
                / 100m
                * SignedStatMath.AmplificationMultiplier(
                    poison * _definition.PoisonScalingPercent / 100m);
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
                    PachimonAttribute.Poison,
                    isAttack: true,
                    applyAttackerAttributeMultiplier: false,
                    applyDamageBonusMultiplier: false,
                    applyOutgoingModifiers: false));
        }
    }
}
