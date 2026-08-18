using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class BattlePassiveRuntime
    {
        private readonly List<IPassiveLogic> _logics = new();

        public BattlePassiveRuntime(
            BattleState state,
            PassiveLogicRegistry logicRegistry,
            bool publishBattleStarted = true)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (logicRegistry == null) throw new ArgumentNullException(nameof(logicRegistry));

            foreach (var unit in state.Player.Units.Concat(state.Enemy.Units))
            {
                foreach (var passiveId in unit.PassiveIds)
                {
                    var logic = logicRegistry.Create(passiveId, unit);
                    _logics.Add(logic);
                    state.Events.Register(logic);
                }
            }

            if (publishBattleStarted)
            {
                state.Events.Publish(new BattleStartedEvent(state));
            }
        }

        public IEnumerable<IStatModifier> CreateStatModifiers(
            BattleState state,
            BattleUnitState owner)
        {
            return _logics
                .Where(logic => ReferenceEquals(logic.Owner, owner))
                .OfType<IBattleStatModifierProvider>()
                .SelectMany(provider => provider.CreateStatModifiers(state));
        }

        public decimal ModifyHealing(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            decimal value)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (value < 0m) throw new ArgumentOutOfRangeException(nameof(value));

            foreach (var provider in _logics
                         .OfType<IHealingModifierProvider>())
            {
                value = provider.ModifyHealing(
                    state,
                    source,
                    target,
                    value);
            }
            return value;
        }

        public ShieldApplicationPlan ModifyShield(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            ShieldApplicationPlan plan)
        {
            foreach (var provider in _logics.OfType<IShieldModifierProvider>())
                plan = provider.ModifyShield(state, source, target, plan);
            return plan;
        }

        public decimal ModifyOutgoingStatusValue(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            BattleStatusId statusId,
            BattleStatusCategory categories,
            decimal value)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (value < 0m) throw new ArgumentOutOfRangeException(nameof(value));

            foreach (var provider in _logics
                         .OfType<IOutgoingStatusValueModifierProvider>())
            {
                value = provider.ModifyOutgoingStatusValue(
                    state,
                    source,
                    target,
                    statusId,
                    categories,
                    value);
            }
            return value;
        }

        public decimal ModifyPenetrationPercent(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            DamageContext context)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var penetration = context.PenetrationPercent;
            if (!context.ApplyOutgoingModifiers)
                return penetration;

            foreach (var provider in _logics
                         .Where(logic => ReferenceEquals(logic.Owner, source))
                         .OfType<IOutgoingPenetrationModifierProvider>())
            {
                penetration = provider.ModifyPenetrationPercent(
                    state,
                    source,
                    target,
                    context,
                    penetration);
            }
            return penetration;
        }

        public bool ShouldContinueTurn(
            BattleState state,
            SkillResolution resolution)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (resolution == null)
                throw new ArgumentNullException(nameof(resolution));

            return _logics
                .Where(logic => ReferenceEquals(logic.Owner, resolution.User))
                .OfType<IContinueTurnAfterSkillProvider>()
                .Any(provider => provider.ShouldContinueTurn(state, resolution));
        }
    }
}
