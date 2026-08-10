using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Reward;

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
            PassiveLogicRegistry passiveLogicRegistry = null,
            bool publishBattleStarted = true)
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
            PassiveLogicRegistry = passiveLogicRegistry
                ?? new PassiveLogicRegistry();
            Events = new BattleEventDispatcher();
            Presentation = new BattlePresentationRecorder(this);
            ToxinPresentation = new ToxinPresentationRecorder();
            Timeline = new BattleTimeline(this);
            Statuses = new BattleStatusRuntime(this);
            SupportEffects = new BattleSupportEffectRuntime(this);
            Fields = new BattleFieldRuntime(this);
            Weather = new BattleWeatherRuntime(this);
            Passives = new BattlePassiveRuntime(
                this,
                PassiveLogicRegistry,
                publishBattleStarted);
            foreach (var unit in Player.Units.Concat(Enemy.Units))
            {
                unit.SetBattleModifierProvider(
                    () => Passives.CreateStatModifiers(this, unit)
                        .Concat(Weather.CreateStatModifiers(unit)));
            }
        }

        public int BattleSeed { get; }
        public long CurrentTick { get; internal set; }
        public BattleSideState Player { get; }
        public BattleSideState Enemy { get; }
        public BattleTimeline Timeline { get; }
        public BattleEventDispatcher Events { get; }
        public BattlePresentationRecorder Presentation { get; }
        public ToxinPresentationRecorder ToxinPresentation { get; }
        public BattleStatusRuntime Statuses { get; }
        public BattleSupportEffectRuntime SupportEffects { get; }
        public BattleFieldRuntime Fields { get; }
        public BattleWeatherRuntime Weather { get; }
        public BattlePassiveRuntime Passives { get; }
        internal PassiveLogicRegistry PassiveLogicRegistry { get; }
        public BattleOutcome Outcome { get; private set; }
        public IReadOnlyList<string> LogEntries => _logEntries;

        public decimal ResolveAttributeRatio(
            PachimonAttribute attribute,
            decimal baseRatio)
        {
            return baseRatio * Weather.GetAttributeRatioMultiplier(attribute);
        }

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
            Presentation.RecordLog(message);
        }
    }
}
