using System;
using System.Collections.Generic;
using Pachimon.Items;
using Pachimon.Map;
using Pachimon.Run;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class NodeSelectionWindowView : MonoBehaviour
    {
        [SerializeField] private BattleNodeWindowView _battleWindow;
        [SerializeField] private SimpleNodeWindowView _simpleWindow;
        [SerializeField] private GameObject _footer;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        private StartCandidateWindowView _startCandidateWindow;
        private CityShopWindowView _cityWindow;
        private LayoutMode _layoutMode = LayoutMode.Expanded;

        private Action _onConfirm;
        private Action _onCancel;

        public BattleNodeWindowView BattleWindow => _battleWindow;

        private void OnDestroy() => RemoveButtonListeners();

        public void Configure(
            BattleNodeWindowView battleWindow,
            SimpleNodeWindowView simpleWindow,
            GameObject footer,
            Button confirmButton,
            Button cancelButton)
        {
            _battleWindow = battleWindow;
            _simpleWindow = simpleWindow;
            _footer = footer;
            _confirmButton = confirmButton;
            _cancelButton = cancelButton;
            Hide();
        }

        public void ShowBattle(
            TrainerPreviewContent trainerPreview,
            IReadOnlyList<PachimonPreviewContent> pachimonPreviews,
            bool showFooter,
            Action onConfirm,
            Action onCancel)
        {
            gameObject.SetActive(true);
            SetWindow(_battleWindow);
            _battleWindow?.Bind(trainerPreview, pachimonPreviews);
            ConfigureFooter(showFooter, onConfirm, onCancel, "決定", "キャンセル");
        }

        public void ShowStartCandidates(
            IReadOnlyList<PachimonPreviewContent> previews,
            IReadOnlyList<bool> candidateSelections,
            int selectedIndex,
            Action<int> onTabSelected,
            string confirmLabel,
            Action onConfirm,
            Action onCancel)
        {
            EnsureStartCandidateWindow();
            gameObject.SetActive(true);
            SetWindow(_startCandidateWindow);
            _startCandidateWindow?.Bind(
                previews,
                candidateSelections,
                selectedIndex,
                onTabSelected);
            ConfigureFooter(true, onConfirm, onCancel, confirmLabel, "キャンセル");
        }

        public void ShowSimple(
            string title,
            string details,
            bool showFooter,
            Action onConfirm,
            Action onCancel)
        {
            gameObject.SetActive(true);
            SetWindow(_simpleWindow);
            _simpleWindow?.Bind(title, details);
            ConfigureFooter(showFooter, onConfirm, onCancel, "決定", "キャンセル");
        }

        public void ShowCity(
            CityNodeContent city,
            ItemCatalog itemCatalog,
            RunState runState,
            bool purchaseEnabled,
            bool showFooter,
            Action<int> onDetails,
            Action<string> onPurchase,
            Action onConfirm,
            Action onCancel)
        {
            EnsureCityWindow();
            gameObject.SetActive(true);
            SetWindow(_cityWindow);
            _cityWindow?.Bind(
                city,
                itemCatalog,
                runState,
                purchaseEnabled,
                onDetails,
                onPurchase);
            ConfigureFooter(showFooter, onConfirm, onCancel, "決定", "キャンセル");
        }

        public void Hide()
        {
            RemoveButtonListeners();
            _onConfirm = null;
            _onCancel = null;
            gameObject.SetActive(false);
        }

        public void ApplyLayoutMode(LayoutMode layoutMode)
        {
            _layoutMode = layoutMode;
            _startCandidateWindow?.ApplyLayoutMode(layoutMode);
        }

        private void SetWindow(MonoBehaviour activeWindow)
        {
            if (_battleWindow != null) _battleWindow.gameObject.SetActive(activeWindow == _battleWindow);
            if (_simpleWindow != null) _simpleWindow.gameObject.SetActive(activeWindow == _simpleWindow);
            if (_startCandidateWindow != null)
            {
                _startCandidateWindow.gameObject.SetActive(activeWindow == _startCandidateWindow);
            }
            if (_cityWindow != null)
            {
                _cityWindow.gameObject.SetActive(activeWindow == _cityWindow);
            }
        }

        private void ConfigureFooter(
            bool isVisible,
            Action onConfirm,
            Action onCancel,
            string confirmLabel,
            string cancelLabel)
        {
            RemoveButtonListeners();
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            _footer?.SetActive(isVisible);
            if (!isVisible) return;
            SetButtonLabel(_confirmButton, confirmLabel);
            SetButtonLabel(_cancelButton, cancelLabel);
            _confirmButton?.onClick.AddListener(Confirm);
            _cancelButton?.onClick.AddListener(Cancel);
        }

        private void EnsureStartCandidateWindow()
        {
            if (_startCandidateWindow != null)
            {
                return;
            }

            var windowObject = new GameObject(
                "StartCandidateWindow",
                typeof(RectTransform),
                typeof(StartCandidateWindowView));
            windowObject.layer = gameObject.layer;
            var rect = windowObject.GetComponent<RectTransform>();
            var windowParent = _battleWindow != null
                ? _battleWindow.transform.parent
                : transform;
            rect.SetParent(windowParent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _startCandidateWindow = windowObject.GetComponent<StartCandidateWindowView>();
            _startCandidateWindow.Initialize(_battleWindow?.PachimonTabTemplate);
            _startCandidateWindow.ApplyLayoutMode(_layoutMode);
        }

        private void EnsureCityWindow()
        {
            if (_cityWindow != null)
            {
                return;
            }

            var windowParent = _battleWindow != null
                ? _battleWindow.transform.parent
                : transform;
            _cityWindow = CityShopWindowView.CreateRuntime(windowParent);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            var text = button?.GetComponentInChildren<TMP_Text>(true);
            if (text != null) text.text = label;
        }

        private void RemoveButtonListeners()
        {
            _confirmButton?.onClick.RemoveListener(Confirm);
            _cancelButton?.onClick.RemoveListener(Cancel);
        }

        private void Confirm() => _onConfirm?.Invoke();
        private void Cancel() => _onCancel?.Invoke();
    }
}
