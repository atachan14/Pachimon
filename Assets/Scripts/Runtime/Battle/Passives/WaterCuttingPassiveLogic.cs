using System;
using System.Linq;
using Pachimon.Passives;

namespace Pachimon.Battle
{
    public sealed class WaterCuttingPassiveLogic :
        IPassiveLogic,
        IContinueTurnAfterSkillProvider
    {
        public WaterCuttingPassiveLogic(
            BattleUnitState owner,
            WaterCuttingPassiveAsset definition)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
        }

        public BattleUnitState Owner { get; }
        public WaterCuttingPassiveAsset Definition { get; }

        public void Handle(IBattleEvent battleEvent)
        {
        }

        public bool ShouldContinueTurn(
            BattleState state,
            SkillResolution resolution)
        {
            return Owner.IsAlive
                && ReferenceEquals(resolution.User, Owner)
                && resolution.Effects.Any(effect =>
                    effect.Damage > 0
                    && effect.Target.Side != Owner.Side
                    && effect.Target.IsDefeated);
        }
    }
}
