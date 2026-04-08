using Pachimon.Battle;
using UnityEngine;
using UnityEngine.Events;

namespace Pachimon.UI
{
    public sealed class BattleScreen : NodeScreen
    {
        [field: SerializeField] public BattleMainView BattleMainView { get; private set; }
        [field: SerializeField] public RewardOverlayView RewardOverlayView { get; private set; }

        private BattleState _currentState;

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
            BattleMainView?.Render(state);
        }

        public void ConfigureLogWindow(LogWindowView logWindowView, UnityAction advanceAction)
        {
            if (logWindowView == null)
            {
                return;
            }

            logWindowView.SetLogText(BuildBattleLogText());
            logWindowView.ShowSingleOption("âº: êÌì¨äÆóπ", advanceAction);
        }

        private string BuildBattleLogText()
        {
            if (_currentState == null || _currentState.LogEntries == null || _currentState.LogEntries.Count == 0)
            {
                return "Battle Log\n- no events";
            }

            var text = "Battle Log";
            foreach (var entry in _currentState.LogEntries)
            {
                text += "\n- " + entry;
            }

            return text;
        }
    }
}
