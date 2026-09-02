using System.Linq;
using Pachimon.App;
using Pachimon.Trainer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class TitleSceneInstaller : MonoBehaviour
    {
        private const float CompactLayoutWidth = 600f;
        private static readonly Color TextColor = Color.black;
        private static readonly Color DisabledTextColor = Color.black;

        [Header("Scene References")]
        [SerializeField] private GameObject _titleRoot;
        [SerializeField] private TMP_InputField _playerNameInput;
        [SerializeField] private Button _newGameButton;
        [SerializeField] private TrainerNameCatalog _trainerNameCatalog;

        private string _suggestedPlayerName = NewGameRequest.GuestPlayerName;
        private RectTransform _menuDialogRect;
        private RectTransform _nameInputDialogRect;
        private RectTransform _nameLabelRect;
        private Image _nameInputBorder;
        private Vector2 _lastRootSize = new(float.MinValue, float.MinValue);

        private void Awake()
        {
            EnsureWhiteBackground();
            EnsureMenuDialog();
            EnsurePlayerNameInput();
            RefreshResponsiveLayout();
            ValidateReferences();
            WireButtons();
        }

        private void LateUpdate()
        {
            RefreshResponsiveLayout();
        }

        private void EnsurePlayerNameInput()
        {
            if (_titleRoot == null)
            {
                return;
            }

            if (_playerNameInput == null)
            {
                var inputObject = new GameObject(
                    "PlayerNameInput",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                inputObject.SetActive(false);
                inputObject.layer = _titleRoot.layer;
                var createdInputRect = inputObject.GetComponent<RectTransform>();
                createdInputRect.SetParent(_titleRoot.transform, false);
                var background = inputObject.GetComponent<Image>();
                background.color = Color.white;
                var inputField = inputObject.AddComponent<TMP_InputField>();

                var textArea = CreateRectObject("TextArea", createdInputRect);
                textArea.anchorMin = Vector2.zero;
                textArea.anchorMax = Vector2.one;
                textArea.offsetMin = new Vector2(16f, 8f);
                textArea.offsetMax = new Vector2(-16f, -8f);
                textArea.gameObject.AddComponent<RectMask2D>();

                var inputText = CreateText("Text", textArea, string.Empty, 22f);
                inputText.color = TextColor;

                var placeholder = CreateText(
                    "Placeholder",
                    textArea,
                    NewGameRequest.GuestPlayerName,
                    22f);
                placeholder.color = DisabledTextColor;

                _playerNameInput = inputField;
                _playerNameInput.targetGraphic = background;
                _playerNameInput.textViewport = textArea;
                _playerNameInput.textComponent = inputText;
                _playerNameInput.placeholder = placeholder;
                _playerNameInput.lineType = TMP_InputField.LineType.SingleLine;
                _playerNameInput.contentType = TMP_InputField.ContentType.Standard;
                _playerNameInput.characterLimit = 12;
                inputObject.SetActive(true);
            }

            _playerNameInput.customCaretColor = true;
            _playerNameInput.caretColor = TextColor;
            _playerNameInput.caretWidth = 3;
            _playerNameInput.caretBlinkRate = 0.7f;
            _playerNameInput.selectionColor = new Color(0.96f, 0.55f, 0.20f, 0.35f);
            _playerNameInput.onSelect.RemoveListener(HandleNameInputSelected);
            _playerNameInput.onDeselect.RemoveListener(HandleNameInputDeselected);
            _playerNameInput.onSelect.AddListener(HandleNameInputSelected);
            _playerNameInput.onDeselect.AddListener(HandleNameInputDeselected);

            var border = CreateImageObject("NameInputDialog", _titleRoot.transform, Color.black);
            _nameInputBorder = border;
            var borderRect = border.rectTransform;
            _nameInputDialogRect = borderRect;
            borderRect.anchorMin = new Vector2(0.5f, 0f);
            borderRect.anchorMax = new Vector2(0.5f, 0f);
            borderRect.pivot = new Vector2(0.5f, 0f);
            borderRect.anchoredPosition = new Vector2(0f, 52f);
            borderRect.sizeDelta = new Vector2(328f, 64f);

            var nameLabel = CreateText("NameLabel", (RectTransform)_titleRoot.transform, "お名前：", 22f);
            var nameLabelRect = (RectTransform)nameLabel.transform;
            _nameLabelRect = nameLabelRect;
            nameLabelRect.anchorMin = new Vector2(0.5f, 0f);
            nameLabelRect.anchorMax = new Vector2(0.5f, 0f);
            nameLabelRect.pivot = new Vector2(1f, 0.5f);
            nameLabelRect.anchoredPosition = new Vector2(-176f, 84f);
            nameLabelRect.sizeDelta = new Vector2(120f, 64f);
            nameLabel.alignment = TextAlignmentOptions.MidlineRight;
            nameLabel.color = TextColor;

            var inputRect = (RectTransform)_playerNameInput.transform;
            inputRect.SetParent(borderRect, false);
            SetStretch(inputRect, 4f);

            _suggestedPlayerName = ChooseSuggestedPlayerName();
            if (_playerNameInput.placeholder is TMP_Text placeholderText)
            {
                placeholderText.text = _suggestedPlayerName;
                placeholderText.color = Color.black;
            }

            if (_newGameButton != null)
            {
                var inputNavigation = _playerNameInput.navigation;
                inputNavigation.mode = Navigation.Mode.Explicit;
                inputNavigation.selectOnUp = _newGameButton;
                _playerNameInput.navigation = inputNavigation;

                var buttonNavigation = _newGameButton.navigation;
                buttonNavigation.mode = Navigation.Mode.Explicit;
                buttonNavigation.selectOnDown = _playerNameInput;
                _newGameButton.navigation = buttonNavigation;
            }
        }

        private void HandleNameInputSelected(string _)
        {
            if (_nameInputBorder != null)
            {
                _nameInputBorder.color = new Color(0.96f, 0.42f, 0.10f, 1f);
            }
        }

        private void HandleNameInputDeselected(string _)
        {
            if (_nameInputBorder != null)
            {
                _nameInputBorder.color = Color.black;
            }
        }

        private string ChooseSuggestedPlayerName()
        {
            var names = _trainerNameCatalog?.Names
                .Where(name => name != null && !string.IsNullOrWhiteSpace(name.DisplayName))
                .Select(name => name.DisplayName.Trim())
                .Distinct()
                .ToArray();

            return names == null || names.Length == 0
                ? NewGameRequest.GuestPlayerName
                : names[Random.Range(0, names.Length)];
        }

        private void EnsureWhiteBackground()
        {
            if (_titleRoot == null)
            {
                return;
            }

            var background = _titleRoot.GetComponent<Image>();
            if (background == null)
            {
                background = _titleRoot.AddComponent<Image>();
            }

            background.color = Color.white;
            background.raycastTarget = false;
        }

        private void EnsureMenuDialog()
        {
            if (_titleRoot == null || _newGameButton == null)
            {
                return;
            }

            var border = CreateImageObject("MenuDialog", _titleRoot.transform, Color.black);
            var borderRect = border.rectTransform;
            _menuDialogRect = borderRect;
            borderRect.anchorMin = new Vector2(0f, 1f);
            borderRect.anchorMax = new Vector2(0f, 1f);
            borderRect.pivot = new Vector2(0f, 1f);
            borderRect.anchoredPosition = new Vector2(32f, -32f);
            borderRect.sizeDelta = new Vector2(300f, 190f);

            var panel = CreateImageObject("Inner", borderRect, Color.white);
            SetStretch(panel.rectTransform, 4f);

            ConfigureMenuButton(_newGameButton, panel.rectTransform, "はじめから", 0, true);
            CreateMenuButton(panel.rectTransform, "つづきから", 1);
            CreateMenuButton(panel.rectTransform, "設定", 2);
        }

        private void RefreshResponsiveLayout()
        {
            if (_titleRoot == null
                || _titleRoot.transform is not RectTransform rootRect
                || _menuDialogRect == null
                || _nameInputDialogRect == null
                || _nameLabelRect == null)
            {
                return;
            }

            var rootSize = rootRect.rect.size;
            if ((rootSize - _lastRootSize).sqrMagnitude < 0.01f)
            {
                return;
            }

            _lastRootSize = rootSize;
            if (rootSize.x < CompactLayoutWidth)
            {
                ApplyCompactLayout(rootSize.x);
                return;
            }

            ApplyExpandedLayout();
        }

        private void ApplyCompactLayout(float rootWidth)
        {
            _menuDialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            _menuDialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            _menuDialogRect.pivot = new Vector2(0.5f, 0.5f);
            _menuDialogRect.anchoredPosition = Vector2.zero;
            _menuDialogRect.sizeDelta = new Vector2(
                Mathf.Min(300f, Mathf.Max(0f, rootWidth - 32f)),
                190f);

            var groupWidth = Mathf.Max(0f, rootWidth - 24f);
            var labelWidth = Mathf.Clamp(groupWidth * 0.28f, 84f, 104f);
            const float gap = 8f;
            var inputWidth = Mathf.Max(0f, groupWidth - labelWidth - gap);
            const float inputHeight = 60f;
            const float bottomMargin = 24f;

            _nameInputDialogRect.anchorMin = new Vector2(0.5f, 0f);
            _nameInputDialogRect.anchorMax = new Vector2(0.5f, 0f);
            _nameInputDialogRect.pivot = new Vector2(0.5f, 0f);
            _nameInputDialogRect.anchoredPosition = new Vector2(
                (labelWidth + gap) * 0.5f,
                bottomMargin);
            _nameInputDialogRect.sizeDelta = new Vector2(inputWidth, inputHeight);

            _nameLabelRect.anchorMin = new Vector2(0.5f, 0f);
            _nameLabelRect.anchorMax = new Vector2(0.5f, 0f);
            _nameLabelRect.pivot = new Vector2(1f, 0.5f);
            _nameLabelRect.anchoredPosition = new Vector2(
                (labelWidth - gap - inputWidth) * 0.5f,
                bottomMargin + (inputHeight * 0.5f));
            _nameLabelRect.sizeDelta = new Vector2(labelWidth, inputHeight);
        }

        private void ApplyExpandedLayout()
        {
            _menuDialogRect.anchorMin = new Vector2(0f, 1f);
            _menuDialogRect.anchorMax = new Vector2(0f, 1f);
            _menuDialogRect.pivot = new Vector2(0f, 1f);
            _menuDialogRect.anchoredPosition = new Vector2(32f, -32f);
            _menuDialogRect.sizeDelta = new Vector2(300f, 190f);

            _nameInputDialogRect.anchorMin = new Vector2(0.5f, 0f);
            _nameInputDialogRect.anchorMax = new Vector2(0.5f, 0f);
            _nameInputDialogRect.pivot = new Vector2(0.5f, 0f);
            _nameInputDialogRect.anchoredPosition = new Vector2(0f, 52f);
            _nameInputDialogRect.sizeDelta = new Vector2(328f, 64f);

            _nameLabelRect.anchorMin = new Vector2(0.5f, 0f);
            _nameLabelRect.anchorMax = new Vector2(0.5f, 0f);
            _nameLabelRect.pivot = new Vector2(1f, 0.5f);
            _nameLabelRect.anchoredPosition = new Vector2(-176f, 84f);
            _nameLabelRect.sizeDelta = new Vector2(120f, 64f);
        }

        private static void CreateMenuButton(RectTransform parent, string label, int row)
        {
            var buttonObject = new GameObject(
                label,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.layer = parent.gameObject.layer;
            ConfigureMenuButton(
                buttonObject.GetComponent<Button>(),
                parent,
                label,
                row,
                false);
        }

        private static void ConfigureMenuButton(
            Button button,
            RectTransform parent,
            string label,
            int row,
            bool interactable)
        {
            var rect = (RectTransform)button.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -8f - (row * 58f));
            rect.sizeDelta = new Vector2(-16f, 54f);

            var image = button.GetComponent<Image>();
            if (image == null)
            {
                image = button.gameObject.AddComponent<Image>();
            }

            image.color = Color.white;
            button.targetGraphic = image;
            button.interactable = interactable;

            var buttonLabel = button.GetComponentInChildren<TMP_Text>(true);
            if (buttonLabel == null)
            {
                buttonLabel = CreateText("Label", rect, label, 24f);
            }

            buttonLabel.text = label;
            if (TMP_Settings.defaultFontAsset != null)
            {
                buttonLabel.font = TMP_Settings.defaultFontAsset;
            }

            buttonLabel.fontSize = 24f;
            buttonLabel.fontStyle = FontStyles.Bold;
            buttonLabel.fontWeight = FontWeight.Bold;
            buttonLabel.alignment = TextAlignmentOptions.MidlineLeft;
            ApplyTitleTextStyle(buttonLabel);
            var labelRect = (RectTransform)buttonLabel.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(18f, 0f);
            labelRect.offsetMax = Vector2.zero;
        }

        private static Image CreateImageObject(string objectName, Transform parent, Color color)
        {
            var gameObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            gameObject.layer = parent.gameObject.layer;
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void SetStretch(RectTransform rectTransform, float inset)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(inset, inset);
            rectTransform.offsetMax = new Vector2(-inset, -inset);
        }

        private static RectTransform CreateRectObject(string objectName, RectTransform parent)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.layer = parent.gameObject.layer;
            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private static TextMeshProUGUI CreateText(
            string objectName,
            RectTransform parent,
            string text,
            float fontSize)
        {
            var rectTransform = CreateRectObject(objectName, parent);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var textComponent = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                textComponent.font = TMP_Settings.defaultFontAsset;
            }

            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = FontStyles.Bold;
            textComponent.fontWeight = FontWeight.Bold;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            textComponent.raycastTarget = false;
            ApplyTitleTextStyle(textComponent);
            return textComponent;
        }

        private static void ApplyTitleTextStyle(TMP_Text text)
        {
            text.color = Color.black;
            text.alpha = 1f;
            text.overrideColorTags = true;
            text.faceColor = Color.black;
            text.outlineColor = Color.black;
            text.outlineWidth = 0.04f;
        }

        private void WireButtons()
        {
            if (_newGameButton == null)
            {
                return;
            }

            _newGameButton.onClick.RemoveListener(StartNewGame);
            _newGameButton.onClick.AddListener(StartNewGame);
        }

        private void OnDestroy()
        {
            _newGameButton?.onClick.RemoveListener(StartNewGame);
            _playerNameInput?.onSelect.RemoveListener(HandleNameInputSelected);
            _playerNameInput?.onDeselect.RemoveListener(HandleNameInputDeselected);
        }

        private void StartNewGame()
        {
            var enteredName = _playerNameInput != null ? _playerNameInput.text : null;
            NewGameRequest.Prepare(
                string.IsNullOrWhiteSpace(enteredName) ? _suggestedPlayerName : enteredName);
            SceneLoader.LoadGameScene();
        }

        private void ValidateReferences()
        {
            if (_titleRoot == null)
            {
                Debug.LogWarning($"{nameof(TitleSceneInstaller)} on '{name}' is missing TitleRoot.", this);
            }

            if (_newGameButton == null)
            {
                Debug.LogWarning($"{nameof(TitleSceneInstaller)} on '{name}' is missing NewGameButton.", this);
            }

            if (_trainerNameCatalog == null)
            {
                Debug.LogWarning($"{nameof(TitleSceneInstaller)} on '{name}' is missing TrainerNameCatalog.", this);
            }

        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateReferences();
        }
#endif
    }
}
