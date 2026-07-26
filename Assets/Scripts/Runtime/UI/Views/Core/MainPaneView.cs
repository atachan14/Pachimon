using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class MainPaneView : MonoBehaviour
    {
        [field: SerializeField] public RectTransform GraphicWindow { get; private set; }
        [field: SerializeField] public LogWindowView LogWindowView { get; private set; }

        private readonly List<NodeScreen> _screens = new();

        public NodeScreen CurrentScreen { get; private set; }

        private void Awake()
        {
            EnsureBackground();
        }

        public void Initialize(RectTransform graphicWindow, LogWindowView logWindowView)
        {
            GraphicWindow = graphicWindow;
            LogWindowView = logWindowView;
        }

        public void RegisterScreen(NodeScreen screen)
        {
            if (screen == null || _screens.Contains(screen))
            {
                return;
            }

            _screens.Add(screen);
            screen.gameObject.SetActive(false);
        }

        public void Show(NodeScreen screen)
        {
            foreach (var registered in _screens)
            {
                registered.gameObject.SetActive(registered == screen);
            }

            CurrentScreen = screen;
        }

        private void EnsureBackground()
        {
            var background = GetComponent<Image>();
            if (background == null)
            {
                background = gameObject.AddComponent<Image>();
            }

            background.color = GameUiPalette.MainPaneBackground;
            background.raycastTarget = false;
        }
    }
}
