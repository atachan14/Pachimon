using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.Run
{
    public enum StartNodeProgressState
    {
        IntroDialogue = 0,
        Selecting = 1,
        SelectionConfirmation = 2,
        FinalDialogue = 3,
        Completed = 4,
    }

    public sealed class StartDialogueData
    {
        public StartDialogueData(
            string greeting,
            string selectionPrompt,
            string confirmationPrompt,
            string finalMessage)
        {
            Greeting = greeting;
            SelectionPrompt = selectionPrompt;
            ConfirmationPrompt = confirmationPrompt;
            FinalMessage = finalMessage;
        }

        public string Greeting { get; }
        public string SelectionPrompt { get; }
        public string ConfirmationPrompt { get; }
        public string FinalMessage { get; }

        public static StartDialogueData CreateDefault(string playerName)
        {
            return new StartDialogueData(
                $"よく来たね、{playerName}",
                "ここに3匹のパチモンがおる。\n1匹選びなさい。",
                "このパチモンでよろしいか",
                "バッジを8つ以上集めて、パチモンマスターを目指すのじゃ！");
        }
    }

    public sealed class StartNodeController
    {
        private readonly string[] _candidateIds;
        private readonly int _selectionCount;
        private readonly Func<IReadOnlyList<string>, bool> _tryCommitParty;
        private readonly Action _onCompleted;
        private readonly List<string> _selectedIds = new();

        public StartNodeController(
            IEnumerable<string> candidateIds,
            int selectionCount,
            StartDialogueData dialogue,
            Func<IReadOnlyList<string>, bool> tryCommitParty,
            Action onCompleted)
        {
            if (candidateIds == null) throw new ArgumentNullException(nameof(candidateIds));
            if (selectionCount <= 0) throw new ArgumentOutOfRangeException(nameof(selectionCount));

            _candidateIds = candidateIds.ToArray();
            if (_candidateIds.Length < selectionCount
                || _candidateIds.Any(string.IsNullOrWhiteSpace)
                || _candidateIds.Distinct().Count() != _candidateIds.Length)
            {
                throw new ArgumentException("Start candidates must be unique, non-empty, and numerous enough.", nameof(candidateIds));
            }

            _selectionCount = selectionCount;
            Dialogue = dialogue ?? throw new ArgumentNullException(nameof(dialogue));
            _tryCommitParty = tryCommitParty ?? throw new ArgumentNullException(nameof(tryCommitParty));
            _onCompleted = onCompleted ?? throw new ArgumentNullException(nameof(onCompleted));
        }

        public event Action Changed;

        public StartNodeProgressState State { get; private set; } =
            StartNodeProgressState.IntroDialogue;

        public StartDialogueData Dialogue { get; }
        public IReadOnlyList<string> CandidateIds => _candidateIds;
        public IReadOnlyList<string> SelectedIds => _selectedIds;
        public int SelectionCount => _selectionCount;

        public int GetSelectionOrder(string candidateId)
        {
            var index = _selectedIds.IndexOf(candidateId);
            return index >= 0 ? index + 1 : 0;
        }

        public bool AdvanceIntro()
        {
            if (State != StartNodeProgressState.IntroDialogue)
            {
                return false;
            }

            State = StartNodeProgressState.Selecting;
            Changed?.Invoke();
            return true;
        }

        public bool ToggleCandidate(string candidateId)
        {
            if (State != StartNodeProgressState.Selecting
                || !_candidateIds.Contains(candidateId))
            {
                return false;
            }

            var selectedIndex = _selectedIds.IndexOf(candidateId);
            if (selectedIndex >= 0)
            {
                _selectedIds.RemoveAt(selectedIndex);
                Changed?.Invoke();
                return true;
            }

            if (_selectedIds.Count >= _selectionCount)
            {
                return false;
            }

            _selectedIds.Add(candidateId);
            if (_selectedIds.Count == _selectionCount)
            {
                State = StartNodeProgressState.SelectionConfirmation;
            }

            Changed?.Invoke();
            return true;
        }

        public bool RestartSelection()
        {
            if (State != StartNodeProgressState.SelectionConfirmation)
            {
                return false;
            }

            _selectedIds.Clear();
            State = StartNodeProgressState.Selecting;
            Changed?.Invoke();
            return true;
        }

        public bool ConfirmSelection()
        {
            if (State != StartNodeProgressState.SelectionConfirmation
                || _selectedIds.Count != _selectionCount
                || !_tryCommitParty(_selectedIds.ToArray()))
            {
                return false;
            }

            State = StartNodeProgressState.FinalDialogue;
            Changed?.Invoke();
            return true;
        }

        public bool Complete()
        {
            if (State != StartNodeProgressState.FinalDialogue)
            {
                return false;
            }

            State = StartNodeProgressState.Completed;
            Changed?.Invoke();
            _onCompleted();
            return true;
        }
    }
}
