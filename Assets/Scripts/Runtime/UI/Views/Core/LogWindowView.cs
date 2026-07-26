using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class LogWindowView : MonoBehaviour
    {
        private const float OptionAreaMinHeight = 56f;
        private const float OptionButtonWidth = 180f;
        private const float OptionButtonMinWidth = 120f;
        private const float OptionButtonHeight = 44f;
        private const float OptionButtonFontSize = 20f;
        private const float SkillAreaHeight = 176f;
        private const float SkillButtonHeight = 48f;
        private const float MinimumTextLineHeightMultiplier = 1.4f;
        private const float TextHorizontalPadding = 14f;
        private const float TextVerticalPadding = 12f;
        private static readonly Color SelectedSkillBorderColor =
            new(0.96f, 0.62f, 0.12f, 1f);

        [field: SerializeField] public TMP_Text TextLogText { get; private set; }
        [field: SerializeField] public RectTransform SelectGridRoot { get; private set; }
        private RectTransform _runtimeOptionContainer;
        private RectTransform _contentRoot;
        private LayoutElement _textLogLayout;
        private LayoutElement _selectGridLayout;
        private RectTransform _advanceOverlay;
        private UnityAction _advanceAction;
        private bool _layoutRefreshPending;
        private readonly Dictionary<int, Outline> _skillOptionOutlines = new();

        private void OnRectTransformDimensionsChange()
        {
            if (TextLogText != null && SelectGridRoot != null)
            {
                RequestLayoutRefresh();
            }
        }

        private void LateUpdate()
        {
            if (!_layoutRefreshPending || _contentRoot == null)
            {
                return;
            }

            _layoutRefreshPending = false;
            Canvas.ForceUpdateCanvases();
            UpdateTextPreferredHeight(TextLogText != null ? TextLogText.text : string.Empty);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRoot);
            if (_runtimeOptionContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_runtimeOptionContainer);
            }
        }

        public void Initialize(TMP_Text textLogText, RectTransform selectGridRoot)
        {
            TextLogText = textLogText;
            SelectGridRoot = selectGridRoot;
            EnsureLogLayout();
        }

        public void SetLogText(string text)
        {
            if (TextLogText != null)
            {
                ApplyDefaultFont(TextLogText);
                TextLogText.color = Color.black;
                TextLogText.alpha = 1f;
                TextLogText.faceColor = Color.white;
                TextLogText.overrideColorTags = true;
                TextLogText.canvasRenderer.SetAlpha(1f);
                TextLogText.text = text;
                UpdateTextPreferredHeight(text);
                RequestLayoutRefresh();
            }
        }

        public void ClearOptions()
        {
            HideAdvancePrompt();
            _skillOptionOutlines.Clear();
            if (SelectGridRoot == null)
            {
                return;
            }

            EnsureRuntimeOptionContainer();
            for (var i = _runtimeOptionContainer.childCount - 1; i >= 0; i--)
            {
                var option = _runtimeOptionContainer.GetChild(i).gameObject;
                option.SetActive(false);
                Destroy(option);
            }

            SetOptionAreaVisible(false);
        }

        public void ShowAdvancePrompt(UnityAction action)
        {
            ClearOptions();
            EnsureAdvanceOverlay();
            _advanceAction = action;
            _advanceOverlay.gameObject.SetActive(action != null);
            _advanceOverlay.SetAsLastSibling();
        }

        public void ShowSingleOption(string label, UnityAction action)
        {
            ShowOptions(new LogWindowOption(label, action));
        }

        public void ShowOptions(params LogWindowOption[] options)
        {
            ClearOptions();
            EnsureRuntimeOptionContainer();
            var horizontalLayout = _runtimeOptionContainer.GetComponent<HorizontalLayoutGroup>();
            if (horizontalLayout != null)
            {
                horizontalLayout.enabled = true;
            }

            if (options == null)
            {
                return;
            }

            for (var i = 0; i < options.Length; i++)
            {
                CreateOptionButton(i, options[i].Label, options[i].Action);
            }

            SetOptionAreaVisible(options.Length > 0);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_runtimeOptionContainer);
        }

        public void ShowSkillOptions(
            IReadOnlyList<LogWindowSkillOption> skills,
            LogWindowOption? struggleOption = null)
        {
            ClearOptions();
            EnsureRuntimeOptionContainer();
            var horizontalLayout = _runtimeOptionContainer.GetComponent<HorizontalLayoutGroup>();
            if (horizontalLayout != null)
            {
                horizontalLayout.enabled = false;
            }

            var gridObject = new GameObject(
                "RuntimeSkillGrid",
                typeof(RectTransform),
                typeof(GridLayoutGroup));
            gridObject.layer = SelectGridRoot.gameObject.layer;
            var gridRect = gridObject.GetComponent<RectTransform>();
            gridRect.SetParent(_runtimeOptionContainer, false);
            Stretch(gridRect, new Vector2(8f, 6f), new Vector2(-8f, -6f));

            var grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(6, 6, 6, 6);
            grid.spacing = new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
            var availableWidth = Mathf.Max(420f, SelectGridRoot.rect.width - 44f);
            grid.cellSize = new Vector2((availableWidth - 16f) / 3f, SkillButtonHeight);

            if (skills != null)
            {
                for (var index = 0; index < skills.Count; index++)
                {
                    var option = skills[index];
                    CreateOptionButton(
                        gridRect,
                        index,
                        option.Label,
                        option.Action,
                        option.IsInteractable,
                        false);
                    RegisterSkillSelectionOutline(
                        gridRect.GetChild(gridRect.childCount - 1)?.gameObject,
                        option.SelectionId);
                }
            }

            if (struggleOption.HasValue)
            {
                CreateStruggleOverlay(struggleOption.Value);
            }

            SetOptionAreaVisible(true, SkillAreaHeight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
        }

        public void SetSelectedSkillOption(int? selectionId)
        {
            foreach (var pair in _skillOptionOutlines)
            {
                if (pair.Value != null)
                {
                    pair.Value.enabled =
                        selectionId.HasValue && pair.Key == selectionId.Value;
                }
            }
        }

        private void CreateOptionButton(
            int index,
            string label,
            UnityAction action)
        {
            EnsureRuntimeOptionContainer();
            CreateOptionButton(
                _runtimeOptionContainer,
                index,
                label,
                action,
                true,
                false);
        }

        private void CreateOptionButton(
            RectTransform parent,
            int index,
            string label,
            UnityAction action,
            bool isInteractable,
            bool isEmphasized)
        {
            if (parent == null)
            {
                return;
            }

            var buttonObject = new GameObject(
                $"RuntimeOptionButton{index + 1}",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            buttonObject.layer = SelectGridRoot.gameObject.layer;
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = isInteractable
                ? Color.black
                : new Color(0.58f, 0.58f, 0.58f, 1f);

            var button = buttonObject.GetComponent<Button>();
            button.interactable = isInteractable;
            button.onClick.RemoveAllListeners();
            if (isInteractable && action != null)
            {
                button.onClick.AddListener(action);
            }

            var layoutElement = buttonObject.GetComponent<LayoutElement>();
            layoutElement.minWidth = isEmphasized ? 220f : OptionButtonMinWidth;
            layoutElement.minHeight = isEmphasized ? 62f : OptionButtonHeight;
            layoutElement.preferredWidth = isEmphasized ? 260f : OptionButtonWidth;
            layoutElement.preferredHeight = isEmphasized ? 62f : OptionButtonHeight;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.layer = SelectGridRoot.gameObject.layer;
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelText = labelObject.GetComponent<TextMeshProUGUI>();
            ApplyDefaultFont(labelText);

            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontSize = isEmphasized
                ? OptionButtonFontSize + 4f
                : OptionButtonFontSize;
            labelText.color = isInteractable
                ? Color.white
                : new Color(0.25f, 0.25f, 0.25f, 1f);
            labelText.alpha = 1f;
            labelText.faceColor = Color.white;
            labelText.overrideColorTags = true;
            labelText.fontStyle = FontStyles.Bold;
            labelText.canvasRenderer.SetAlpha(1f);
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.overflowMode = TextOverflowModes.Overflow;
            labelText.text = label;

            var labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 0f);
            labelRect.offsetMax = new Vector2(-10f, 0f);
        }

        private void CreateStruggleOverlay(LogWindowOption option)
        {
            var overlayObject = new GameObject(
                "StruggleOverlay",
                typeof(RectTransform),
                typeof(Image));
            overlayObject.layer = SelectGridRoot.gameObject.layer;
            var overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.SetParent(_runtimeOptionContainer, false);
            Stretch(overlayRect, Vector2.zero, Vector2.zero);
            var overlayImage = overlayObject.GetComponent<Image>();
            overlayImage.color = new Color(1f, 1f, 1f, 0.72f);
            overlayImage.raycastTarget = true;

            CreateOptionButton(
                overlayRect,
                0,
                option.Label,
                option.Action,
                true,
                true);
            RegisterSkillSelectionOutline(
                overlayRect.GetChild(overlayRect.childCount - 1)?.gameObject,
                0);
            var buttonRect = overlayRect.GetChild(0) as RectTransform;
            if (buttonRect != null)
            {
                buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
                buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
                buttonRect.pivot = new Vector2(0.5f, 0.5f);
                buttonRect.anchoredPosition = Vector2.zero;
                buttonRect.sizeDelta = new Vector2(260f, 62f);
            }
        }

        private void RegisterSkillSelectionOutline(
            GameObject buttonObject,
            int selectionId)
        {
            if (buttonObject == null)
            {
                return;
            }

            var outline = buttonObject.GetComponent<Outline>()
                ?? buttonObject.AddComponent<Outline>();
            outline.effectColor = SelectedSkillBorderColor;
            outline.effectDistance = new Vector2(4f, -4f);
            outline.useGraphicAlpha = false;
            outline.enabled = false;
            _skillOptionOutlines[selectionId] = outline;
        }

        private static void ApplyDefaultFont(TMP_Text text)
        {
            if (text != null && TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }
        }

        private void EnsureAdvanceOverlay()
        {
            if (_advanceOverlay != null)
            {
                return;
            }

            var overlayObject = new GameObject(
                "RuntimeAdvanceOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            overlayObject.layer = gameObject.layer;
            _advanceOverlay = overlayObject.GetComponent<RectTransform>();
            _advanceOverlay.SetParent(transform, false);
            Stretch(_advanceOverlay, Vector2.zero, Vector2.zero);

            var layoutElement = overlayObject.GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            var image = overlayObject.GetComponent<Image>();
            image.color = GameUiPalette.Transparent;
            image.raycastTarget = true;

            var button = overlayObject.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(InvokeAdvanceAction);

            var indicatorObject = new GameObject(
                "Indicator",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            indicatorObject.layer = gameObject.layer;
            indicatorObject.transform.SetParent(_advanceOverlay, false);
            var indicator = indicatorObject.GetComponent<TextMeshProUGUI>();
            ApplyDefaultFont(indicator);
            indicator.text = "▼";
            indicator.fontSize = 24f;
            indicator.fontStyle = FontStyles.Bold;
            indicator.alignment = TextAlignmentOptions.Center;
            indicator.color = Color.black;
            indicator.raycastTarget = false;

            var indicatorRect = indicator.rectTransform;
            indicatorRect.anchorMin = Vector2.one;
            indicatorRect.anchorMax = Vector2.one;
            indicatorRect.pivot = Vector2.one;
            indicatorRect.anchoredPosition = new Vector2(-12f, -8f);
            indicatorRect.sizeDelta = new Vector2(44f, 36f);

            _advanceOverlay.gameObject.SetActive(false);
        }

        private void InvokeAdvanceAction()
        {
            var action = _advanceAction;
            HideAdvancePrompt();
            action?.Invoke();
        }

        private void HideAdvancePrompt()
        {
            _advanceAction = null;
            if (_advanceOverlay != null)
            {
                _advanceOverlay.gameObject.SetActive(false);
            }
        }

        private void EnsureRuntimeOptionContainer()
        {
            if (_runtimeOptionContainer != null || SelectGridRoot == null)
            {
                return;
            }

            var containerObject = new GameObject(
                "RuntimeOptionContainer",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup));
            containerObject.layer = SelectGridRoot.gameObject.layer;
            _runtimeOptionContainer = containerObject.GetComponent<RectTransform>();
            _runtimeOptionContainer.SetParent(SelectGridRoot, false);
            _runtimeOptionContainer.anchorMin = Vector2.zero;
            _runtimeOptionContainer.anchorMax = Vector2.one;
            _runtimeOptionContainer.offsetMin = new Vector2(8f, 6f);
            _runtimeOptionContainer.offsetMax = new Vector2(-8f, -6f);

            var layout = containerObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            for (var index = 0; index < SelectGridRoot.childCount; index++)
            {
                var child = SelectGridRoot.GetChild(index);
                if (child != _runtimeOptionContainer)
                {
                    child.gameObject.SetActive(false);
                }
            }
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

        private void EnsureLogLayout()
        {
            if (SelectGridRoot == null || TextLogText == null)
            {
                return;
            }

            _contentRoot = SelectGridRoot.parent as RectTransform;
            if (_contentRoot == null || TextLogText.transform.parent != _contentRoot)
            {
                return;
            }

            var verticalLayout = _contentRoot.GetComponent<VerticalLayoutGroup>()
                ?? _contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 8f;
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = true;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;

            _textLogLayout = TextLogText.GetComponent<LayoutElement>()
                ?? TextLogText.gameObject.AddComponent<LayoutElement>();
            TextLogText.margin = new Vector4(
                TextHorizontalPadding,
                TextVerticalPadding,
                TextHorizontalPadding,
                TextVerticalPadding);
            _textLogLayout.minHeight = 0f;
            _textLogLayout.flexibleHeight = 0f;
            UpdateTextPreferredHeight(TextLogText.text);

            _selectGridLayout = SelectGridRoot.GetComponent<LayoutElement>()
                ?? SelectGridRoot.gameObject.AddComponent<LayoutElement>();
            _selectGridLayout.minHeight = 0f;
            _selectGridLayout.preferredHeight = 0f;
            _selectGridLayout.flexibleHeight = 0f;
        }

        private void SetOptionAreaVisible(
            bool isVisible,
            float visibleHeight = OptionAreaMinHeight)
        {
            EnsureLogLayout();
            if (_selectGridLayout == null)
            {
                return;
            }

            _selectGridLayout.minHeight = isVisible ? visibleHeight : 0f;
            _selectGridLayout.preferredHeight = isVisible ? visibleHeight : 0f;
            _selectGridLayout.flexibleHeight = isVisible ? 1f : 0f;
            if (_selectGridLayout.transform.parent is RectTransform parent)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            }

            RequestLayoutRefresh();
        }

        private void UpdateTextPreferredHeight(string text)
        {
            if (TextLogText == null || _textLogLayout == null)
            {
                return;
            }

            var width = Mathf.Max(1f, TextLogText.rectTransform.rect.width);
            var preferred = TextLogText.GetPreferredValues(text ?? string.Empty, width, 0f);
            _textLogLayout.preferredHeight = Mathf.Max(
                TextLogText.fontSize * MinimumTextLineHeightMultiplier,
                preferred.y);
        }

        private void RequestLayoutRefresh()
        {
            _layoutRefreshPending = true;
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

    public readonly struct LogWindowSkillOption
    {
        public LogWindowSkillOption(
            int selectionId,
            string label,
            bool isInteractable,
            UnityAction action)
        {
            SelectionId = selectionId;
            Label = label;
            IsInteractable = isInteractable;
            Action = action;
        }

        public int SelectionId { get; }
        public string Label { get; }
        public bool IsInteractable { get; }
        public UnityAction Action { get; }
    }
}
