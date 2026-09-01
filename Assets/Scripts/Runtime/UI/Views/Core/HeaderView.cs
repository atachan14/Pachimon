using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class HeaderView : MonoBehaviour
    {
        private ItemInventory _itemInventory;

        private static readonly Color ItemCountColor =
            new Color32(0xD7, 0x35, 0x2A, 0xFF);

        [SerializeField] private TMP_Text _itemCountText;
        [SerializeField] private TMP_Text _badgeCountText;
        private Image _goldIcon;

        [field: SerializeField] public TMP_Text GoldText { get; private set; }
        [field: SerializeField] public Button MapButton { get; private set; }
        [field: SerializeField] public Button ItemButton { get; private set; }
        [field: SerializeField] public Button SettingsButton { get; private set; }
        public Button PartyPaneButton { get; private set; }
        public Button InfoPaneButton { get; private set; }
        public Sprite GoldIconSprite => _goldIcon != null ? _goldIcon.sprite : null;

        public void Initialize(
            TMP_Text goldText,
            Button mapButton,
            Button itemButton,
            Button settingsButton)
        {
            GoldText = goldText;
            MapButton = mapButton;
            ItemButton = itemButton;
            SettingsButton = settingsButton;
        }

        private void Awake()
        {
            _goldIcon = GetComponentsInChildren<Image>(true)
                .FirstOrDefault(image => image.name == "GoldIcon");
            ApplyPalette();
            EnsureBadgeCountText();
            ConfigureItemCountText();
            LogMissingReferences();
        }

        public void SetRunSummary(int gold, int badgeCount)
        {
            if (GoldText != null)
            {
                GoldText.text = gold.ToString();
            }

            EnsureBadgeCountText();
            if (_badgeCountText != null)
            {
                _badgeCountText.text = $"Badge:{Math.Max(0, badgeCount)}個";
            }
        }

        private void OnDestroy()
        {
            if (_itemInventory != null)
            {
                _itemInventory.Changed -= RefreshItemCount;
            }
        }

        public void BindItemInventory(ItemInventory itemInventory)
        {
            if (_itemInventory != null)
            {
                _itemInventory.Changed -= RefreshItemCount;
            }

            _itemInventory = itemInventory;
            if (_itemInventory != null)
            {
                _itemInventory.Changed += RefreshItemCount;
            }

            RefreshItemCount();
        }

        public void SetItemButtonInteractable(bool interactable)
        {
            if (ItemButton == null || ItemButton.interactable == interactable)
            {
                return;
            }

            ItemButton.interactable = interactable;
        }

        public void ConfigureCompactPaneButtons(
            Action onPartyClicked,
            Action onInfoClicked)
        {
            PartyPaneButton ??= CreateCompactPaneButton("PartyPaneButton", "PARTY");
            InfoPaneButton ??= CreateCompactPaneButton("InfoPaneButton", "INFO");

            ConfigureButton(PartyPaneButton, onPartyClicked);
            ConfigureButton(InfoPaneButton, onInfoClicked);

            if (MapButton != null)
            {
                PartyPaneButton.transform.SetSiblingIndex(MapButton.transform.GetSiblingIndex());
                InfoPaneButton.transform.SetSiblingIndex(MapButton.transform.GetSiblingIndex() + 1);
            }
        }

        public void SetCompactPaneButtonsVisible(bool visible)
        {
            if (PartyPaneButton != null) PartyPaneButton.gameObject.SetActive(visible);
            if (InfoPaneButton != null) InfoPaneButton.gameObject.SetActive(visible);
        }

        public void SetCompactPaneSelection(CompactPane pane)
        {
            SetPaneButtonColor(PartyPaneButton, pane == CompactPane.Left);
            SetPaneButtonColor(InfoPaneButton, pane == CompactPane.Right);
        }

        private Button CreateCompactPaneButton(string objectName, string label)
        {
            var parent = MapButton != null ? MapButton.transform.parent : transform;
            var buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            buttonObject.layer = gameObject.layer;
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.72f);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            var layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredWidth = 72f;

            var textObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.layer = gameObject.layer;
            textObject.transform.SetParent(buttonObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 15f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = GameUiPalette.HeaderText;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;

            return button;
        }

        private static void ConfigureButton(Button button, Action onClicked)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            if (onClicked != null)
            {
                button.onClick.AddListener(() => onClicked());
            }
        }

        private static void SetPaneButtonColor(Button button, bool selected)
        {
            if (button != null && button.targetGraphic is Image image)
            {
                image.color = selected
                    ? new Color(0.72f, 0.86f, 0.82f, 1f)
                    : new Color(1f, 1f, 1f, 0.72f);
            }
        }

        private void ApplyPalette()
        {
            if (TryGetComponent<Image>(out var background))
            {
                background.color = GameUiPalette.HeaderBackground;
            }

            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                text.color = GameUiPalette.HeaderText;
            }
        }

        private void ConfigureItemCountText()
        {
            if (_itemCountText == null || ItemButton == null)
            {
                return;
            }

            _itemCountText.gameObject.SetActive(true);
            _itemCountText.enableAutoSizing = false;
            _itemCountText.fontSize = 14f;
            _itemCountText.fontStyle = FontStyles.Bold;
            _itemCountText.color = ItemCountColor;
            _itemCountText.raycastTarget = false;
            _itemCountText.textWrappingMode = TextWrappingModes.NoWrap;
            _itemCountText.overflowMode = TextOverflowModes.Overflow;
            _itemCountText.margin = Vector4.zero;
            _itemCountText.ForceMeshUpdate();
            EnsureItemCountBadge(_itemCountText.rectTransform);
        }

        private void EnsureBadgeCountText()
        {
            if (_badgeCountText == null)
            {
                _badgeCountText = GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(text => text.name == "BadgeCountText");
            }
            if (_badgeCountText == null && GoldText != null)
            {
                _badgeCountText = Instantiate(GoldText, GoldText.transform.parent, false);
                _badgeCountText.name = "BadgeCountText";
                _badgeCountText.transform.SetSiblingIndex(
                    GoldText.transform.GetSiblingIndex() + 1);
            }
            if (_badgeCountText == null)
            {
                return;
            }

            _badgeCountText.gameObject.SetActive(true);
            _badgeCountText.raycastTarget = false;
            _badgeCountText.textWrappingMode = TextWrappingModes.NoWrap;
            _badgeCountText.overflowMode = TextOverflowModes.Overflow;
            _badgeCountText.alignment = TextAlignmentOptions.MidlineLeft;
            var layout = _badgeCountText.GetComponent<LayoutElement>()
                ?? _badgeCountText.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 180f;
        }

        private RectTransform EnsureItemCountBadge(RectTransform textRect)
        {
            if (ItemButton == null || textRect == null)
            {
                return null;
            }

            var badgeTransform = ItemButton.transform.Find("ItemCountBadge");
            if (badgeTransform == null)
            {
                var badgeObject = new GameObject(
                    "ItemCountBadge",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(CircularBadgeGraphic),
                    typeof(LayoutElement));
                badgeObject.layer = ItemButton.gameObject.layer;
                badgeTransform = badgeObject.transform;
                badgeTransform.SetParent(ItemButton.transform, false);
            }

            var badgeRect = (RectTransform)badgeTransform;
            const float badgeSize = 24f;
            var textSize = textRect.rect.size;
            badgeRect.anchorMin = textRect.anchorMin;
            badgeRect.anchorMax = textRect.anchorMax;
            badgeRect.pivot = textRect.pivot;
            badgeRect.anchoredPosition = textRect.anchoredPosition
                + (Vector2.one * 0.5f - textRect.pivot)
                * (textSize - Vector2.one * badgeSize);
            badgeRect.sizeDelta = Vector2.one * badgeSize;
            badgeRect.localRotation = Quaternion.identity;
            badgeRect.localScale = Vector3.one;

            var layoutElement = badgeRect.GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            var badgeGraphic = badgeRect.GetComponent<CircularBadgeGraphic>();
            badgeGraphic.raycastTarget = false;
            badgeGraphic.Configure(Color.white, ItemCountColor, 2f);
            badgeRect.SetSiblingIndex(textRect.GetSiblingIndex());
            return badgeRect;
        }

        private void RefreshItemCount()
        {
            if (_itemCountText == null)
            {
                return;
            }

            _itemCountText.text = (_itemInventory?.Count ?? 0).ToString();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            LogMissingReferences();
        }
#endif

        private void LogMissingReferences()
        {
            var missing = new List<string>();

            if (GoldText == null) missing.Add(nameof(GoldText));
            if (MapButton == null) missing.Add(nameof(MapButton));
            if (ItemButton == null) missing.Add(nameof(ItemButton));
            if (SettingsButton == null) missing.Add(nameof(SettingsButton));

            if (missing.Count == 0)
            {
                return;
            }

            Debug.LogWarning($"{nameof(HeaderView)} on '{name}' is missing references: {string.Join(", ", missing)}", this);
        }

    }
}
