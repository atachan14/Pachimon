using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Pachimon.Battle;

namespace Pachimon.UI
{
    public sealed class ActionGaugeView : MonoBehaviour
    {
        private const float SecondsPerTick = 0.004f;
        private const float MinimumDuration = 0.12f;
        private const float MaximumDuration = 0.8f;

        private Image _elapsed;
        private Image _remaining;
        private TMP_Text _value;
        private Presentation _current;
        private Presentation _target;
        private Presentation _final;
        private bool _isInitialized;
        private bool _hasFinal;
        private float _animationDuration;
        private float _animationElapsed;

        public void Configure(
            Image elapsed,
            Image remaining,
            TMP_Text value)
        {
            _elapsed = elapsed;
            _remaining = remaining;
            _value = value;
        }

        public void Present(
            BattleActionPhase phase,
            float ratio,
            int totalTicks,
            int remainingTicks,
            Color elapsedColor,
            Color remainingColor,
            Color valueColor,
            string valueText,
            bool showRemaining,
            bool useValueText = false)
        {
            var next = new Presentation(
                phase,
                Mathf.Clamp01(ratio),
                Mathf.Max(0, totalTicks),
                Mathf.Max(0, remainingTicks),
                elapsedColor,
                remainingColor,
                valueColor,
                valueText,
                showRemaining,
                useValueText);
            if (!_isInitialized)
            {
                _isInitialized = true;
                Apply(next);
                return;
            }

            if ((_hasFinal && _final.HasSameState(next))
                || (_animationDuration > 0f
                    && !_hasFinal
                    && _target.HasSameState(next)))
            {
                return;
            }

            if (ShouldFinishCurrentPhase(_current, next))
            {
                var completed = _current.WithProgress(1f, 0);
                BeginAnimation(completed, next);
                return;
            }

            if (_current.Phase == next.Phase
                && !Mathf.Approximately(_current.Ratio, next.Ratio))
            {
                BeginAnimation(next);
                return;
            }

            Apply(next);
        }

        private void Update()
        {
            if (_animationDuration <= 0f)
            {
                return;
            }

            _animationElapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(
                _animationElapsed / _animationDuration);
            var eased = 1f - Mathf.Pow(1f - progress, 3f);
            var interpolated = Presentation.Lerp(
                _current,
                _target,
                eased);
            Render(interpolated);

            if (progress < 1f)
            {
                return;
            }

            _animationDuration = 0f;
            _current = _target;
            if (_hasFinal)
            {
                var final = _final;
                _hasFinal = false;
                Apply(final);
            }
        }

        private void BeginAnimation(Presentation target)
        {
            _target = target;
            _hasFinal = false;
            StartAnimation();
        }

        private void BeginAnimation(
            Presentation phaseEnd,
            Presentation final)
        {
            _target = phaseEnd;
            _final = final;
            _hasFinal = true;
            StartAnimation();
        }

        private void StartAnimation()
        {
            var changedTicks = Mathf.Abs(
                _current.RemainingTicks - _target.RemainingTicks);
            _animationDuration = Mathf.Clamp(
                changedTicks * SecondsPerTick,
                MinimumDuration,
                MaximumDuration);
            _animationElapsed = 0f;
        }

        private void Apply(Presentation presentation)
        {
            _animationDuration = 0f;
            _hasFinal = false;
            _current = presentation;
            _target = presentation;
            Render(presentation);
        }

        private void Render(Presentation presentation)
        {
            if (_elapsed == null || _remaining == null || _value == null)
            {
                return;
            }

            _elapsed.enabled = presentation.Ratio > 0f;
            _elapsed.color = presentation.ElapsedColor;
            SetHorizontalRange(
                _elapsed.rectTransform,
                0f,
                presentation.Ratio);

            _remaining.enabled =
                presentation.ShowRemaining
                && presentation.Ratio < 1f;
            _remaining.color = presentation.RemainingColor;
            SetHorizontalRange(
                _remaining.rectTransform,
                presentation.Ratio,
                1f);

            _value.color = presentation.ValueColor;
            _value.text = presentation.IsTimed && !presentation.UseValueText
                ? presentation.RemainingTicks.ToString()
                : presentation.ValueText;
        }

        private static bool ShouldFinishCurrentPhase(
            Presentation current,
            Presentation next)
        {
            return current.IsTimed
                && current.Ratio < 1f
                && current.Phase != next.Phase;
        }

        private static void SetHorizontalRange(
            RectTransform rect,
            float minimum,
            float maximum)
        {
            rect.anchorMin = new Vector2(Mathf.Clamp01(minimum), 0f);
            rect.anchorMax = new Vector2(Mathf.Clamp01(maximum), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private readonly struct Presentation
        {
            public Presentation(
                BattleActionPhase phase,
                float ratio,
                int totalTicks,
                int remainingTicks,
                Color elapsedColor,
                Color remainingColor,
                Color valueColor,
                string valueText,
                bool showRemaining,
                bool useValueText)
            {
                Phase = phase;
                Ratio = ratio;
                TotalTicks = totalTicks;
                RemainingTicks = remainingTicks;
                ElapsedColor = elapsedColor;
                RemainingColor = remainingColor;
                ValueColor = valueColor;
                ValueText = valueText;
                ShowRemaining = showRemaining;
                UseValueText = useValueText;
            }

            public BattleActionPhase Phase { get; }
            public float Ratio { get; }
            public int TotalTicks { get; }
            public int RemainingTicks { get; }
            public Color ElapsedColor { get; }
            public Color RemainingColor { get; }
            public Color ValueColor { get; }
            public string ValueText { get; }
            public bool ShowRemaining { get; }
            public bool UseValueText { get; }
            public bool IsTimed =>
                Phase == BattleActionPhase.InitialDelay
                || Phase == BattleActionPhase.Startup
                || Phase == BattleActionPhase.Recovery;

            public Presentation WithProgress(
                float ratio,
                int remainingTicks)
            {
                return new Presentation(
                    Phase,
                    ratio,
                    TotalTicks,
                    remainingTicks,
                    ElapsedColor,
                    RemainingColor,
                    ValueColor,
                    ValueText,
                    ShowRemaining,
                    UseValueText);
            }

            public bool HasSameState(Presentation other)
            {
                return Phase == other.Phase
                    && RemainingTicks == other.RemainingTicks
                    && Mathf.Approximately(Ratio, other.Ratio)
                    && UseValueText == other.UseValueText
                    && ValueText == other.ValueText;
            }

            public static Presentation Lerp(
                Presentation from,
                Presentation to,
                float progress)
            {
                return new Presentation(
                    from.Phase,
                    Mathf.Lerp(from.Ratio, to.Ratio, progress),
                    from.TotalTicks,
                    Mathf.RoundToInt(Mathf.Lerp(
                        from.RemainingTicks,
                        to.RemainingTicks,
                        progress)),
                    Color.Lerp(
                        from.ElapsedColor,
                        to.ElapsedColor,
                        progress),
                    Color.Lerp(
                        from.RemainingColor,
                        to.RemainingColor,
                        progress),
                    Color.Lerp(
                        from.ValueColor,
                        to.ValueColor,
                        progress),
                    progress < 0.5f ? from.ValueText : to.ValueText,
                    progress < 0.5f
                        ? from.ShowRemaining
                        : to.ShowRemaining,
                    progress < 0.5f
                        ? from.UseValueText
                        : to.UseValueText);
            }
        }
    }
}
