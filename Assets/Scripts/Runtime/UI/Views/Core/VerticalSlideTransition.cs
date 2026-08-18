using System;
using System.Collections;
using UnityEngine;

namespace Pachimon.UI
{
    public sealed class VerticalSlideTransition
    {
        private readonly MonoBehaviour _owner;
        private readonly RectTransform _rect;
        private readonly CanvasGroup _canvasGroup;
        private readonly Func<bool> _isOpen;
        private readonly bool _blockRaycastsWhileMoving;
        private readonly bool _applyAlpha;
        private Coroutine _routine;
        private float _slideDistance;

        public VerticalSlideTransition(
            MonoBehaviour owner,
            RectTransform rect,
            CanvasGroup canvasGroup,
            Func<bool> isOpen,
            bool blockRaycastsWhileMoving = false,
            bool applyAlpha = true)
        {
            _owner = owner;
            _rect = rect;
            _canvasGroup = canvasGroup;
            _isOpen = isOpen;
            _blockRaycastsWhileMoving = blockRaycastsWhileMoving;
            _applyAlpha = applyAlpha;
        }

        public bool IsRunning => _routine != null;

        public void SetSlideDistance(float distance)
        {
            _slideDistance = Mathf.Max(0f, distance);
        }

        public void Play(
            float targetProgress,
            float duration,
            bool deactivateWhenClosed = false)
        {
            Stop();
            targetProgress = Mathf.Clamp01(targetProgress);
            if (_owner == null || !_owner.isActiveAndEnabled || duration <= 0f)
            {
                Apply(targetProgress);
                DeactivateIfClosed(targetProgress, deactivateWhenClosed);
                return;
            }

            _routine = _owner.StartCoroutine(
                Animate(targetProgress, duration, deactivateWhenClosed));
        }

        public void Snap(float progress)
        {
            Stop();
            Apply(progress);
        }

        public void Cancel()
        {
            Stop();
        }

        private void Stop()
        {
            if (_routine == null || _owner == null)
            {
                return;
            }

            _owner.StopCoroutine(_routine);
            _routine = null;
        }

        private IEnumerator Animate(
            float targetProgress,
            float duration,
            bool deactivateWhenClosed)
        {
            var startProgress = GetProgress();
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var eased = progress * progress * (3f - 2f * progress);
                Apply(Mathf.Lerp(startProgress, targetProgress, eased));
                yield return null;
            }

            Apply(targetProgress);
            _routine = null;
            DeactivateIfClosed(targetProgress, deactivateWhenClosed);
        }

        private float GetProgress()
        {
            var distance = GetSlideDistance();
            return distance <= 0f || _rect == null
                ? 1f
                : 1f - Mathf.Clamp01(_rect.anchoredPosition.y / distance);
        }

        private void Apply(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (_rect != null)
            {
                _rect.anchoredPosition = new Vector2(
                    0f,
                    Mathf.Lerp(GetSlideDistance(), 0f, progress));
            }

            if (_canvasGroup == null)
            {
                return;
            }

            if (_applyAlpha)
            {
                _canvasGroup.alpha = progress;
            }
            var isOpen = _isOpen?.Invoke() == true;
            _canvasGroup.interactable = isOpen && progress >= 0.999f;
            _canvasGroup.blocksRaycasts = _blockRaycastsWhileMoving
                ? progress > 0.01f
                : isOpen && progress >= 0.999f;
        }

        private float GetSlideDistance()
        {
            return _slideDistance > 0f
                ? _slideDistance
                : Mathf.Max(1f, _rect?.rect.height ?? 1f);
        }

        private void DeactivateIfClosed(
            float progress,
            bool deactivateWhenClosed)
        {
            if (deactivateWhenClosed
                && progress <= 0f
                && _isOpen?.Invoke() != true
                && _owner != null)
            {
                _owner.gameObject.SetActive(false);
            }
        }
    }
}
