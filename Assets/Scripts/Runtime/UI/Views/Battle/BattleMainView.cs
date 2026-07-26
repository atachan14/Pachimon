using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Pachimon.Battle;
using Pachimon.Data;
using Pachimon.Items;

namespace Pachimon.UI
{
    public sealed class BattleMainView : MonoBehaviour
    {
        [field: SerializeField] public RectTransform GraphicWindow { get; private set; }
        [field: SerializeField] public BattleUnitAreaView EnemyArea { get; private set; }
        [field: SerializeField] public BattleUnitAreaView AllyArea { get; private set; }

        private const float SlideDuration = 0.38f;
        private RectTransform _trainerLayer;
        private Image _playerTrainerImage;
        private Image _enemyTrainerImage;
        private CanvasGroup _enemyAreaCanvasGroup;
        private CanvasGroup _allyAreaCanvasGroup;
        private Vector3 _enemyAreaHomePosition;
        private Vector3 _allyAreaHomePosition;

        private void Awake()
        {
            if (GraphicWindow == null)
            {
                GraphicWindow = transform as RectTransform;
            }

            EnsureGraphicWindowClip();
        }

        public void Initialize(
            RectTransform graphicWindow,
            BattleUnitAreaView enemyArea,
            BattleUnitAreaView allyArea)
        {
            GraphicWindow = graphicWindow;
            EnemyArea = enemyArea;
            AllyArea = allyArea;
            EnsureGraphicWindowClip();
        }

        public void Render(BattleState state, PachimonCatalog pachimonCatalog = null)
        {
            if (state == null)
            {
                return;
            }

            EnemyArea?.RenderUnits(state.Enemy.Units, "Enemy", pachimonCatalog, false);
            AllyArea?.RenderUnits(state.Player.Units, "Ally", pachimonCatalog, true);
        }

        public void ShowSkillPreview(BattleState state, SkillPreview preview)
        {
            if (state == null || preview == null)
            {
                ClearSkillPreview();
                return;
            }

            EnemyArea?.ShowSkillPreview(state.Enemy.Units, preview.Effects);
            AllyArea?.ShowSkillPreview(state.Player.Units, preview.Effects);
        }

        public void ClearSkillPreview()
        {
            EnemyArea?.ClearSkillPreview();
            AllyArea?.ClearSkillPreview();
        }

        public void ConfigureItemDrops(
            Func<ItemInstance, int, bool> tryUseOnAlly,
            Func<ItemInstance, int, bool> tryUseOnEnemy)
        {
            AllyArea?.ConfigureItemDrops(tryUseOnAlly);
            EnemyArea?.ConfigureItemDrops(tryUseOnEnemy);
        }

        public void ConfigureUnitClicks(
            Action<int> onAllyClicked,
            Action<int> onEnemyClicked)
        {
            AllyArea?.ConfigureUnitClicks(onAllyClicked);
            EnemyArea?.ConfigureUnitClicks(onEnemyClicked);
        }

        public void PlayTrainerEntrance(
            Sprite playerTrainer,
            Sprite enemyTrainer,
            Action onCompleted)
        {
            StopAllCoroutines();
            EnsureTrainerLayer();
            BindTrainer(_playerTrainerImage, playerTrainer);
            BindTrainer(_enemyTrainerImage, enemyTrainer);
            CaptureUnitAreaHomePositions();
            ResetUnitAreasToHiddenHomePositions();
            SetUnitAreasVisible(false);
            _trainerLayer.gameObject.SetActive(true);
            StartCoroutine(AnimateTrainerEntrance(onCompleted));
        }

        public void PlayTrainerExitAndUnitEntrance(Action onCompleted)
        {
            StopAllCoroutines();
            EnsureTrainerLayer();
            StartCoroutine(AnimateTrainerExitAndUnits(onCompleted));
        }

        public void SetUnitAreasVisible(bool isVisible)
        {
            _enemyAreaCanvasGroup ??= EnsureCanvasGroup(EnemyArea);
            _allyAreaCanvasGroup ??= EnsureCanvasGroup(AllyArea);
            if (_enemyAreaCanvasGroup != null) _enemyAreaCanvasGroup.alpha = isVisible ? 1f : 0f;
            if (_allyAreaCanvasGroup != null) _allyAreaCanvasGroup.alpha = isVisible ? 1f : 0f;
        }

        private IEnumerator AnimateTrainerEntrance(Action onCompleted)
        {
            Canvas.ForceUpdateCanvases();
            var width = Mathf.Max(GraphicWindow.rect.width, 600f);
            var playerRect = _playerTrainerImage.rectTransform;
            var enemyRect = _enemyTrainerImage.rectTransform;
            FitTrainerRectToLayer(playerRect);
            FitTrainerRectToLayer(enemyRect);
            var playerY = ClampTrainerY(
                playerRect,
                GetAreaCenterY(AllyArea, -GraphicWindow.rect.height * 0.2f));
            var enemyY = ClampTrainerY(
                enemyRect,
                GetAreaCenterY(EnemyArea, GraphicWindow.rect.height * 0.2f));
            var playerStart = new Vector2(-width * 0.72f, playerY);
            var enemyStart = new Vector2(width * 0.72f, enemyY);
            var playerEnd = new Vector2(-width * 0.22f, playerY);
            var enemyEnd = new Vector2(width * 0.22f, enemyY);
            playerRect.anchoredPosition = playerStart;
            enemyRect.anchoredPosition = enemyStart;

            yield return Animate(
                SlideDuration,
                progress =>
                {
                    var eased = EaseOutCubic(progress);
                    playerRect.anchoredPosition = Vector2.LerpUnclamped(playerStart, playerEnd, eased);
                    enemyRect.anchoredPosition = Vector2.LerpUnclamped(enemyStart, enemyEnd, eased);
                });
            onCompleted?.Invoke();
        }

        private IEnumerator AnimateTrainerExitAndUnits(Action onCompleted)
        {
            Canvas.ForceUpdateCanvases();
            var width = Mathf.Max(GraphicWindow.rect.width, 600f);
            var playerRect = _playerTrainerImage.rectTransform;
            var enemyRect = _enemyTrainerImage.rectTransform;
            var playerStart = playerRect.anchoredPosition;
            var enemyStart = enemyRect.anchoredPosition;
            var playerEnd = new Vector2(-width * 0.72f, playerStart.y);
            var enemyEnd = new Vector2(width * 0.72f, enemyStart.y);

            _enemyAreaCanvasGroup ??= EnsureCanvasGroup(EnemyArea);
            _allyAreaCanvasGroup ??= EnsureCanvasGroup(AllyArea);
            if (EnemyArea != null)
            {
                EnemyArea.transform.localPosition = _enemyAreaHomePosition + Vector3.right * width * 0.35f;
            }
            if (AllyArea != null)
            {
                AllyArea.transform.localPosition = _allyAreaHomePosition + Vector3.left * width * 0.35f;
            }

            yield return Animate(
                SlideDuration,
                progress =>
                {
                    var eased = EaseInCubic(progress);
                    playerRect.anchoredPosition = Vector2.LerpUnclamped(playerStart, playerEnd, eased);
                    enemyRect.anchoredPosition = Vector2.LerpUnclamped(enemyStart, enemyEnd, eased);
                });

            _trainerLayer.gameObject.SetActive(false);
            yield return Animate(
                SlideDuration,
                progress =>
                {
                    var eased = EaseOutCubic(progress);
                    if (_enemyAreaCanvasGroup != null) _enemyAreaCanvasGroup.alpha = eased;
                    if (_allyAreaCanvasGroup != null) _allyAreaCanvasGroup.alpha = eased;
                    if (EnemyArea != null)
                    {
                        EnemyArea.transform.localPosition = Vector3.LerpUnclamped(
                            _enemyAreaHomePosition + Vector3.right * width * 0.35f,
                            _enemyAreaHomePosition,
                            eased);
                    }
                    if (AllyArea != null)
                    {
                        AllyArea.transform.localPosition = Vector3.LerpUnclamped(
                            _allyAreaHomePosition + Vector3.left * width * 0.35f,
                            _allyAreaHomePosition,
                            eased);
                    }
                });
            onCompleted?.Invoke();
        }

        private void CaptureUnitAreaHomePositions()
        {
            Canvas.ForceUpdateCanvases();
            _enemyAreaHomePosition = EnemyArea != null
                ? EnemyArea.transform.localPosition
                : Vector3.zero;
            _allyAreaHomePosition = AllyArea != null
                ? AllyArea.transform.localPosition
                : Vector3.zero;
        }

        private void ResetUnitAreasToHiddenHomePositions()
        {
            if (EnemyArea != null)
            {
                EnemyArea.transform.localPosition = _enemyAreaHomePosition;
            }

            if (AllyArea != null)
            {
                AllyArea.transform.localPosition = _allyAreaHomePosition;
            }

            _enemyAreaCanvasGroup ??= EnsureCanvasGroup(EnemyArea);
            _allyAreaCanvasGroup ??= EnsureCanvasGroup(AllyArea);
            if (_enemyAreaCanvasGroup != null)
            {
                _enemyAreaCanvasGroup.alpha = 0f;
            }

            if (_allyAreaCanvasGroup != null)
            {
                _allyAreaCanvasGroup.alpha = 0f;
            }
        }

        private float GetAreaCenterY(Component area, float fallback)
        {
            if (_trainerLayer == null || area?.transform is not RectTransform areaRect)
            {
                return fallback;
            }

            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                _trainerLayer,
                areaRect);
            return bounds.center.y;
        }

        private void FitTrainerRectToLayer(RectTransform trainerRect)
        {
            if (_trainerLayer == null || trainerRect == null)
            {
                return;
            }

            const float edgePadding = 16f;
            var availableWidth = Mathf.Max(1f, _trainerLayer.rect.width * 0.42f);
            var availableHeight = Mathf.Max(1f, _trainerLayer.rect.height - edgePadding * 2f);
            trainerRect.sizeDelta = new Vector2(
                Mathf.Min(230f, availableWidth),
                Mathf.Min(330f, availableHeight));
        }

        private float ClampTrainerY(RectTransform trainerRect, float requestedY)
        {
            if (_trainerLayer == null || trainerRect == null)
            {
                return requestedY;
            }

            const float edgePadding = 16f;
            var halfHeight = trainerRect.rect.height * 0.5f;
            var minimumY = -_trainerLayer.rect.height * 0.5f + halfHeight + edgePadding;
            var maximumY = _trainerLayer.rect.height * 0.5f - halfHeight - edgePadding;
            return minimumY <= maximumY
                ? Mathf.Clamp(requestedY, minimumY, maximumY)
                : 0f;
        }

        private void EnsureTrainerLayer()
        {
            if (_trainerLayer != null)
            {
                return;
            }

            var layerObject = new GameObject(
                "RuntimeTrainerLayer",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(LayoutElement));
            layerObject.layer = gameObject.layer;
            _trainerLayer = layerObject.GetComponent<RectTransform>();
            _trainerLayer.SetParent(transform, false);
            _trainerLayer.anchorMin = Vector2.zero;
            _trainerLayer.anchorMax = Vector2.one;
            _trainerLayer.offsetMin = Vector2.zero;
            _trainerLayer.offsetMax = Vector2.zero;
            _trainerLayer.SetAsLastSibling();
            layerObject.GetComponent<LayoutElement>().ignoreLayout = true;
            var backdrop = layerObject.GetComponent<Image>();
            backdrop.color = Color.white;
            backdrop.raycastTarget = false;
            _playerTrainerImage = CreateTrainerImage(_trainerLayer, "PlayerTrainer");
            _enemyTrainerImage = CreateTrainerImage(_trainerLayer, "EnemyTrainer");
        }

        private void EnsureGraphicWindowClip()
        {
            if (GraphicWindow == null)
            {
                return;
            }

            if (GraphicWindow.GetComponent<RectMask2D>() == null)
            {
                GraphicWindow.gameObject.AddComponent<RectMask2D>();
            }
        }

        private static Image CreateTrainerImage(RectTransform parent, string objectName)
        {
            var imageObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.layer = parent.gameObject.layer;
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(230f, 330f);
            return image;
        }

        private static void BindTrainer(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.color = Color.white;
        }

        private static CanvasGroup EnsureCanvasGroup(Component component)
        {
            if (component == null)
            {
                return null;
            }

            return component.GetComponent<CanvasGroup>()
                ?? component.gameObject.AddComponent<CanvasGroup>();
        }

        private static IEnumerator Animate(float duration, Action<float> update)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                update?.Invoke(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            update?.Invoke(1f);
        }

        private static float EaseOutCubic(float value)
        {
            var inverse = 1f - value;
            return 1f - inverse * inverse * inverse;
        }

        private static float EaseInCubic(float value)
        {
            return value * value * value;
        }
    }
}
