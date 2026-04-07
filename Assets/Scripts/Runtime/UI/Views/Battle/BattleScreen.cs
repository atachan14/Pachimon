using Pachimon.Battle;
using UnityEngine;

namespace Pachimon.UI
{
    public sealed class BattleScreen : NodeScreen
    {
        [field: SerializeField] public BattleMainView BattleMainView { get; private set; }
        [field: SerializeField] public RewardOverlayView RewardOverlayView { get; private set; }

        public void Initialize(
            BattleMainView battleMainView,
            RewardOverlayView rewardOverlayView)
        {
            BattleMainView = battleMainView;
            RewardOverlayView = rewardOverlayView;
        }

        public void Render(BattleState state)
        {
            BattleMainView?.Render(state);
        }
    }
}
