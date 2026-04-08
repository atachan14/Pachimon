using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using Pachimon.Battle;

namespace Pachimon.UI
{
    public sealed class BattleUnitAreaView : MonoBehaviour
    {
        [field: SerializeField] public RectTransform BarsRoot { get; private set; }
        [field: SerializeField] public RectTransform GraphicsRoot { get; private set; }

        public void Initialize(RectTransform barsRoot, RectTransform graphicsRoot)
        {
            BarsRoot = barsRoot;
            GraphicsRoot = graphicsRoot;
        }

        public void RenderUnits(IReadOnlyList<BattleUnit> units, string sideLabel)
        {
            if (BarsRoot != null)
            {
                var barsText = GetOrCreateRuntimeLabel(BarsRoot, "RuntimeBarsLabel", TextAlignmentOptions.TopLeft, 20);
                barsText.text = BuildBarsText(units, sideLabel);
            }

            if (GraphicsRoot != null)
            {
                var graphicsText = GetOrCreateRuntimeLabel(GraphicsRoot, "RuntimeGraphicsLabel", TextAlignmentOptions.Center, 22);
                graphicsText.text = BuildGraphicsText(units, sideLabel);
            }
        }

        private static string BuildBarsText(IReadOnlyList<BattleUnit> units, string sideLabel)
        {
            var builder = new StringBuilder();
            builder.AppendLine(sideLabel + " Units");

            if (units == null || units.Count == 0)
            {
                builder.Append("- none");
                return builder.ToString();
            }

            foreach (var unit in units)
            {
                builder.Append("[")
                    .Append(unit.SlotIndex + 1)
                    .Append("] ")
                    .Append(unit.DisplayName)
                    .Append("  HP ")
                    .Append(unit.CurrentHp)
                    .Append("/")
                    .Append(unit.MaxHp)
                    .Append("  MN ")
                    .Append(unit.CurrentMn);

                if (!unit.IsAlive)
                {
                    builder.Append("  DOWN");
                }

                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        private static string BuildGraphicsText(IReadOnlyList<BattleUnit> units, string sideLabel)
        {
            var builder = new StringBuilder();
            builder.AppendLine(sideLabel + " Graphics");

            if (units == null || units.Count == 0)
            {
                builder.Append("(empty)");
                return builder.ToString();
            }

            foreach (var unit in units)
            {
                builder.Append('[')
                    .Append(unit.SlotIndex + 1)
                    .Append("] ")
                    .Append(unit.DisplayName)
                    .AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        private static TextMeshProUGUI GetOrCreateRuntimeLabel(RectTransform parent, string objectName, TextAlignmentOptions alignment, float fontSize)
        {
            var existing = parent.Find(objectName);
            TextMeshProUGUI label;

            if (existing != null && existing.TryGetComponent(out label))
            {
                return label;
            }

            var labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            label = labelObject.GetComponent<TextMeshProUGUI>();
            label.alignment = alignment;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;

            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 8f);
            rect.offsetMax = new Vector2(-8f, -8f);
            return label;
        }
    }
}
