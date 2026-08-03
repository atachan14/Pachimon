using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.Battle
{
    public sealed class BattleTargetQuery
    {
        private readonly BattleState _state;
        private readonly BattleUnitState _user;
        private readonly BattleSideState _allies;
        private readonly BattleSideState _enemies;

        public BattleTargetQuery(BattleState state, BattleUnitState user)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _user = user ?? throw new ArgumentNullException(nameof(user));
            _allies = user.Side == BattleSide.Player ? state.Player : state.Enemy;
            _enemies = state.GetOpposingSide(user.Side);
            if (!ReferenceEquals(_allies.GetUnitAt(user.SlotIndex), user))
            {
                throw new ArgumentException(
                    "The Skill user does not belong to this Battle.",
                    nameof(user));
            }
        }

        public BattleState State => _state;
        public BattleUnitState GetSelf() => _user;
        public BattleUnitState GetFrontEnemy() => _enemies.GetFrontLiving();
        public BattleUnitState GetBackEnemy() => _enemies.GetBackLiving();
        public BattleUnitState GetLowestHpEnemy() =>
            _enemies.GetAllLiving()
                .OrderBy(unit => unit.CurrentHp)
                .ThenBy(unit => unit.SlotIndex)
                .FirstOrDefault();
        public IReadOnlyList<BattleUnitState> GetAllEnemies() => _enemies.GetAllLiving();
        public BattleUnitState GetFrontAlly() => _allies.GetFrontLiving();
        public IReadOnlyList<BattleUnitState> GetAlliesBehind(BattleUnitState source) =>
            _allies.GetLivingBehind(source);
        public IReadOnlyList<BattleUnitState> GetAlliesAhead(BattleUnitState source) =>
            _allies.GetLivingAheadOf(source);
    }
}
