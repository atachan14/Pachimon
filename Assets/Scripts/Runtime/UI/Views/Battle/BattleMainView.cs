using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Pachimon.Battle;

namespace Pachimon.UI
{
    public sealed class BattleMainView : MonoBehaviour
    {
        [field: SerializeField] public RectTransform GraphicWindow { get; private set; }
        [field: SerializeField] public BattleUnitAreaView EnemyArea { get; private set; }
        [field: SerializeField] public BattleUnitAreaView AllyArea { get; private set; }
        [field: FormerlySerializedAs("BattleLogWindow")]
        [field: SerializeField] public RectTransform BattleLogRoot { get; private set; }
        [field: SerializeField] public RectTransform SkillSelectorRoot { get; private set; }

        public void Initialize(
            RectTransform graphicWindow,
            BattleUnitAreaView enemyArea,
            BattleUnitAreaView allyArea,
            RectTransform battleLogRoot,
            RectTransform skillSelectorRoot)
        {
            GraphicWindow = graphicWindow;
            EnemyArea = enemyArea;
            AllyArea = allyArea;
            BattleLogRoot = battleLogRoot;
            SkillSelectorRoot = skillSelectorRoot;
        }

        public void Render(BattleState state)
        {
            if (state == null)
            {
                return;
            }

            EnemyArea?.RenderUnits(state.Enemies, "Enemy");
            AllyArea?.RenderUnits(state.Allies, "Ally");

            if (BattleLogRoot != null)
            {
                var logLabel = GetOrCreateRuntimeLogLabel(BattleLogRoot);
                logLabel.text = BuildLogText(state.LogEntries);
            }

            if (SkillSelectorRoot != null)
            {
                RenderSkillSelector(SkillSelectorRoot);
            }
        }

        private static string BuildLogText(IReadOnlyList<string> logEntries)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Battle Log");

            if (logEntries == null || logEntries.Count == 0)
            {
                builder.Append("- no events");
                return builder.ToString();
            }

            foreach (var entry in logEntries)
            {
                builder.Append("- ").AppendLine(entry);
            }

            return builder.ToString().TrimEnd();
        }

        private static TextMeshProUGUI GetOrCreateRuntimeLogLabel(RectTransform parent)
        {
            var existing = parent.Find("RuntimeBattleLogLabel");
            TextMeshProUGUI label;

            if (existing != null && existing.TryGetComponent(out label))
            {
                return label;
            }

            var labelObject = new GameObject("RuntimeBattleLogLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            label = labelObject.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.TopLeft;
            label.fontSize = 20f;
            label.color = Color.white;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;

            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(20f, 20f);
            rect.offsetMax = new Vector2(-20f, -20f);
            return label;
        }

        private static void RenderSkillSelector(RectTransform parent)
        {
            EnsureRuntimeSkillButton(parent, 0, "Skill 1");
            EnsureRuntimeSkillButton(parent, 1, "Skill 2");
            EnsureRuntimeSkillButton(parent, 2, "Skill 3");
        }

        private static void EnsureRuntimeSkillButton(RectTransform parent, int index, string labelText)
        {
            var buttonName = $"RuntimeSkillButton{index + 1}";
            var existing = parent.Find(buttonName);
            GameObject buttonObject;

            if (existing == null)
            {
                buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(parent, false);

                var image = buttonObject.GetComponent<Image>();
                image.color = new Color(0.24f, 0.31f, 0.42f);

                var buttonRect = buttonObject.GetComponent<RectTransform>();
                buttonRect.sizeDelta = new Vector2(180f, 52f);

                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(buttonObject.transform, false);
                var label = labelObject.GetComponent<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 20f;
                label.color = Color.white;
                label.enableWordWrapping = false;
                label.overflowMode = TextOverflowModes.Ellipsis;

                var labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(10f, 8f);
                labelRect.offsetMax = new Vector2(-10f, -8f);
            }
            else
            {
                buttonObject = existing.gameObject;
            }

            var runtimeLabel = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
            if (runtimeLabel != null)
            {
                runtimeLabel.text = labelText;
            }
        }
    }
}
