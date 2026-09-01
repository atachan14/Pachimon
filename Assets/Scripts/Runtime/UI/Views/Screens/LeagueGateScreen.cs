using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class LeagueGateScreen : NodeScreen
    {
        private const string ProfessorResourcePath = "Professor/professor";

        [SerializeField] private Sprite _professorSprite;
        private RectTransform _runtimeRoot;

        public void ShowProfessor()
        {
            EnsureRuntimeRoot();
            _runtimeRoot.gameObject.SetActive(true);
        }

        public void HideProfessor()
        {
            if (_runtimeRoot != null)
                _runtimeRoot.gameObject.SetActive(false);
        }

        private void EnsureRuntimeRoot()
        {
            if (_runtimeRoot != null) return;

            var root = new GameObject("LeagueGateRuntimeRoot", typeof(RectTransform));
            root.layer = gameObject.layer;
            _runtimeRoot = root.GetComponent<RectTransform>();
            _runtimeRoot.SetParent(transform, false);
            _runtimeRoot.anchorMin = Vector2.zero;
            _runtimeRoot.anchorMax = Vector2.one;
            _runtimeRoot.offsetMin = Vector2.zero;
            _runtimeRoot.offsetMax = Vector2.zero;

            var graphicObject = new GameObject(
                "ProfessorGraphic",
                typeof(RectTransform),
                typeof(Image));
            graphicObject.layer = gameObject.layer;
            var graphicRect = graphicObject.GetComponent<RectTransform>();
            graphicRect.SetParent(_runtimeRoot, false);
            graphicRect.anchorMin = new Vector2(0.48f, 0.04f);
            graphicRect.anchorMax = new Vector2(0.94f, 0.96f);
            graphicRect.offsetMin = Vector2.zero;
            graphicRect.offsetMax = Vector2.zero;
            var graphic = graphicObject.GetComponent<Image>();
            graphic.sprite = ResolveProfessorSprite();
            graphic.preserveAspect = true;
            graphic.raycastTarget = false;
            graphic.color = Color.white;
        }

        private Sprite ResolveProfessorSprite()
        {
            if (_professorSprite == null)
                _professorSprite = Resources.Load<Sprite>(ProfessorResourcePath);
            return _professorSprite;
        }
    }
}
