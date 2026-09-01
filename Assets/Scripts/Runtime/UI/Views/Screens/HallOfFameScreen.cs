using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class HallOfFameScreen : NodeScreen
    {
        private Button _returnToTitleButton;

        public void Present(Action returnToTitle)
        {
            EnsureRuntimeContent();
            _returnToTitleButton.onClick.RemoveAllListeners();
            if (returnToTitle != null)
            {
                _returnToTitleButton.onClick.AddListener(returnToTitle.Invoke);
            }
        }

        private void EnsureRuntimeContent()
        {
            if (_returnToTitleButton != null)
            {
                return;
            }

            var buttonObject = new GameObject(
                "ReturnToTitleButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.layer = gameObject.layer;
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.SetParent(transform, false);
            buttonRect.anchorMin = new Vector2(0.34f, 0.40f);
            buttonRect.anchorMax = new Vector2(0.66f, 0.54f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            buttonObject.GetComponent<Image>().color = GameUiPalette.ButtonAccent;
            _returnToTitleButton = buttonObject.GetComponent<Button>();

            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.layer = gameObject.layer;
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(buttonRect, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(8f, 4f);
            label.rectTransform.offsetMax = new Vector2(-8f, -4f);
            if (TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }
            label.text = "タイトルに戻る";
            label.fontSize = 24f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = GameUiPalette.OnAccentText;
            label.raycastTarget = false;
        }
    }
}