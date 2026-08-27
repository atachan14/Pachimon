using System;
using System.Text;
using Pachimon.App;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public readonly struct RuntimeErrorDiagnosticContext
    {
        public RuntimeErrorDiagnosticContext(
            int? runSeed,
            string nodeId,
            long? battleTick)
        {
            RunSeed = runSeed;
            NodeId = nodeId ?? string.Empty;
            BattleTick = battleTick;
        }

        public int? RunSeed { get; }
        public string NodeId { get; }
        public long? BattleTick { get; }
    }

    public sealed class RuntimeErrorOverlayView : MonoBehaviour
    {
        private const int SortingOrder = short.MaxValue - 1;

        private CanvasGroup _canvasGroup;
        private TMP_Text _summary;
        private TMP_Text _details;
        private TMP_Text _detailsButtonLabel;
        private TMP_Text _copyButtonLabel;
        private GameObject _detailsViewport;
        private Func<RuntimeErrorDiagnosticContext> _contextProvider;
        private string _report = string.Empty;
        private bool _detailsShown;
        private bool _hasError;
        private bool _handlingLog;
        private bool _listening;

        public static RuntimeErrorOverlayView CreateRuntime(RectTransform parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));

            var root = new GameObject(
                "RuntimeErrorOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(Image),
                typeof(LayoutElement),
                typeof(RuntimeErrorOverlayView));
            root.layer = parent.gameObject.layer;
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect);

            var canvas = root.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;
            root.GetComponent<LayoutElement>().ignoreLayout = true;

            var view = root.GetComponent<RuntimeErrorOverlayView>();
            view.Build();
            view.StartListening();
            return view;
        }

        public void ConfigureDiagnostics(
            Func<RuntimeErrorDiagnosticContext> contextProvider)
        {
            _contextProvider = contextProvider;
        }

        private void OnDestroy()
        {
            if (_listening)
            {
                Application.logMessageReceived -= HandleLog;
                _listening = false;
            }
        }

        private void StartListening()
        {
            if (_listening)
            {
                return;
            }

            Application.logMessageReceived += HandleLog;
            _listening = true;
        }

        private void HandleLog(
            string condition,
            string stackTrace,
            LogType type)
        {
            if (_hasError
                || _handlingLog
                || (type != LogType.Exception && type != LogType.Assert))
            {
                return;
            }

            _handlingLog = true;
            try
            {
                var context = _contextProvider?.Invoke() ?? default;
                _report = BuildReport(condition, stackTrace, type, context);
                _summary.text = BuildSummary(condition, context);
                _details.text = _report;
                _copyButtonLabel.text = "内容をコピー";
                _hasError = true;
                SetDetailsShown(false);
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                transform.SetAsLastSibling();
            }
            catch
            {
                // Never emit another log while handling the original exception.
            }
            finally
            {
                _handlingLog = false;
            }
        }

        private void ToggleDetails()
        {
            SetDetailsShown(!_detailsShown);
        }

        private void SetDetailsShown(bool shown)
        {
            _detailsShown = shown;
            _summary.gameObject.SetActive(!shown);
            _detailsViewport.SetActive(shown);
            _detailsButtonLabel.text = shown ? "概要に戻る" : "詳細を表示";
        }

        private void CopyReport()
        {
            if (string.IsNullOrEmpty(_report))
            {
                return;
            }

            try
            {
                GUIUtility.systemCopyBuffer = _report;
                _copyButtonLabel.text = "コピーしました";
            }
            catch
            {
                _copyButtonLabel.text = "コピー失敗・F12で確認";
            }
        }

        private static void ReturnToTitle()
        {
            SceneLoader.LoadTitleScene();
        }

        private void Build()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.11f, 0.78f);

            var panel = CreateImage(
                "Panel",
                transform,
                new Color32(250, 247, 241, 255),
                raycastTarget: true);
            SetAnchors(
                panel.rectTransform,
                new Vector2(0.14f, 0.12f),
                new Vector2(0.86f, 0.88f));

            var accent = CreateImage(
                "Accent",
                panel.transform,
                new Color32(184, 57, 50, 255));
            SetAnchors(
                accent.rectTransform,
                new Vector2(0f, 0.93f),
                Vector2.one);

            var title = CreateText(
                "Title",
                panel.transform,
                34f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            title.text = "エラーが発生しました";
            title.color = new Color32(132, 35, 31, 255);
            SetAnchors(
                title.rectTransform,
                new Vector2(0.06f, 0.79f),
                new Vector2(0.94f, 0.91f));

            _summary = CreateText(
                "Summary",
                panel.transform,
                22f,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft);
            _summary.textWrappingMode = TextWrappingModes.Normal;
            SetAnchors(
                _summary.rectTransform,
                new Vector2(0.08f, 0.22f),
                new Vector2(0.92f, 0.76f));

            _detailsViewport = CreateDetailsViewport(panel.transform);
            _detailsViewport.SetActive(false);

            var detailsButton = CreateButton(
                "DetailsButton",
                panel.transform,
                ToggleDetails,
                out _detailsButtonLabel);
            SetAnchors(
                detailsButton.GetComponent<RectTransform>(),
                new Vector2(0.06f, 0.06f),
                new Vector2(0.34f, 0.16f));

            var copyButton = CreateButton(
                "CopyButton",
                panel.transform,
                CopyReport,
                out _copyButtonLabel);
            SetAnchors(
                copyButton.GetComponent<RectTransform>(),
                new Vector2(0.36f, 0.06f),
                new Vector2(0.64f, 0.16f));

            var titleButton = CreateButton(
                "TitleButton",
                panel.transform,
                ReturnToTitle,
                out var titleButtonLabel);
            titleButtonLabel.text = "タイトルへ戻る";
            SetAnchors(
                titleButton.GetComponent<RectTransform>(),
                new Vector2(0.66f, 0.06f),
                new Vector2(0.94f, 0.16f));
        }

        private GameObject CreateDetailsViewport(Transform parent)
        {
            var viewport = new GameObject(
                "DetailsViewport",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(RectMask2D),
                typeof(ScrollRect));
            viewport.layer = parent.gameObject.layer;
            viewport.transform.SetParent(parent, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            SetAnchors(
                viewportRect,
                new Vector2(0.08f, 0.22f),
                new Vector2(0.92f, 0.76f));
            viewport.GetComponent<Image>().color = new Color32(235, 232, 226, 255);

            _details = CreateText(
                "Details",
                viewport.transform,
                16f,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft);
            _details.textWrappingMode = TextWrappingModes.Normal;
            _details.rectTransform.anchorMin = new Vector2(0f, 1f);
            _details.rectTransform.anchorMax = Vector2.one;
            _details.rectTransform.pivot = new Vector2(0.5f, 1f);
            _details.rectTransform.offsetMin = new Vector2(18f, 0f);
            _details.rectTransform.offsetMax = new Vector2(-18f, 0f);
            var fitter = _details.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = viewport.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = _details.rectTransform;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;
            return viewport;
        }

        private static string BuildSummary(
            string condition,
            RuntimeErrorDiagnosticContext context)
        {
            var builder = new StringBuilder();
            builder.AppendLine("ゲームの実行中にエラーが発生しました。");
            builder.AppendLine("処理を続けず、詳細をコピーして報告してください。");
            builder.AppendLine();
            AppendContext(builder, context);
            builder.AppendLine();
            builder.AppendLine(condition ?? string.Empty);
            return builder.ToString();
        }

        private static string BuildReport(
            string condition,
            string stackTrace,
            LogType type,
            RuntimeErrorDiagnosticContext context)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Pachimon Runtime Error");
            builder.Append("Occurred: ")
                .AppendLine(DateTimeOffset.Now.ToString("O"));
            builder.Append("Unity: ").AppendLine(Application.unityVersion);
            builder.Append("Platform: ").AppendLine(Application.platform.ToString());
            builder.Append("Type: ").AppendLine(type.ToString());
            AppendContext(builder, context);
            builder.AppendLine();
            builder.AppendLine(condition ?? string.Empty);
            builder.AppendLine();
            builder.AppendLine(stackTrace ?? string.Empty);
            return builder.ToString();
        }

        private static void AppendContext(
            StringBuilder builder,
            RuntimeErrorDiagnosticContext context)
        {
            builder.Append("Run Seed: ")
                .AppendLine(context.RunSeed?.ToString() ?? "Unavailable");
            builder.Append("Node: ")
                .AppendLine(string.IsNullOrEmpty(context.NodeId)
                    ? "Unavailable"
                    : context.NodeId);
            builder.Append("Battle Tick: ")
                .AppendLine(context.BattleTick?.ToString() ?? "Unavailable");
        }

        private static Image CreateImage(
            string objectName,
            Transform parent,
            Color color,
            bool raycastTarget = false)
        {
            var target = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            target.layer = parent.gameObject.layer;
            target.transform.SetParent(parent, false);
            var image = target.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static TMP_Text CreateText(
            string objectName,
            Transform parent,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            var target = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            target.layer = parent.gameObject.layer;
            target.transform.SetParent(parent, false);
            var text = target.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = GameUiPalette.PrimaryText;
            text.richText = false;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            string objectName,
            Transform parent,
            Action onClicked,
            out TMP_Text label)
        {
            var target = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            target.layer = parent.gameObject.layer;
            target.transform.SetParent(parent, false);
            var image = target.GetComponent<Image>();
            image.color = GameUiPalette.ButtonNeutral;
            var button = target.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClicked?.Invoke());

            label = CreateText(
                "Label",
                target.transform,
                18f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            label.color = GameUiPalette.OnAccentText;
            Stretch(label.rectTransform);
            return button;
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

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
