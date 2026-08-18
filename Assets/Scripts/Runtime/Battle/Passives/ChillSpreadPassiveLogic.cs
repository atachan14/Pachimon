using System;
using System.Linq;
using Pachimon.Passives;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class ChillSpreadPassiveLogic : IPassiveLogic
    {
        private readonly ChillSpreadPassiveAsset _definition;
        public ChillSpreadPassiveLogic(BattleUnitState owner,
            ChillSpreadPassiveAsset definition)
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
                || !damage.Target.IsDefeated) return;
            var chill = damage.GetStatusValueBeforeDamage(BattleStatusId.Chill);
            var spread = SignedStatMath.FloorNonNegative(
                chill * _definition.SpreadPercent / 100m);
            if (spread <= 0) return;
            var side = damage.Target.Side == BattleSide.Player
                ? damage.State.Player : damage.State.Enemy;
            foreach (var target in side.GetAllLiving().ToArray())
                damage.State.Statuses.ApplyStatus(target,
                    BattleStatusFactory.CreateSlow(Owner, spread,
                        _definition.ChillStatus));
        }
    }
}
