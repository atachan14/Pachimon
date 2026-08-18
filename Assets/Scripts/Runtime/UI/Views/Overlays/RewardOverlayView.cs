using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Reward;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public enum RewardSelectionKind
    {
        Passive = 0,
        Skill = 1,
    }

    public sealed class RewardChoiceContent
    {
        public RewardChoiceContent(int id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public int Id { get; }
        public string DisplayName { get; }
    }

    public sealed class RewardSourcePachimonContent
    {
        public RewardSourcePachimonContent(
            string displayName,
            Sprite frontSprite,
            IEnumerable<RewardChoiceContent> skills,
            IEnumerable<RewardChoiceContent> passives)
        {
            DisplayName = displayName;
            FrontSprite = frontSprite;
            Skills = skills?.ToArray() ?? Array.Empty<RewardChoiceContent>();
            Passives = passives?.ToArray() ?? Array.Empty<RewardChoiceContent>();
        }

        public string DisplayName { get; }
        public Sprite FrontSprite { get; }
        public IReadOnlyList<RewardChoiceContent> Skills { get; }
        public IReadOnlyList<RewardChoiceContent> Passives { get; }
    }

    public sealed class RewardTargetPachimonContent
    {
        public RewardTargetPachimonContent(
            string instanceId,
            string displayName,
            Sprite frontSprite)
        {
            InstanceId = instanceId;
            DisplayName = displayName;
            FrontSprite = frontSprite;
        }

        public string InstanceId { get; }
        public string DisplayName { get; }
        public Sprite FrontSprite { get; }
    }

    public sealed class RewardOverlayContent
    {
        public RewardOverlayContent(
            int gold,
            bool usesBadge,
            IReadOnlyList<RewardSourcePachimonContent> sources,
            IReadOnlyList<RewardTargetPachimonContent> targets,
            Func<BattleRewardSlot, bool> claimImmediate,
            Func<RewardSelectionKind, int, string, bool> canGrant,
            Func<RewardSelectionKind, int, string, bool> grant,
            Action completed)
        {
            Gold = gold;
            UsesBadge = usesBadge;
            Sources = sources ?? Array.Empty<RewardSourcePachimonContent>();
            Targets = targets ?? Array.Empty<RewardTargetPachimonContent>();
            ClaimImmediate = claimImmediate;
            CanGrant = canGrant;
            Grant = grant;
            Completed = completed;
        }

        public int Gold { get; }
        public bool UsesBadge { get; }
        public IReadOnlyList<RewardSourcePachimonContent> Sources { get; }
        public IReadOnlyList<RewardTargetPachimonContent> Targets { get; }
        public Func<BattleRewardSlot, bool> ClaimImmediate { get; }
        public Func<RewardSelectionKind, int, string, bool> CanGrant { get; }
        public Func<RewardSelectionKind, int, string, bool> Grant { get; }
        public Action Completed { get; }
    }

    public sealed class RewardOverlayView : MonoBehaviour
    {
        private const float OpenDuration = 0.45f;
        private const float RewardButtonCloseDuration = 0.22f;
        private const float SelectionOpenDuration = 0.25f;
        private const float SelectionCloseDuration = 0.38f;
        private static readonly Color RewardButtonColor =
            new Color32(45, 57, 61, 255);
        private static readonly Color EnabledTargetColor =
            new Color32(225, 235, 218, 255);
        private static readonly Color DisabledTargetColor =
            new Color32(175, 178, 175, 255);

        [field: SerializeField] public TMP_Text TitleText { get; private set; }
        [field: SerializeField] public TMP_Text BodyText { get; private set; }

        private readonly Dictionary<BattleRewardSlot, Button> _rewardButtons = new();
        private readonly List<TargetButtonBinding> _targetButtons = new();
        private RectTransform _runtimeRoot;
        private RectTransform _buttonContainer;
        private RectTransform _selectionRoot;
        private RectTransform _targetGrid;
        private ScrollRect _selectionScrollRect;
        private TMP_Text _selectionStatusText;
        private CanvasGroup _canvasGroup;
        private RewardOverlayContent _content;
        private RewardSelectionKind _selectionKind;
        private int _selectedChoiceId;
        private int _claimedCount;
        private bool _isClosing;

        private sealed class TargetButtonBinding
        {
            public TargetButtonBinding(Button button, Image graphic, TMP_Text label)
            {
                Button = button;
                Graphic = graphic;
                Label = label;
            }

            public Button Button { get; }
            public Image Graphic { get; }
            public TMP_Text Label { get; }
        }

        public void Initialize(TMP_Text titleText, TMP_Text bodyText)
        {
            TitleText = titleText;
            BodyText = bodyText;
        }

        public void Present(RewardOverlayContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _claimedCount = 0;
            _isClosing = false;
            gameObject.SetActive(true);
            EnsureRuntimeRoot();
            PrepareMainContent();
            StartCoroutine(AnimateOpen());
        }

        public void Open()
        {
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void EnsureRuntimeRoot()
        {
            var rect = transform as RectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(36f, 36f);
            rect.offsetMax = new Vector2(-36f, -36f);

            var background = GetComponent<Image>();
            if (background == null)
            {
                background = gameObject.AddComponent<Image>();
            }

            background.color = new Color32(250, 245, 226, 255);
            background.raycastTarget = true;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            if (_runtimeRoot != null)
            {
                return;
            }

            var rootObject = new GameObject("RuntimeRewardContent", typeof(RectTransform));
            rootObject.layer = gameObject.layer;
            _runtimeRoot = rootObject.GetComponent<RectTransform>();
            _runtimeRoot.SetParent(transform, false);
            Stretch(_runtimeRoot, new Vector2(28f, 24f), new Vector2(-28f, -24f));
        }

        private void PrepareMainContent()
        {
            if (_buttonContainer == null)
            {
                BuildMainContent();
            }

            if (_selectionRoot != null)
            {
                _selectionRoot.gameObject.SetActive(false);
                Destroy(_selectionRoot.gameObject);
                _selectionRoot = null;
                _targetButtons.Clear();
            }

            SetButtonLabel(
                _rewardButtons[BattleRewardSlot.Gold],
                $"Gold  +{_content.Gold}");
            SetButtonLabel(
                _rewardButtons[BattleRewardSlot.Secondary],
                _content.UsesBadge ? "バッジ" : "ステータス");
            foreach (var button in _rewardButtons.Values)
            {
                button.gameObject.SetActive(true);
                button.interactable = true;
                button.transform.localScale = Vector3.one;
                button.transform.localRotation = Quaternion.identity;
            }
        }

        private void BuildMainContent()
        {
            _rewardButtons.Clear();

            TitleText = CreateText(
                "RewardTitle",
                _runtimeRoot,
                "大盤振る舞いだ！",
                34f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            var titleRect = TitleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0.78f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var buttonsObject = new GameObject(
                "RewardButtons",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup));
            buttonsObject.layer = gameObject.layer;
            _buttonContainer = buttonsObject.GetComponent<RectTransform>();
            _buttonContainer.SetParent(_runtimeRoot, false);
            _buttonContainer.anchorMin = new Vector2(0.16f, 0.08f);
            _buttonContainer.anchorMax = new Vector2(0.84f, 0.78f);
            _buttonContainer.offsetMin = Vector2.zero;
            _buttonContainer.offsetMax = Vector2.zero;

            var layout = buttonsObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateRewardButton(
                BattleRewardSlot.Gold,
                $"Gold  +{_content.Gold}",
                () => ClaimImmediate(BattleRewardSlot.Gold));
            CreateRewardButton(
                BattleRewardSlot.Secondary,
                _content.UsesBadge ? "バッジ" : "ステータス",
                () => ClaimImmediate(BattleRewardSlot.Secondary));
            CreateRewardButton(
                BattleRewardSlot.Passive,
                "パッシヴ",
                () => OpenSelection(RewardSelectionKind.Passive));
            CreateRewardButton(
                BattleRewardSlot.Skill,
                "スキル",
                () => OpenSelection(RewardSelectionKind.Skill));
        }

        private void CreateRewardButton(
            BattleRewardSlot slot,
            string label,
            UnityAction action)
        {
            var button = CreateButton(
                $"Reward{slot}Button",
                _buttonContainer,
                label,
                action,
                RewardButtonColor,
                Color.white);
            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 58f;
            layout.minHeight = 50f;
            layout.flexibleWidth = 1f;
            _rewardButtons[slot] = button;
        }

        private void ClaimImmediate(BattleRewardSlot slot)
        {
            if (_content.ClaimImmediate?.Invoke(slot) == true)
            {
                StartCoroutine(AnimateRewardButtonClaim(_rewardButtons[slot]));
            }
        }

        private void OpenSelection(RewardSelectionKind kind)
        {
            if (_selectionRoot != null)
            {
                return;
            }

            _selectionKind = kind;
            _selectedChoiceId = 0;
            BuildSelectionWindow();
            StartCoroutine(AnimateSelectionOpen());
        }

        private void BuildSelectionWindow()
        {
            _targetButtons.Clear();
            var selectionObject = new GameObject(
                "RewardSelectionWindow",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup));
            selectionObject.layer = gameObject.layer;
            _selectionRoot = selectionObject.GetComponent<RectTransform>();
            _selectionRoot.SetParent(_runtimeRoot, false);
            _selectionRoot.anchorMin = new Vector2(0.03f, 0.03f);
            _selectionRoot.anchorMax = new Vector2(0.97f, 0.97f);
            _selectionRoot.offsetMin = Vector2.zero;
            _selectionRoot.offsetMax = Vector2.zero;
            _selectionRoot.SetAsLastSibling();
            selectionObject.GetComponent<Image>().color = new Color32(243, 247, 242, 255);

            var heading = CreateText(
                "SelectionTitle",
                _selectionRoot,
                _selectionKind == RewardSelectionKind.Skill
                    ? "取得するスキルを選択"
                    : "取得するパッシヴを選択",
                26f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            SetAnchors(heading.rectTransform, new Vector2(0f, 0.9f), Vector2.one);

            _selectionStatusText = CreateText(
                "SelectionStatus",
                _selectionRoot,
                "Enemyから候補を選んでください",
                18f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);
            SetAnchors(
                _selectionStatusText.rectTransform,
                new Vector2(0f, 0.84f),
                new Vector2(1f, 0.9f));

            _selectionScrollRect = CreateScrollView(_selectionRoot, out var contentRect);
            BuildSelectionContent(contentRect);
        }

        private void BuildSelectionContent(RectTransform contentRect)
        {
            var vertical = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(18, 18, 16, 24);
            vertical.spacing = 18f;
            vertical.childAlignment = TextAnchor.UpperCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
            var fitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateSectionLabel(contentRect, "Enemy");
            var sourceGrid = CreateThreeColumnGrid(
                "EnemyRewardGrid",
                contentRect,
                GetSourceGridHeight());
            foreach (var source in _content.Sources)
            {
                BuildSourceColumn(sourceGrid, source);
            }

            CreateSectionLabel(contentRect, "Player");
            _targetGrid = CreateThreeColumnGrid("PlayerTargetGrid", contentRect, 210f);
            RebuildTargetGrid();
        }

        private void BuildSourceColumn(
            RectTransform sourceGrid,
            RewardSourcePachimonContent source)
        {
            var columnObject = new GameObject(
                "RewardSource",
                typeof(RectTransform),
                typeof(Image),
                typeof(VerticalLayoutGroup));
            columnObject.layer = gameObject.layer;
            columnObject.transform.SetParent(sourceGrid, false);
            columnObject.GetComponent<Image>().color = new Color32(232, 238, 230, 255);
            var column = columnObject.GetComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(6, 6, 8, 8);
            column.spacing = 5f;
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            CreatePachimonGraphic(columnObject.transform, source.FrontSprite, 92f);
            var name = CreateText(
                "Name",
                columnObject.transform,
                source.DisplayName,
                17f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

            var choices = _selectionKind == RewardSelectionKind.Skill
                ? source.Skills
                : source.Passives;
            foreach (var choice in choices)
            {
                var capturedChoice = choice;
                var button = CreateButton(
                    $"RewardChoice{choice.Id}",
                    columnObject.transform,
                    choice.DisplayName,
                    () => SelectChoice(capturedChoice),
                    new Color32(55, 71, 75, 255),
                    Color.white);
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
            }
        }

        private void SelectChoice(RewardChoiceContent choice)
        {
            _selectedChoiceId = choice.Id;
            _selectionStatusText.text = $"{choice.DisplayName}：覚えさせるパチモンを選択";
            RebuildTargetGrid();
            StartCoroutine(ScrollToPlayerTargets());
        }

        private void RebuildTargetGrid()
        {
            foreach (var binding in _targetButtons)
            {
                binding.Button.gameObject.SetActive(false);
            }

            for (var index = 0; index < _content.Targets.Count; index++)
            {
                var target = _content.Targets[index];
                var canGrant = _selectedChoiceId > 0
                    && _content.CanGrant?.Invoke(
                        _selectionKind,
                        _selectedChoiceId,
                        target.InstanceId) == true;
                var binding = GetOrCreateTargetButton(index);
                BindTargetButton(binding, target, canGrant);
            }
        }

        private TargetButtonBinding GetOrCreateTargetButton(int index)
        {
            while (_targetButtons.Count <= index)
            {
                _targetButtons.Add(CreateTargetButton(_targetButtons.Count));
            }

            return _targetButtons[index];
        }

        private TargetButtonBinding CreateTargetButton(int index)
        {
            var button = CreateButton(
                $"RewardTarget_{index + 1}",
                _targetGrid,
                string.Empty,
                null,
                DisabledTargetColor,
                Color.black);
            var graphic = CreatePachimonGraphic(button.transform, null, 116f);
            graphic.transform.SetAsFirstSibling();

            var layout = button.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 8, 8);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var label = button.GetComponentInChildren<TMP_Text>();
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
            return new TargetButtonBinding(button, graphic, label);
        }

        private void BindTargetButton(
            TargetButtonBinding binding,
            RewardTargetPachimonContent target,
            bool canGrant)
        {
            binding.Button.gameObject.name = $"RewardTarget{target.InstanceId}";
            binding.Button.gameObject.SetActive(true);
            binding.Button.interactable = canGrant;
            binding.Button.targetGraphic.color = canGrant
                ? EnabledTargetColor
                : DisabledTargetColor;
            binding.Graphic.sprite = target.FrontSprite;
            binding.Graphic.enabled = target.FrontSprite != null;
            binding.Label.text = target.DisplayName;
            binding.Button.onClick.RemoveAllListeners();
            binding.Button.onClick.AddListener(() => GrantToTarget(target));
        }

        private void GrantToTarget(RewardTargetPachimonContent target)
        {
            if (_selectedChoiceId <= 0
                || _content.Grant?.Invoke(
                    _selectionKind,
                    _selectedChoiceId,
                    target.InstanceId) != true)
            {
                return;
            }

            var slot = _selectionKind == RewardSelectionKind.Skill
                ? BattleRewardSlot.Skill
                : BattleRewardSlot.Passive;
            StartCoroutine(AnimateSelectionClose(slot));
        }

        private IEnumerator AnimateOpen()
        {
            Canvas.ForceUpdateCanvases();
            var rect = transform as RectTransform;
            var target = rect.anchoredPosition;
            var start = target + Vector2.up * Mathf.Max(Screen.height, rect.rect.height + 100f);
            _canvasGroup.alpha = 1f;
            rect.anchoredPosition = start;

            var elapsed = 0f;
            while (elapsed < OpenDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                rect.anchoredPosition = Vector2.LerpUnclamped(
                    start,
                    target,
                    Smooth(Mathf.Clamp01(elapsed / OpenDuration)));
                yield return null;
            }

            rect.anchoredPosition = target;
        }

        private IEnumerator AnimateRewardButtonClaim(Button button)
        {
            if (button == null || !button.interactable)
            {
                yield break;
            }

            button.interactable = false;
            var rect = button.transform as RectTransform;
            var elapsed = 0f;
            while (elapsed < RewardButtonCloseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                rect.localScale = Vector3.one * (
                    1f - Smooth(Mathf.Clamp01(elapsed / RewardButtonCloseDuration)));
                yield return null;
            }

            button.gameObject.SetActive(false);
            _claimedCount++;
            if (_claimedCount >= 4)
            {
                StartCoroutine(AnimateOverlayClose());
            }
        }

        private IEnumerator AnimateSelectionOpen()
        {
            var canvasGroup = _selectionRoot.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            _selectionRoot.localScale = Vector3.zero;
            var elapsed = 0f;
            while (elapsed < SelectionOpenDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Smooth(Mathf.Clamp01(elapsed / SelectionOpenDuration));
                canvasGroup.alpha = progress;
                _selectionRoot.localScale = Vector3.one * progress;
                yield return null;
            }

            canvasGroup.alpha = 1f;
            _selectionRoot.localScale = Vector3.one;
        }

        private IEnumerator AnimateSelectionClose(BattleRewardSlot slot)
        {
            var closingRoot = _selectionRoot;
            _selectionRoot = null;
            var elapsed = 0f;
            while (elapsed < SelectionCloseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Smooth(Mathf.Clamp01(elapsed / SelectionCloseDuration));
                closingRoot.localScale = Vector3.one * (1f - progress);
                closingRoot.localRotation = Quaternion.Euler(0f, 0f, progress * 360f);
                yield return null;
            }

            Destroy(closingRoot.gameObject);
            _targetButtons.Clear();
            StartCoroutine(AnimateRewardButtonClaim(_rewardButtons[slot]));
        }

        private IEnumerator AnimateOverlayClose()
        {
            if (_isClosing)
            {
                yield break;
            }

            _isClosing = true;
            yield return new WaitForSecondsRealtime(0.12f);
            var rect = transform as RectTransform;
            var start = rect.anchoredPosition;
            var target = start + Vector2.up * Mathf.Max(Screen.height, rect.rect.height + 100f);
            var elapsed = 0f;
            while (elapsed < OpenDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                rect.anchoredPosition = Vector2.LerpUnclamped(
                    start,
                    target,
                    Smooth(Mathf.Clamp01(elapsed / OpenDuration)));
                yield return null;
            }

            var completed = _content.Completed;
            gameObject.SetActive(false);
            completed?.Invoke();
        }

        private IEnumerator ScrollToPlayerTargets()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            var start = _selectionScrollRect.verticalNormalizedPosition;
            const float duration = 0.3f;
            var elapsed = 0f;
            while (elapsed < duration && _selectionScrollRect != null)
            {
                elapsed += Time.unscaledDeltaTime;
                _selectionScrollRect.verticalNormalizedPosition = Mathf.Lerp(
                    start,
                    0f,
                    Smooth(Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            if (_selectionScrollRect != null)
            {
                _selectionScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private ScrollRect CreateScrollView(
            RectTransform parent,
            out RectTransform contentRect)
        {
            var scrollObject = new GameObject(
                "SelectionScroll",
                typeof(RectTransform),
                typeof(ScrollRect));
            scrollObject.layer = gameObject.layer;
            var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.SetParent(parent, false);
            SetAnchors(
                scrollRectTransform,
                new Vector2(0.03f, 0.04f),
                new Vector2(0.97f, 0.84f));

            var viewportObject = new GameObject(
                "Viewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(RectMask2D));
            viewportObject.layer = gameObject.layer;
            var viewport = viewportObject.GetComponent<RectTransform>();
            viewport.SetParent(scrollRectTransform, false);
            Stretch(viewport, Vector2.zero, Vector2.zero);
            viewportObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.55f);

            var contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.layer = gameObject.layer;
            contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.SetParent(viewport, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;
            return scroll;
        }

        private float GetSourceGridHeight()
        {
            var maximumChoices = _content.Sources.Count == 0
                ? 0
                : _content.Sources.Max(source =>
                    _selectionKind == RewardSelectionKind.Skill
                        ? source.Skills.Count
                        : source.Passives.Count);
            return Mathf.Max(220f, 150f + maximumChoices * 40f);
        }

        private RectTransform CreateThreeColumnGrid(
            string objectName,
            RectTransform parent,
            float height)
        {
            var gridObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(GridLayoutGroup),
                typeof(LayoutElement));
            gridObject.layer = gameObject.layer;
            var rect = gridObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var layoutElement = gridObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = height;
            layoutElement.minHeight = height;

            var grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.spacing = new Vector2(12f, 0f);
            grid.padding = new RectOffset(4, 4, 0, 0);
            grid.childAlignment = TextAnchor.UpperCenter;
            var width = Mathf.Max(300f, _runtimeRoot.rect.width - 90f);
            grid.cellSize = new Vector2((width - 32f) / 3f, height);
            return rect;
        }

        private void CreateSectionLabel(RectTransform parent, string text)
        {
            var label = CreateText(
                $"{text}Label",
                parent,
                text,
                22f,
                FontStyles.Bold,
                TextAlignmentOptions.Left);
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
        }

        private Image CreatePachimonGraphic(
            Transform parent,
            Sprite sprite,
            float height)
        {
            var graphicObject = new GameObject(
                "Graphic",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            graphicObject.layer = gameObject.layer;
            graphicObject.transform.SetParent(parent, false);
            var image = graphicObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : GameUiPalette.MissingGraphic;
            image.preserveAspect = true;
            image.raycastTarget = false;
            graphicObject.GetComponent<LayoutElement>().preferredHeight = height;
            return image;
        }

        private static TMP_Text CreateText(
            string objectName,
            Transform parent,
            string text,
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
            var tmp = textObject.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.alignment = alignment;
            tmp.color = Color.black;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button CreateButton(
            string objectName,
            Transform parent,
            string label,
            UnityAction action,
            Color background,
            Color foreground)
        {
            var buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.layer = parent.gameObject.layer;
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<Image>().color = background;
            var button = buttonObject.GetComponent<Button>();
            if (action != null)
            {
                button.onClick.AddListener(action);
            }

            var text = CreateText(
                "Label",
                buttonObject.transform,
                label,
                19f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            text.color = foreground;
            Stretch(text.rectTransform, new Vector2(6f, 2f), new Vector2(-6f, -2f));
            return button;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = label ?? string.Empty;
            }
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

        private static float Smooth(float value)
        {
            return value * value * (3f - 2f * value);
        }
    }
}
