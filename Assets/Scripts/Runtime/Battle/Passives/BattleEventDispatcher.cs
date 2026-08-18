using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public interface IPassiveLogic
    {
        BattleUnitState Owner { get; }
        void Handle(IBattleEvent battleEvent);
    }

    public interface IBattleStatModifierProvider
    {
        IEnumerable<IStatModifier> CreateStatModifiers(BattleState state);
    }

    public interface IHealingModifierProvider
    {
        decimal ModifyHealing(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            decimal value);
    }

    public interface IShieldModifierProvider
    {
        ShieldApplicationPlan ModifyShield(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            ShieldApplicationPlan plan);
    }

    public interface IOutgoingStatusValueModifierProvider
    {
        decimal ModifyOutgoingStatusValue(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            BattleStatusId statusId,
            BattleStatusCategory categories,
            decimal value);
    }

    public interface IOutgoingPenetrationModifierProvider
    {
        decimal ModifyPenetrationPercent(
            BattleState state,
            BattleUnitState source,
            BattleUnitState target,
            DamageContext context,
            decimal penetrationPercent);
    }

    public interface IContinueTurnAfterSkillProvider
    {
        bool ShouldContinueTurn(
            BattleState state,
            SkillResolution resolution);
    }

    public sealed class BattleEventDispatcher
    {
        private readonly List<IPassiveLogic> _passiveLogics = new();
        private readonly Queue<IBattleEvent> _eventQueue = new();
        private bool _isDispatching;
        private bool _clearAfterDispatch;

        public int RegisteredPassiveCount => _passiveLogics.Count;

        public void Register(IPassiveLogic passiveLogic)
        {
            if (passiveLogic == null) throw new ArgumentNullException(nameof(passiveLogic));
            _passiveLogics.Add(passiveLogic);
        }

        public void Publish(IBattleEvent battleEvent)
        {
            if (battleEvent == null) throw new ArgumentNullException(nameof(battleEvent));
            _eventQueue.Enqueue(battleEvent);
            if (_isDispatching)
            {
                return;
            }

            _isDispatching = true;
            try
            {
                while (_eventQueue.Count > 0)
                {
                    Dispatch(_eventQueue.Dequeue());
                }
            }
            catch
            {
                _eventQueue.Clear();
                throw;
            }
            finally
            {
                _isDispatching = false;
                if (_clearAfterDispatch)
                {
                    Clear();
                }
            }
        }

        public void PublishFinal(IBattleEvent battleEvent)
        {
            _clearAfterDispatch = true;
            Publish(battleEvent);
        }

        public void Clear()
        {
            _passiveLogics.Clear();
            _eventQueue.Clear();
            _clearAfterDispatch = false;
        }

        private void Dispatch(IBattleEvent battleEvent)
        {
            foreach (var passiveLogic in _passiveLogics
                         .OrderBy(logic => GetEventPriority(logic.Owner, battleEvent))
                         .ThenBy(logic => logic.Owner.Side)
                         .ThenBy(logic => logic.Owner.SlotIndex)
                         .ThenBy(logic => logic.Owner.TiePriority)
                         .ToArray())
            {
                passiveLogic.Handle(battleEvent);
            }
        }

        private static int GetEventPriority(
            BattleUnitState owner,
            IBattleEvent battleEvent)
        {
            if (ReferenceEquals(owner, battleEvent.Target)) return 0;
            if (ReferenceEquals(owner, battleEvent.Source)) return 1;
            return 2;
        }
    }
}
