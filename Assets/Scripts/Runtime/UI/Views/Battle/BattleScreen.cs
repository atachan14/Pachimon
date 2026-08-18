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
                if (_flowController?.Phase
                    == BattleFlowPhase.AwaitingPlayerSkill)
                {
                    ShowPlayerSkillChoices(
                        _flowController.RefreshPlayerSkillChoices());
                }

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
            Action<BattleFieldEffectInstance> fieldEffectDetailsRequested,
            Action<BattleWeatherInstance> weatherDetailsRequested,
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
            _observedDomainLogCount = 0;
            _completionSent = false;
            _previewedSkillSlotId = null;
            BattleMainView?.ClearSkillPreview();
            BattleMainView?.ConfigureFieldEffectClicks(
                fieldEffectDetailsRequested,
                weatherDetailsRequested);

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
            var toxinTransitions = _currentState.ToxinPresentation.Drain();
            if (toxinTransitions.Count > 0)
            {
                if (BattleMainView == null)
                {
                    _stateChanged?.Invoke(_currentState);
                    HandleFlowStep(step);
                    return;
                }

                BattleMainView.PlayToxinDamage(
                    toxinTransitions,
                    () =>
                    {
                        _stateChanged?.Invoke(_currentState);
                        HandleFlowStep(step);
                    });
                return;
            }

            HandleFlowStep(step);
        }

        private void HandleFlowStep(BattleFlowStep step)
        {
            BattleMainView?.RenderFields(_currentState);
            switch (step.Kind)
            {
                case BattleFlowStepKind.PlayerInputRequired:
                    ShowPlayerSkillChoices(step);
                    break;
                case BattleFlowStepKind.ActionStarted:
                    HandleActionStarted(step);
                    break;
                case BattleFlowStepKind.ActionResolved:
                    HandleActionResolved(step);
                    break;
                case BattleFlowStepKind.ActionCancelled:
                    HandleActionCancelled(step);
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
                        : !choice.IsCooldownReady
                            ? $"{choice.Skill.DisplayName}\nCD {remainingCooldown}"
                            : $"{choice.Skill.DisplayName}\nMN {choice.Skill.BaseManaCost}";
                    var skillSlotId = choice.SlotId;
                    return new LogWindowSkillOption(
                        skillSlotId,
                        label,
                        choice.IsUsable,
                        () => PreviewOrSubmitPlayerSkill(skillSlotId),
                        choice.Skill);
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
            var step = _flowController.SubmitPlayerSkill(skillSlotId);
            if (step.Kind == BattleFlowStepKind.ActionStarted)
            {
                HandleActionStarted(step);
            }
            else
            {
                HandleActionResolved(step);
            }
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
            var page = BuildActionDialoguePage(step.Resolution);
            _logWindowView.PlayDialoguePage(
                page,
                CompleteActionPresentation);
        }

        private void HandleActionStarted(BattleFlowStep step)
        {
            ClearSkillPreview();
            Render(_currentState);
            _stateChanged?.Invoke(_currentState);
            var action = step.PendingAction;
            _logWindowView.SetLogText(
                $"{action.User.DisplayName}は{action.Skill.DisplayName}を構えた");
            _logWindowView.ShowAdvancePrompt(AdvanceBattle);
        }

        private void HandleActionCancelled(BattleFlowStep step)
        {
            ClearSkillPreview();
            Render(_currentState);
            _stateChanged?.Invoke(_currentState);
            var action = step.PendingAction;
            _logWindowView.SetLogText(
                $"{action.User.DisplayName}の{action.Skill.DisplayName}は不発に終わった");
            _logWindowView.ShowAdvancePrompt(AdvanceBattle);
        }

        private DialoguePage BuildActionDialoguePage(SkillResolution resolution)
        {
            if (resolution == null)
            {
                return new DialoguePage(Array.Empty<DialogueBlock>());
            }

            var presentation = resolution.Presentation;
            if (presentation.Steps.Count > 0)
            {
                _observedDomainLogCount = _currentState.LogEntries.Count;
                var blocks = new List<DialogueBlock>();
                foreach (var group in presentation.Steps
                    .GroupBy(step => step.BlockIndex)
                    .OrderBy(group => group.Key))
                {
                    var steps = group.ToArray();
                    var transitions = AggregateTransitions(
                        (group.Key == 0
                            && presentation.InitialManaTransition != null
                                ? new[] { presentation.InitialManaTransition }
                                : Array.Empty<BattleResourceTransition>())
                        .Concat(steps.SelectMany(step => step.Transitions)));
                    var showHeading = group.Key == 0
                        || presentation.BlockStyle
                            == BattlePresentationBlockStyle.RepeatedSkill;
                    var lines = new List<DialogueLine>();
                    if (showHeading)
                    {
                        var heading = group.Key == 0
                            ? $"{resolution.User.DisplayName}の{resolution.Skill.DisplayName}！"
                            : $"{resolution.User.DisplayName}の{resolution.Skill.DisplayName}が再発動！";
                        lines.Add(new DialogueLine(
                            heading,
                            () => BeginBattleDialogueBlock(
                            resolution.User,
                            transitions)));
                    }

                    var transitionsApplied = showHeading;
                    foreach (var presentationStep in steps)
                    {
                        if (!string.IsNullOrWhiteSpace(presentationStep.Text))
                        {
                            var focusUnit = presentationStep.FocusUnit;
                            var applyBlockTransitions = !transitionsApplied;
                            lines.Add(new DialogueLine(
                                presentationStep.Text,
                                applyBlockTransitions
                                    ? () => BeginBattleDialogueBlock(
                                        focusUnit ?? resolution.User,
                                        transitions)
                                    : () => FocusBattleUnit(focusUnit)));
                            transitionsApplied = true;
                        }

                        if (presentationStep.Kind
                                == BattlePresentationStepKind.DamageApplied
                            && presentationStep.Transitions.Any(transition =>
                                ReferenceEquals(
                                    transition.Unit,
                                    presentationStep.FocusUnit)
                                && transition.HpAfter == 0))
                        {
                            var defeatedUnit = presentationStep.FocusUnit;
                            lines.Add(new DialogueLine(
                                $"{defeatedUnit.DisplayName}は戦闘不能になった",
                                () => FocusBattleUnit(defeatedUnit)));
                        }
                    }

                    blocks.Add(new DialogueBlock(lines));
                }

                return new DialoguePage(blocks);
            }

            var fallbackLines = new List<DialogueLine>();
            var initialTransitions = presentation.InitialManaTransition == null
                ? Array.Empty<BattleResourceTransition>()
                : new[] { presentation.InitialManaTransition };
            fallbackLines.Add(new DialogueLine(
                $"{resolution.User.DisplayName}の{resolution.Skill.DisplayName}！",
                () => BeginBattleDialogueBlock(
                    resolution.User,
                    initialTransitions)));
            while (_observedDomainLogCount < _currentState.LogEntries.Count)
            {
                fallbackLines.Add(new DialogueLine(
                    _currentState.LogEntries[_observedDomainLogCount],
                    null));
                _observedDomainLogCount++;
            }

            foreach (var effect in resolution.Effects)
            {
                var damageKind = effect.IsTrueDamage ? "確定ダメージ" : "ダメージ";
                fallbackLines.Add(new DialogueLine(
                    $"{effect.Target.DisplayName}に{effect.Damage}の{damageKind}！",
                    () => FocusBattleUnit(effect.Target)));
                if (effect.Target.IsDefeated)
                {
                    fallbackLines.Add(new DialogueLine(
                        $"{effect.Target.DisplayName}は戦闘不能になった",
                        () => FocusBattleUnit(effect.Target)));
                }
            }

            return new DialoguePage(new[] { new DialogueBlock(fallbackLines) });
        }

        private void BeginBattleDialogueBlock(
            BattleUnitState focusUnit,
            IReadOnlyList<BattleResourceTransition> transitions)
        {
            FocusBattleUnit(focusUnit);
            foreach (var transition in transitions)
            {
                BattleMainView?.PresentResourceSnapshot(transition);
            }
        }

        private void FocusBattleUnit(BattleUnitState unit)
        {
            if (unit != null)
            {
                _unitFocused?.Invoke(unit);
            }
        }

        private static IReadOnlyList<BattleResourceTransition> AggregateTransitions(
            IEnumerable<BattleResourceTransition> transitions)
        {
            var byUnit = new Dictionary<BattleUnitState, BattleResourceTransition>();
            foreach (var transition in transitions.Where(value => value != null))
            {
                if (byUnit.TryGetValue(transition.Unit, out var existing))
                {
                    byUnit[transition.Unit] = new BattleResourceTransition(
                        transition.Unit,
                        existing.HpBefore,
                        transition.HpAfter,
                        existing.MnBefore,
                        transition.MnAfter);
                }
                else
                {
                    byUnit.Add(transition.Unit, transition);
                }
            }

            return byUnit.Values.ToArray();
        }

        private void CompleteActionPresentation()
        {
            Render(_currentState);
            _stateChanged?.Invoke(_currentState);
            AdvanceBattle();
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

    }
}
