using TMPro;
using UnityEngine;
using Pachimon.Battle;

namespace Pachimon.UI
{
    public sealed class BattleMainView : MonoBehaviour
    {
        [field: SerializeField] public RectTransform GraphicWindow { get; private set; }
        [field: SerializeField] public BattleUnitAreaView EnemyArea { get; private set; }
        [field: SerializeField] public BattleUnitAreaView AllyArea { get; private set; }

        public void Initialize(
            RectTransform graphicWindow,
            BattleUnitAreaView enemyArea,
            BattleUnitAreaView allyArea)
        {
            GraphicWindow = graphicWindow;
            EnemyArea = enemyArea;
            AllyArea = allyArea;
        }

        public void Render(BattleState state)
        {
            if (state == null)
            {
                return;
            }

            EnemyArea?.RenderUnits(state.Enemies, "Enemy");
            AllyArea?.RenderUnits(state.Allies, "Ally");
        }
    }
}
