using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class HeaderView : MonoBehaviour
    {
        private static readonly BadgePaletteEntry[] BadgePalette =
        {
            new(AllocationType.Fire, "FireArea", "#E84B3C"),
            new(AllocationType.Aqua, "AquaArea", "#356AE0", "WaterArea"),
            new(AllocationType.Leaf, "LeafArea", "#288A47"),
            new(AllocationType.Electric, "ElecArea", "#F2C94C", "ElectricArea"),
            new(AllocationType.Poison, "PoisonArea", "#FFA7DF"),
            new(AllocationType.Ice, "IceArea", "#62D5E6"),
            new(AllocationType.Wind, "WindArea", "#91C83E", "GroundArea"),
            new(AllocationType.Dragon, "DragonArea", "#707887"),
        };

        private readonly Dictionary<AllocationType, TMP_Text> _badgeTexts = new();

        [field: SerializeField] public TMP_Text GoldText { get; private set; }
        [field: FormerlySerializedAs("RowText")]
        [field: SerializeField] public TMP_Text StageText { get; private set; }
        [field: SerializeField] public TMP_Text BadgeText { get; private set; }
        [field: SerializeField] public Button MapButton { get; private set; }
        [field: SerializeField] public Button ItemButton { get; private set; }
        [field: SerializeField] public Button SettingsButton { get; private set; }
        public Button PartyPaneButton { get; private set; }
        public Button InfoPaneButton { get; private set; }

        public void Initialize(
            TMP_Text goldText,
            TMP_Text stageText,
            TMP_Text badgeText,
            Button mapButton,
            Button itemButton,
            Button settingsButton)
        {
            GoldText = goldText;
            StageText = stageText;
            BadgeText = badgeText;
            MapButton = mapButton;
            ItemButton = itemButton;
            SettingsButton = settingsButton;
        }

        private void Awake()
        {
            ApplyPalette();
            InitializeBadgeDetails();
            LogMissingReferences();
        }

        public void SetBadgeCount(AllocationType allocationType, int count)
        {
            if (_badgeTexts.TryGetValue(allocationType, out var badgeText))
            {
                badgeText.text = Mathf.Max(0, count).ToString();
            }
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

        private void InitializeBadgeDetails()
        {
            _badgeTexts.Clear();
            var detailArea = GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == "DetailArea");
            if (detailArea == null)
            {
                Debug.LogWarning("Header Badge DetailArea was not found.", this);
                return;
            }

            for (var index = 0; index < BadgePalette.Length; index++)
            {
                var palette = BadgePalette[index];
                var area = FindDirectChild(detailArea, palette.AreaName, palette.LegacyAreaName);
                if (area == null)
                {
                    Debug.LogWarning($"Header Badge area {palette.AreaName} was not found.", this);
                    continue;
                }

                area.name = palette.AreaName;
                area.SetSiblingIndex(index);

                var icon = area.GetComponentsInChildren<Image>(true)
                    .FirstOrDefault(image => image.name == "BadgeIcon");
                if (icon != null)
                {
                    icon.color = palette.Color;
                }

                var badgeText = area.GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(text => text.name == "BadgeText");
                if (badgeText != null)
                {
                    badgeText.text = "0";
                    badgeText.color = GameUiPalette.HeaderText;
                    _badgeTexts[palette.AllocationType] = badgeText;
                }
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

        private static Transform FindDirectChild(
            Transform parent,
            string areaName,
            string legacyAreaName)
        {
            for (var index = 0; index < parent.childCount; index++)
            {
                var child = parent.GetChild(index);
                if (child.name == areaName || child.name == legacyAreaName)
                {
                    return child;
                }
            }

            return null;
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
            if (StageText == null) missing.Add(nameof(StageText));
            if (BadgeText == null) missing.Add(nameof(BadgeText));
            if (MapButton == null) missing.Add(nameof(MapButton));
            if (ItemButton == null) missing.Add(nameof(ItemButton));
            if (SettingsButton == null) missing.Add(nameof(SettingsButton));

            if (missing.Count == 0)
            {
                return;
            }

            Debug.LogWarning($"{nameof(HeaderView)} on '{name}' is missing references: {string.Join(", ", missing)}", this);
        }

        private readonly struct BadgePaletteEntry
        {
            public BadgePaletteEntry(
                AllocationType allocationType,
                string areaName,
                string htmlColor,
                string legacyAreaName = null)
            {
                AllocationType = allocationType;
                AreaName = areaName;
                LegacyAreaName = legacyAreaName;
                if (!ColorUtility.TryParseHtmlString(htmlColor, out var color))
                {
                    throw new ArgumentException($"Invalid Badge color: {htmlColor}", nameof(htmlColor));
                }

                Color = color;
            }

            public AllocationType AllocationType { get; }
            public string AreaName { get; }
            public string LegacyAreaName { get; }
            public Color Color { get; }
        }
    }
}
