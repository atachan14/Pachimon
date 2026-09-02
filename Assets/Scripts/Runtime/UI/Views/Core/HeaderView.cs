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
        private const string BadgeIconResourcePath = "UI/Badge";
        private const string LeftPaneIconResourcePath = "UI/LeftPane";
        private const string RightPaneIconResourcePath = "UI/RightPane";
        private ItemInventory _itemInventory;

        private static readonly Color ItemCountColor =
            new Color32(0xD7, 0x35, 0x2A, 0xFF);

        [SerializeField] private TMP_Text _itemCountText;
        [SerializeField] private TMP_Text _badgeCountText;
        private Image _goldIcon;
        private Image _badgeIcon;
        private Transform _badgeArea;

        [field: SerializeField] public TMP_Text GoldText { get; private set; }
        [field: SerializeField] public Button MapButton { get; private set; }
        [field: SerializeField] public Button ItemButton { get; private set; }
        [field: SerializeField] public Button SettingsButton { get; private set; }
        [field: SerializeField] public Button LeftPaneButton { get; private set; }
        [field: SerializeField] public Button RightPaneButton { get; private set; }
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
            EnsureBadgeCountDisplay();
            ConfigureItemCountText();
            LogMissingReferences();
        }

        public void SetRunSummary(int gold, int badgeCount)
        {
            if (GoldText != null)
            {
                GoldText.text = gold.ToString();
                LayoutRebuilder.MarkLayoutForRebuild(GoldText.rectTransform);
                if (GoldText.transform.parent is RectTransform goldArea)
                {
                    LayoutRebuilder.MarkLayoutForRebuild(goldArea);
                }
            }

            EnsureBadgeCountDisplay();
            if (_badgeCountText != null)
            {
                _badgeCountText.text = $"{Math.Max(0, badgeCount)}\u500b";
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
            Action onLeftClicked,
            Action onRightClicked)
        {
            var goldArea = GoldText != null ? GoldText.transform.parent : null;
            var leftParent = goldArea != null ? goldArea.parent : transform;
            var rightParent = SettingsButton != null
                ? SettingsButton.transform.parent
                : transform;

            LeftPaneButton ??= CreateCompactPaneButton(
                "LeftPaneButton",
                LeftPaneIconResourcePath,
                leftParent);
            RightPaneButton ??= CreateCompactPaneButton(
                "RightPaneButton",
                RightPaneIconResourcePath,
                rightParent);

            ConfigureButton(LeftPaneButton, onLeftClicked);
            ConfigureButton(RightPaneButton, onRightClicked);

            if (goldArea != null
                && LeftPaneButton.transform.parent == goldArea.parent)
            {
                LeftPaneButton.transform.SetAsFirstSibling();
                goldArea.SetSiblingIndex(1);
            }
            if (SettingsButton != null)
            {
                RightPaneButton.transform.SetAsLastSibling();
            }
        }

        public void SetCompactPaneButtonsVisible(bool visible)
        {
            if (LeftPaneButton != null) LeftPaneButton.gameObject.SetActive(visible);
            if (RightPaneButton != null) RightPaneButton.gameObject.SetActive(visible);
        }

        public void SetCompactPaneSelection(CompactPane pane)
        {
            SetPaneButtonColor(LeftPaneButton, pane == CompactPane.Left);
            SetPaneButtonColor(RightPaneButton, pane == CompactPane.Right);
        }

        public void ApplyLayoutMode(LayoutMode _)
        {
            EnsureBadgeCountDisplay();
            PreserveLayoutControlledFontSize(GoldText);
            PreserveLayoutControlledFontSize(_badgeCountText);
            PreserveLayoutControlledFontSize(_itemCountText);
        }

        private static void PreserveLayoutControlledFontSize(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            var typography = text.GetComponent<ResponsiveTypographySize>()
                ?? text.gameObject.AddComponent<ResponsiveTypographySize>();
            typography.SetLayoutControlledFontSize(text, text.fontSize);
        }

        private Button CreateCompactPaneButton(
            string objectName,
            string iconResourcePath,
            Transform parent)
        {
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
            image.color = Color.clear;
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            var layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredWidth = 72f;

            var iconObject = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            iconObject.layer = gameObject.layer;
            iconObject.transform.SetParent(buttonObject.transform, false);
            var iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(6f, 6f);
            iconRect.offsetMax = new Vector2(-6f, -6f);

            var icon = iconObject.GetComponent<Image>();
            icon.sprite = Resources.Load<Sprite>(iconResourcePath);
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            if (icon.sprite == null)
            {
                Debug.LogWarning(
                    $"Compact Pane icon was not found at Resources/{iconResourcePath}.",
                    this);
            }

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
                    ? new Color(0.72f, 0.86f, 0.82f, 0.65f)
                    : Color.clear;
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

        private void EnsureBadgeCountDisplay()
        {
            _badgeArea ??= GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == "BadgeArea");
            if (_badgeArea != null)
            {
                _badgeArea.gameObject.SetActive(true);
            }

            _badgeIcon ??= GetComponentsInChildren<Image>(true)
                .FirstOrDefault(image => image.name == "BadgeIcon");
            if (_badgeIcon != null)
            {
                _badgeIcon.sprite ??= Resources.Load<Sprite>(BadgeIconResourcePath);
                _badgeIcon.preserveAspect = true;
                _badgeIcon.raycastTarget = false;
                if (!_badgeIcon.TryGetComponent<LayoutElement>(out _))
                {
                    var iconLayout = _badgeIcon.gameObject.AddComponent<LayoutElement>();
                    iconLayout.preferredWidth = 80f;
                    iconLayout.flexibleWidth = 0f;
                }
            }

            if (_badgeCountText == null)
            {
                _badgeCountText = GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(text =>
                        text.name is "BadgeCountText" or "BadgeText");
            }
            if (_badgeCountText == null && GoldText != null)
            {
                var parent = _badgeArea != null
                    ? _badgeArea
                    : GoldText.transform.parent;
                _badgeCountText = Instantiate(GoldText, parent, false);
                _badgeCountText.name = "BadgeCountText";
                if (_badgeIcon != null)
                {
                    _badgeCountText.transform.SetSiblingIndex(
                        _badgeIcon.transform.GetSiblingIndex() + 1);
                }
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
            if (!_badgeCountText.TryGetComponent<LayoutElement>(out _))
            {
                var layout = _badgeCountText.gameObject.AddComponent<LayoutElement>();
                layout.preferredWidth = 72f;
                layout.flexibleWidth = 0f;
            }
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
