using Pachimon.App;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class TitleSceneInstaller : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private GameObject _titleRoot;
        [SerializeField] private TMP_InputField _playerNameInput;
        [SerializeField] private Button _newGameButton;

        private void Awake()
        {
            EnsurePlayerNameInput();
            ValidateReferences();
            WireButtons();
        }

        private void EnsurePlayerNameInput()
        {
            if (_playerNameInput != null || _titleRoot == null)
            {
                return;
            }

            var inputObject = new GameObject(
                "PlayerNameInput",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(TMP_InputField));
            inputObject.layer = _titleRoot.layer;
            var inputRect = inputObject.GetComponent<RectTransform>();
            inputRect.SetParent(_titleRoot.transform, false);
            inputRect.anchorMin = new Vector2(0.5f, 0.5f);
            inputRect.anchorMax = new Vector2(0.5f, 0.5f);
            inputRect.pivot = new Vector2(0.5f, 0.5f);
            inputRect.sizeDelta = new Vector2(300f, 64f);

            var buttonRect = _newGameButton != null ? _newGameButton.transform as RectTransform : null;
            inputRect.anchoredPosition = buttonRect != null
                ? buttonRect.anchoredPosition + new Vector2(0f, 105f)
                : new Vector2(0f, 105f);

            var background = inputObject.GetComponent<Image>();
            background.color = new Color(0.96f, 0.97f, 0.94f, 1f);

            var textArea = CreateRectObject("TextArea", inputRect);
            textArea.anchorMin = Vector2.zero;
            textArea.anchorMax = Vector2.one;
            textArea.offsetMin = new Vector2(16f, 8f);
            textArea.offsetMax = new Vector2(-16f, -8f);
            textArea.gameObject.AddComponent<RectMask2D>();

            var inputText = CreateText("Text", textArea, string.Empty, 24f);
            inputText.color = new Color(0.08f, 0.11f, 0.12f, 1f);

            var placeholder = CreateText("Placeholder", textArea, "名前を入力してください", 22f);
            placeholder.color = new Color(0.25f, 0.30f, 0.30f, 0.55f);

            _playerNameInput = inputObject.GetComponent<TMP_InputField>();
            _playerNameInput.targetGraphic = background;
            _playerNameInput.textViewport = textArea;
            _playerNameInput.textComponent = inputText;
            _playerNameInput.placeholder = placeholder;
            _playerNameInput.lineType = TMP_InputField.LineType.SingleLine;
            _playerNameInput.contentType = TMP_InputField.ContentType.Standard;

            if (_newGameButton != null)
            {
                var inputNavigation = _playerNameInput.navigation;
                inputNavigation.mode = Navigation.Mode.Explicit;
                inputNavigation.selectOnDown = _newGameButton;
                _playerNameInput.navigation = inputNavigation;

                var buttonNavigation = _newGameButton.navigation;
                buttonNavigation.mode = Navigation.Mode.Explicit;
                buttonNavigation.selectOnUp = _playerNameInput;
                _newGameButton.navigation = buttonNavigation;

                var buttonLabel = _newGameButton.GetComponentInChildren<TMP_Text>(true);
                if (buttonLabel != null)
                {
                    buttonLabel.text = "はじめから";
                }
            }
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
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            textComponent.raycastTarget = false;
            return textComponent;
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
        }

        private void StartNewGame()
        {
            NewGameRequest.Prepare(_playerNameInput != null ? _playerNameInput.text : null);
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

        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateReferences();
        }
#endif
    }
}
