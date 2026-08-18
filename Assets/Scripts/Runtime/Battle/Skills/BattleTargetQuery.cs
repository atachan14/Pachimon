using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.Battle
{
    public sealed class SkillTargetUnavailableException : InvalidOperationException
    {
        public SkillTargetUnavailableException()
            : base("No target is currently available for this Skill.")
        {
        }
    }

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
        public BattleUnitState GetFrontEnemy() =>
            GetEnemyTargets().FirstOrDefault()
            ?? throw new SkillTargetUnavailableException();
        public BattleUnitState GetBackEnemy() =>
            GetEnemyTargets().LastOrDefault()
            ?? throw new SkillTargetUnavailableException();
        public BattleUnitState GetLowestHpEnemy() =>
            GetEnemyTargets()
                .OrderBy(unit => unit.CurrentHp)
                .ThenBy(unit => unit.SlotIndex)
                .FirstOrDefault()
            ?? throw new SkillTargetUnavailableException();
        public IReadOnlyList<BattleUnitState> GetAllEnemies() => GetEnemyTargets();
        public IReadOnlyList<BattleUnitState> GetAllAllies() => _allies.GetAllLiving();
        public BattleUnitState GetFrontAlly() => _allies.GetFrontLiving();
        public BattleUnitState GetLowestHpPercentageAlly() =>
            _allies.GetAllLiving()
                .OrderBy(unit => (decimal)unit.CurrentHp / unit.MaxHp)
                .ThenBy(unit => unit.SlotIndex)
                .FirstOrDefault()
            ?? throw new SkillTargetUnavailableException();
        public IReadOnlyList<BattleUnitState> GetAlliesBehind(BattleUnitState source) =>
            _allies.GetLivingBehind(source);
        public IReadOnlyList<BattleUnitState> GetAlliesAhead(BattleUnitState source) =>
            _allies.GetLivingAheadOf(source);

        private IReadOnlyList<BattleUnitState> GetEnemyTargets()
        {
            var targets = _enemies.GetAllTargetable();
            return targets.Count > 0
                ? targets
                : throw new SkillTargetUnavailableException();
        }
    }
}
