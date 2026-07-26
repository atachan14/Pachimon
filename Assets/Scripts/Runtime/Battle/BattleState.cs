using System;
using System.Collections.Generic;

namespace Pachimon.Battle
{
    public sealed class BattleState
    {
        private readonly List<string> _logEntries = new();
        private bool _battleEndedPublished;

        public BattleState(
            int battleSeed,
            BattleSideState player,
            BattleSideState enemy,
            PassiveLogicRegistry passiveLogicRegistry = null)
        {
            if (player?.Side != BattleSide.Player)
            {
                throw new ArgumentException("Player Side is required.", nameof(player));
            }

            if (enemy?.Side != BattleSide.Enemy)
            {
                throw new ArgumentException("Enemy Side is required.", nameof(enemy));
            }

            BattleSeed = battleSeed;
            Player = player;
            Enemy = enemy;
            Events = new BattleEventDispatcher();
            Timeline = new BattleTimeline(this);
            Passives = new BattlePassiveRuntime(
                this,
                passiveLogicRegistry ?? new PassiveLogicRegistry());
        }

        public int BattleSeed { get; }
        public long CurrentTick { get; internal set; }
        public BattleSideState Player { get; }
        public BattleSideState Enemy { get; }
        public BattleTimeline Timeline { get; }
        public BattleEventDispatcher Events { get; }
        public BattlePassiveRuntime Passives { get; }
        public BattleOutcome Outcome { get; private set; }
        public IReadOnlyList<string> LogEntries => _logEntries;

        public BattleSideState GetOpposingSide(BattleSide side)
        {
            return side switch
            {
                BattleSide.Player => Enemy,
                BattleSide.Enemy => Player,
                _ => throw new ArgumentOutOfRangeException(nameof(side)),
            };
        }

        public BattleOutcome EvaluateOutcome()
        {
            Outcome = Player.IsDefeated
                ? BattleOutcome.PlayerDefeat
                : Enemy.IsDefeated
                    ? BattleOutcome.PlayerVictory
                    : BattleOutcome.Undecided;
            if (!_battleEndedPublished && Outcome != BattleOutcome.Undecided)
            {
                _battleEndedPublished = true;
                Events.PublishFinal(new BattleEndedEvent(this, Outcome));
            }

            return Outcome;
        }

        public void AddLog(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            _logEntries.Add(message);
        }
    }
}
