using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Battle;
using Pachimon.Data;
using Pachimon.Skills;
using UnityEngine;

namespace Pachimon.UI
{
    public sealed class BattleScreen : NodeScreen
    {
        [field: SerializeField] public BattleMainView BattleMainView { get; private set; }
        [field: SerializeField] public RewardOverlayView RewardOverlayView { get; private set; }

        private readonly Queue<BattleLogMessage> _pendingMessages = new();
        private BattleState _currentState;
        private BattleFlowController _flowController;
        private LogWindowView _logWindowView;
        private PachimonCatalog _pachimonCatalog;
        private Action<BattleState> _stateChanged;
        private Action<BattleUnitState> _unitFocused;
        private Action<BattleOutcome> _battleCompleted;
        private int _observedDomainLogCount;
        private bool _completionSent;
        private int? _previewedSkillSlotId;
        public bool CanUseItems =>
            !_completionSent
            && _flowController?.Phase == BattleFlowPhase.AwaitingPlayerSkill;

        public void Initialize(
            BattleMainView battleMainView,
            RewardOverlayView rewardOverlayView)
        {
            BattleMainView = battleMainView;
            RewardOverlayView = rewardOverlayView;
        }

        public void Render(BattleState state)
        {
            _currentState = state;
            BattleMainView?.Render(state, _pachimonCatalog);
        }

        public void ApplyExternalBattleStateChange()
        {
            if (_currentState == null || _completionSent)
            {
                return;
            }

            ClearSkillPreview();
            Render(_currentState);
            _stateChanged?.Invoke(_currentState);
            var outcome = _currentState.EvaluateOutcome();
            if (outcome == BattleOutcome.Undecided)
            {
                return;
            }

            _logWindowView?.ClearOptions();
            ShowBattleCompleted(outcome);
        }

        public void BeginBattle(
            BattleState state,
            BattleSkillRuntime skillRuntime,
            LogWindowView logWindowView,
            PachimonCatalog pachimonCatalog,
            Sprite playerTrainerGraphic,
            Sprite enemyTrainerGraphic,
            string challengeMessage,
            Action<BattleState> stateChanged,
            Action<BattleUnitState> unitFocused,
            Action<BattleOutcome> battleCompleted)
        {
            _currentState = state ?? throw new ArgumentNullException(nameof(state));
            _flowController = new BattleFlowController(
                state,
                skillRuntime ?? throw new ArgumentNullException(nameof(skillRuntime)));
            _logWindowView = logWindowView
                ?? throw new ArgumentNullException(nameof(logWindowView));
            _pachimonCatalog = pachimonCatalog
                ?? throw new ArgumentNullException(nameof(pachimonCatalog));
            _stateChanged = stateChanged;
            _unitFocused = unitFocused;
            _battleCompleted = battleCompleted;
            _pendingMessages.Clear();
            _observedDomainLogCount = 0;
            _completionSent = false;
            _previewedSkillSlotId = null;
            BattleMainView?.ClearSkillPreview();

            Render(state);
            _stateChanged?.Invoke(state);
            _logWindowView.ClearOptions();
            _logWindowView.SetLogText(string.Empty);
            BattleMainView?.PlayTrainerEntrance(
                playerTrainerGraphic,
                enemyTrainerGraphic,
                () => ShowChallengeMessage(challengeMessage));
        }

        private void ShowChallengeMessage(string challengeMessage)
        {
            _logWindowView.SetLogText(string.IsNullOrWhiteSpace(challengeMessage)
                ? "トレーナーが勝負をしかけてきた"
                : challengeMessage);
            _logWindowView.ShowAdvancePrompt(
                () =>
                {
                    _logWindowView.ClearOptions();
                    BattleMainView?.PlayTrainerExitAndUnitEntrance(
                        () =>
                        {
                            Render(_currentState);
                            AdvanceBattle();
                        });
                });
        }

        private void AdvanceBattle()
        {
            if (_flowController == null || _completionSent)
            {
                return;
            }

            var step = _flowController.Advance();
            switch (step.Kind)
            {
                case BattleFlowStepKind.PlayerInputRequired:
                    ShowPlayerSkillChoices(step);
                    break;
                case BattleFlowStepKind.ActionResolved:
                    HandleActionResolved(step);
                    break;
                case BattleFlowStepKind.BattleCompleted:
                    ShowBattleCompleted(step.Outcome);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ShowPlayerSkillChoices(BattleFlowStep step)
        {
            ClearSkillPreview();
            _unitFocused?.Invoke(step.Actor);
            _logWindowView.SetLogText($"{step.Actor.DisplayName}の行動を選んでください");
            var options = step.SkillChoices
                .Select(choice =>
                {
                    var remainingCooldown = Math.Max(
                        0L,
                        choice.CooldownReadyTick - _currentState.CurrentTick);
                    var label = choice.IsUsable
                        ? choice.Skill.DisplayName
                        : $"{choice.Skill.DisplayName}\nCD {remainingCooldown}";
                    var skillSlotId = choice.SlotId;
                    return new LogWindowSkillOption(
                        skillSlotId,
                        label,
                        choice.IsUsable,
                        () => PreviewOrSubmitPlayerSkill(skillSlotId));
                })
                .ToArray();
            LogWindowOption? struggleOption = step.RequiresStruggleConfirmation
                ? new LogWindowOption(
                    step.StruggleSkill?.DisplayName ?? "わるあがき",
                    () => PreviewOrSubmitPlayerSkill(0))
                : null;
            _logWindowView.ShowSkillOptions(options, struggleOption);
        }

        private void PreviewOrSubmitPlayerSkill(int skillSlotId)
        {
            if (_flowController == null
                || _flowController.Phase != BattleFlowPhase.AwaitingPlayerSkill)
            {
                return;
            }

            if (_previewedSkillSlotId != skillSlotId)
            {
                _previewedSkillSlotId = skillSlotId;
                var preview = _flowController.PreviewPlayerSkill(skillSlotId);
                BattleMainView?.ShowSkillPreview(_currentState, preview);
                _logWindowView.SetSelectedSkillOption(skillSlotId);
                return;
            }

            ClearSkillPreview();
            _logWindowView.ClearOptions();
            HandleActionResolved(_flowController.SubmitPlayerSkill(skillSlotId));
        }

        private void ClearSkillPreview()
        {
            _previewedSkillSlotId = null;
            BattleMainView?.ClearSkillPreview();
            _logWindowView?.SetSelectedSkillOption(null);
        }

        private void HandleActionResolved(BattleFlowStep step)
        {
            ClearSkillPreview();
            Render(_currentState);
            _stateChanged?.Invoke(_currentState);
            QueueActionMessages(step.Resolution);
            ShowNextPendingMessage();
        }

        private void QueueActionMessages(SkillResolution resolution)
        {
            if (resolution == null)
            {
                return;
            }

            _pendingMessages.Enqueue(new BattleLogMessage(
                $"{resolution.User.DisplayName}の{resolution.Skill.DisplayName}！",
                resolution.User));
            while (_observedDomainLogCount < _currentState.LogEntries.Count)
            {
                _pendingMessages.Enqueue(new BattleLogMessage(
                    _currentState.LogEntries[_observedDomainLogCount],
                    null));
                _observedDomainLogCount++;
            }

            foreach (var effect in resolution.Effects)
            {
                var damageKind = effect.IsTrueDamage ? "確定ダメージ" : "ダメージ";
                _pendingMessages.Enqueue(new BattleLogMessage(
                    $"{effect.Target.DisplayName}に{effect.Damage}の{damageKind}！",
                    effect.Target));
                if (effect.Target.IsDefeated)
                {
                    _pendingMessages.Enqueue(new BattleLogMessage(
                        $"{effect.Target.DisplayName}は戦闘不能になった",
                        effect.Target));
                }
            }
        }

        private void ShowNextPendingMessage()
        {
            if (_pendingMessages.Count == 0)
            {
                AdvanceBattle();
                return;
            }

            var message = _pendingMessages.Dequeue();
            _unitFocused?.Invoke(message.FocusUnit);
            _logWindowView.SetLogText(message.Text);
            _logWindowView.ShowAdvancePrompt(ShowNextPendingMessage);
        }

        private void ShowBattleCompleted(BattleOutcome outcome)
        {
            ClearSkillPreview();
            _logWindowView.SetLogText(outcome == BattleOutcome.PlayerVictory
                ? "勝負に勝った！"
                : "目の前が真っ暗になった...");
            _logWindowView.ShowAdvancePrompt(
                () =>
                {
                    if (_completionSent) return;
                    _completionSent = true;
                    _logWindowView.ClearOptions();
                    _battleCompleted?.Invoke(outcome);
                });
        }

        private readonly struct BattleLogMessage
        {
            public BattleLogMessage(string text, BattleUnitState focusUnit)
            {
                Text = text;
                FocusUnit = focusUnit;
            }

            public string Text { get; }
            public BattleUnitState FocusUnit { get; }
        }

    }
}
