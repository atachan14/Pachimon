using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class LogWindowView : MonoBehaviour
    {
        [field: SerializeField] public TMP_Text TextLogText { get; private set; }
        [field: SerializeField] public RectTransform SelectGridRoot { get; private set; }

        public void Initialize(TMP_Text textLogText, RectTransform selectGridRoot)
        {
            TextLogText = textLogText;
            SelectGridRoot = selectGridRoot;
        }

        public void SetLogText(string text)
        {
            if (TextLogText != null)
            {
                TextLogText.text = text;
            }
        }

        public void ClearOptions()
        {
            if (SelectGridRoot == null)
            {
                return;
            }

            for (var i = SelectGridRoot.childCount - 1; i >= 0; i--)
            {
                var child = SelectGridRoot.GetChild(i);
                if (child.name.StartsWith("RuntimeOptionButton"))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        public void ShowSingleOption(string label, UnityAction action)
        {
            ClearOptions();
            CreateOptionButton(0, label, action);
        }

        public void ShowOptions(params LogWindowOption[] options)
        {
            ClearOptions();

            if (options == null)
            {
                return;
            }

            for (var i = 0; i < options.Length; i++)
            {
                CreateOptionButton(i, options[i].Label, options[i].Action);
            }
        }

        private void CreateOptionButton(int index, string label, UnityAction action)
        {
            if (SelectGridRoot == null)
            {
                return;
            }

            var buttonObject = new GameObject($"RuntimeOptionButton{index + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(SelectGridRoot, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.24f, 0.31f, 0.42f);

            var button = buttonObject.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            if (action != null)
            {
                button.onClick.AddListener(action);
            }

            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(180f, 52f);

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelText = labelObject.GetComponent<TextMeshProUGUI>();
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontSize = 20f;
            labelText.color = Color.white;
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            labelText.text = label;

            var labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 8f);
            labelRect.offsetMax = new Vector2(-10f, -8f);
        }
    }

    public readonly struct LogWindowOption
    {
        public LogWindowOption(string label, UnityAction action)
        {
            Label = label;
            Action = action;
        }

        public string Label { get; }

        public UnityAction Action { get; }
    }
}
