using Pachimon.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class ItemDetailView : MonoBehaviour
    {
        private Image _icon;
        private TMP_Text _title;
        private TMP_Text _description;

        public static ItemDetailView CreateRuntime(RectTransform parent)
        {
            var rootObject = new GameObject(
                "ItemDetailView",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(ItemDetailView));
            rootObject.layer = parent.gameObject.layer;
            var root = rootObject.GetComponent<RectTransform>();
            root.SetParent(parent, false);
            Stretch(root, Vector2.zero, Vector2.zero);
            var view = rootObject.GetComponent<ItemDetailView>();
            view.Build();
            view.Hide();
            return view;
        }

        public void Show(ItemAsset item)
        {
            if (item == null)
            {
                Hide();
                return;
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _icon.sprite = item.Icon;
            _icon.enabled = item.Icon != null;
            _title.text = item.DisplayName;
            _description.text = item.Description;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Build()
        {
            GetComponent<Image>().color = GameUiPalette.LeftPaneBackground;

            var iconObject = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            iconObject.layer = gameObject.layer;
            iconObject.transform.SetParent(transform, false);
            _icon = iconObject.GetComponent<Image>();
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;
            SetAnchors(
                _icon.rectTransform,
                new Vector2(0.18f, 0.58f),
                new Vector2(0.82f, 0.94f));

            _title = CreateText(
                "Title",
                transform,
                28f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            SetAnchors(
                _title.rectTransform,
                new Vector2(0.08f, 0.44f),
                new Vector2(0.92f, 0.58f));

            _description = CreateText(
                "Description",
                transform,
                21f,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft);
            _description.textWrappingMode = TextWrappingModes.Normal;
            SetAnchors(
                _description.rectTransform,
                new Vector2(0.1f, 0.08f),
                new Vector2(0.9f, 0.42f));
        }

        private static TMP_Text CreateText(
            string objectName,
            Transform parent,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.layer = parent.gameObject.layer;
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = GameUiPalette.PrimaryText;
            text.raycastTarget = false;
            return text;
        }

        private static void SetAnchors(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(
            RectTransform rect,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
