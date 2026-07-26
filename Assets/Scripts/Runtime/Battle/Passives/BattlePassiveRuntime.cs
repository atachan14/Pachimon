using System;
using System.Linq;

namespace Pachimon.Battle
{
    public sealed class BattlePassiveRuntime
    {
        public BattlePassiveRuntime(
            BattleState state,
            PassiveLogicRegistry logicRegistry)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (logicRegistry == null) throw new ArgumentNullException(nameof(logicRegistry));

            foreach (var unit in state.Player.Units.Concat(state.Enemy.Units))
            {
                foreach (var passiveId in unit.PassiveIds)
                {
                    state.Events.Register(logicRegistry.Create(passiveId, unit));
                }
            }

            state.Events.Publish(new BattleStartedEvent(state));
        }
    }
}
