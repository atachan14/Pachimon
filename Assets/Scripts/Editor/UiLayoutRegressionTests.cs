using NUnit.Framework;
using Pachimon.Reward;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI.Editor.Tests
{
    public sealed class UiLayoutRegressionTests
    {
        [TestCase(LayoutMode.Compact, true, LayoutMode.Compact)]
        [TestCase(LayoutMode.Compact, false, LayoutMode.Compact)]
        [TestCase(LayoutMode.Expanded, true, LayoutMode.Expanded)]
        [TestCase(LayoutMode.Expanded, false, LayoutMode.Compact)]
        public void LayoutModePolicy_ResolvesPreferenceAndScreenSupport(
            LayoutMode preferredMode,
            bool expandedLayoutSupported,
            LayoutMode expectedMode)
        {
            Assert.That(
                LayoutModePolicy.Resolve(
                    preferredMode,
                    expandedLayoutSupported),
                Is.EqualTo(expectedMode));
        }

        [Test]
        public void GameRoot_LayoutModeRoundTripPreservesPaneNavigation()
        {
            var viewport = CreateRect(
                "Viewport",
                null,
                new Vector2(1200f, 800f));
            var root = CreateRect(
                "GameRoot",
                viewport,
                new Vector2(1200f, 800f));
            root.gameObject.SetActive(false);
            try
            {
                var gameRoot = root.gameObject.AddComponent<GameRootView>();
                var headerRect = CreateRect("Header", root, new Vector2(1200f, 96f));
                var headerLayout = headerRect.gameObject.AddComponent<LayoutElement>();
                var header = headerRect.gameObject.AddComponent<HeaderView>();
                header.Initialize(
                    CreateText("GoldText", headerRect),
                    CreateButton("MapButton", headerRect),
                    CreateButton("ItemButton", headerRect),
                    CreateButton("SettingsButton", headerRect));

                var body = CreateRect("Body", root, new Vector2(1200f, 704f));
                var content = CreateRect("Content", body, body.sizeDelta);
                content.gameObject.AddComponent<HorizontalLayoutGroup>();
                var leftRect = CreateRect("LeftPane", content, new Vector2(240f, 704f));
                var mainRect = CreateRect("MainPane", content, new Vector2(720f, 704f));
                var rightRect = CreateRect("RightPane", content, new Vector2(240f, 704f));
                var left = leftRect.gameObject.AddComponent<LeftPaneView>();
                var main = mainRect.gameObject.AddComponent<MainPaneView>();
                var right = rightRect.gameObject.AddComponent<RightPaneView>();
                right.Initialize(null);

                var mapViewport = CreateRect("MapViewport", body, body.sizeDelta);
                var mapRect = CreateRect("MapOverlay", mapViewport, body.sizeDelta);
                var map = mapRect.gameObject.AddComponent<MapOverlayView>();

                gameRoot.Initialize(
                    header,
                    left,
                    main,
                    right,
                    map,
                    headerRect,
                    content,
                    leftRect,
                    mainRect,
                    rightRect,
                    1100f);

                gameRoot.ApplyLayoutMode(LayoutMode.Compact);
                Assert.That(
                    root.rect.width,
                    Is.EqualTo(800f * 2f / 3f).Within(0.01f));
                Assert.That(headerLayout.minHeight, Is.EqualTo(100f));
                Assert.That(headerLayout.preferredHeight, Is.EqualTo(100f));

                right.ShowBattleStatus(
                    default,
                    System.Array.Empty<PachimonPreviewContent>());
                Assert.That(
                    gameRoot.CurrentCompactPane,
                    Is.EqualTo(CompactPane.Main),
                    "Battle status refreshes must not open the RightPane.");

                right.ShowBattleNodePreview(
                    default,
                    System.Array.Empty<PachimonPreviewContent>());
                Assert.That(
                    gameRoot.CurrentCompactPane,
                    Is.EqualTo(CompactPane.Right),
                    "Explicit node previews should open the RightPane.");

                gameRoot.ShowCompactPane(CompactPane.Main, false);
                gameRoot.ShowCompactPane(CompactPane.Left, false);
                Assert.That(gameRoot.CurrentCompactPane, Is.EqualTo(CompactPane.Left));
                Assert.That(leftRect.parent.name, Is.EqualTo("LeftDrawerViewport"));

                gameRoot.ShowCompactPane(CompactPane.Right, false);
                Assert.That(gameRoot.CurrentCompactPane, Is.EqualTo(CompactPane.Right));
                Assert.That(rightRect.parent.name, Is.EqualTo("RightDrawerViewport"));

                gameRoot.ApplyLayoutMode(LayoutMode.Expanded);
                Assert.That(root.rect.width, Is.EqualTo(1200f).Within(0.01f));
                Assert.That(headerLayout.minHeight, Is.EqualTo(100f));
                Assert.That(headerLayout.preferredHeight, Is.EqualTo(100f));
                Assert.That(leftRect.parent, Is.SameAs(content));
                Assert.That(mainRect.parent, Is.SameAs(content));
                Assert.That(rightRect.parent, Is.SameAs(content));
                Assert.That(leftRect.GetSiblingIndex(), Is.EqualTo(0));
                Assert.That(mainRect.GetSiblingIndex(), Is.EqualTo(1));
                Assert.That(rightRect.GetSiblingIndex(), Is.EqualTo(2));

                gameRoot.ApplyLayoutMode(LayoutMode.Compact);
                Assert.That(gameRoot.CurrentCompactPane, Is.EqualTo(CompactPane.Right));
                Assert.That(rightRect.parent.name, Is.EqualTo("RightDrawerViewport"));
            }
            finally
            {
                Object.DestroyImmediate(viewport.gameObject);
            }
        }

        [Test]
        public void OverlayCoordinator_TracksFrontmostVisibleLayer()
        {
            var root = CreateRect("OverlayLayer", null, new Vector2(1200f, 800f));
            try
            {
                var map = CreateRect("MapViewport", root, root.sizeDelta);
                var details = CreateRect("ContentDetailViewport", root, root.sizeDelta);
                var drawer = CreateRect("RightDrawerViewport", root, root.sizeDelta);
                var mapVisible = true;
                var detailsVisible = true;
                var drawerVisible = false;
                var coordinator = new OverlayLayerCoordinator();

                coordinator.Register(map, () => mapVisible);
                coordinator.Register(details, () => detailsVisible);
                coordinator.Register(drawer, () => drawerVisible);

                Assert.That(coordinator.IsTop(details), Is.True);
                Assert.That(coordinator.IsTop(drawer), Is.False);

                coordinator.BringToFront(map);
                Assert.That(coordinator.IsTop(map), Is.True);

                mapVisible = false;
                Assert.That(coordinator.IsTop(details), Is.True);

                drawerVisible = true;
                coordinator.BringToFront(drawer);
                Assert.That(coordinator.IsTop(drawer), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }

        [Test]
        public void VerticalSlideTransition_SnapsOpenAndClosedState()
        {
            var rect = CreateRect("Overlay", null, new Vector2(600f, 400f));
            try
            {
                var owner = rect.gameObject.AddComponent<GameRootView>();
                var canvasGroup = rect.gameObject.AddComponent<CanvasGroup>();
                var isOpen = false;
                var transition = new VerticalSlideTransition(
                    owner,
                    rect,
                    canvasGroup,
                    () => isOpen);
                transition.SetSlideDistance(300f);

                isOpen = true;
                transition.Play(1f, 0f);
                Assert.That(rect.anchoredPosition.y, Is.EqualTo(0f));
                Assert.That(canvasGroup.alpha, Is.EqualTo(1f));
                Assert.That(canvasGroup.interactable, Is.True);
                Assert.That(canvasGroup.blocksRaycasts, Is.True);

                isOpen = false;
                transition.Play(0f, 0f);
                Assert.That(rect.anchoredPosition.y, Is.EqualTo(300f));
                Assert.That(canvasGroup.alpha, Is.EqualTo(0f));
                Assert.That(canvasGroup.interactable, Is.False);
                Assert.That(canvasGroup.blocksRaycasts, Is.False);

                canvasGroup.alpha = 0.4f;
                var positionOnlyTransition = new VerticalSlideTransition(
                    owner,
                    rect,
                    canvasGroup,
                    () => isOpen,
                    applyAlpha: false);
                positionOnlyTransition.SetSlideDistance(300f);
                positionOnlyTransition.Snap(1f);
                Assert.That(canvasGroup.alpha, Is.EqualTo(0.4f));
            }
            finally
            {
                Object.DestroyImmediate(rect.gameObject);
            }
        }

        [Test]
        public void TrainerTab_RepeatedBindReusesBadgeAndRewardViews()
        {
            var root = CreateRect("TrainerTab", null, new Vector2(320f, 900f));
            try
            {
                var viewport = CreateRect("Viewport", root, root.sizeDelta);
                var content = CreateRect("Content", viewport, root.sizeDelta);
                content.gameObject.AddComponent<VerticalLayoutGroup>();
                var view = root.gameObject.AddComponent<TrainerTabView>();
                view.Configure(null, null, null, null, null, null);
                var preview = new TrainerPreviewContent(
                    null,
                    "Trainer",
                    12,
                    null,
                    new[]
                    {
                        new TrainerBadgePreview(PachimonAttribute.Fire, 1),
                        new TrainerBadgePreview(PachimonAttribute.Aqua, 1),
                    },
                    new[]
                    {
                        new TrainerRewardIconContent("炎", "#D85A45", amount: 60),
                        new TrainerRewardIconContent("水", "#4F8FCB", amount: 30),
                    },
                    null,
                    1000,
                    true);

                view.Bind(preview);
                var badgeGrid = root.Find(
                    "Viewport/Content/BadgeSection/BadgeGrid");
                var rewardLines = root.Find(
                    "Viewport/Content/RewardSummarySection/Lines");
                Assert.That(badgeGrid, Is.Not.Null);
                Assert.That(rewardLines, Is.Not.Null);
                var badgeCount = badgeGrid.childCount;
                var lineCount = rewardLines.childCount;

                view.Bind(preview);
                Assert.That(badgeGrid.childCount, Is.EqualTo(badgeCount));
                Assert.That(rewardLines.childCount, Is.EqualTo(lineCount));
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }

        [Test]
        public void ResponsiveGeometry_AppliesExpandedAndCompactBounds()
        {
            var body = CreateRect("Body", null, new Vector2(1200f, 700f));
            body.gameObject.SetActive(false);
            try
            {
                var main = CreateRect("MainPane", body, new Vector2(700f, 700f));
                var leftPane = CreateRect("LeftPane", body, new Vector2(250f, 700f));
                var rightPane = CreateRect("RightPane", body, new Vector2(250f, 700f));
                var overlay = CreateRect("OverlayLayer", body, body.sizeDelta);
                var leftDrawer = CreateRect("LeftDrawer", overlay, body.sizeDelta);
                var rightDrawer = CreateRect("RightDrawer", overlay, body.sizeDelta);
                var map = CreateRect("MapViewport", overlay, Vector2.zero);
                var item = CreateRect("ItemViewport", overlay, Vector2.zero);
                var settings = CreateRect("SettingsViewport", overlay, Vector2.zero);
                var details = CreateRect("DetailViewport", overlay, Vector2.zero);
                var log = CreateRect("LogWindow", main, new Vector2(700f, 210f));
                var mainView = main.gameObject.AddComponent<MainPaneView>();
                var logView = log.gameObject.AddComponent<LogWindowView>();
                mainView.Initialize(null, logView);
                var geometry = new ResponsiveUiGeometry(
                    body,
                    main,
                    leftPane,
                    rightPane,
                    overlay,
                    leftDrawer,
                    rightDrawer,
                    map,
                    item,
                    settings,
                    details,
                    mainView,
                    null,
                    null,
                    null);

                geometry.RefreshIfChanged(LayoutMode.Expanded);
                Assert.That(map.sizeDelta.x, Is.EqualTo(700f).Within(0.01f));
                Assert.That(map.sizeDelta.y, Is.EqualTo(700f).Within(0.01f));
                Assert.That(item.sizeDelta.x, Is.EqualTo(700f).Within(0.01f));
                Assert.That(item.sizeDelta.y, Is.EqualTo(210f).Within(0.01f));
                Assert.That(settings.sizeDelta.x, Is.EqualTo(574f).Within(0.01f));
                Assert.That(settings.sizeDelta.y, Is.EqualTo(434f).Within(0.01f));
                Assert.That(details.sizeDelta.x, Is.EqualTo(700f).Within(0.01f));

                geometry.RefreshIfChanged(LayoutMode.Compact);
                Assert.That(map.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(map.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(leftPane.sizeDelta.x, Is.EqualTo(1200f).Within(0.01f));
                Assert.That(rightPane.sizeDelta.x, Is.EqualTo(1200f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(body.gameObject);
            }
        }

        [Test]
        public void DialoguePlan_PreservesBlockStopsAndScrollsLongBlocks()
        {
            var page = new DialoguePage(new[]
            {
                new DialogueBlock(new[]
                {
                    new DialogueLine("A1"),
                    new DialogueLine("A2"),
                }),
                new DialogueBlock(new[]
                {
                    new DialogueLine("B1"),
                }),
                new DialogueBlock(new[]
                {
                    new DialogueLine("C1"),
                    new DialogueLine("C2"),
                    new DialogueLine("C3"),
                    new DialogueLine("C4"),
                    new DialogueLine("C5"),
                }),
            });

            var segments = DialoguePlaybackPlan.Create(page, 4);
            Assert.That(segments.Count, Is.EqualTo(4));
            Assert.That(segments[0].Text, Is.EqualTo("A1\nA2"));
            Assert.That(segments[1].Text, Is.EqualTo("A1\nA2\nB1"));
            Assert.That(segments[1].RevealFromLineIndex, Is.EqualTo(2));
            Assert.That(segments[2].Text, Is.EqualTo("C1\nC2\nC3\nC4"));
            Assert.That(segments[3].Text, Is.EqualTo("C2\nC3\nC4\nC5"));
            Assert.That(segments[3].RevealFromLineIndex, Is.EqualTo(3));
        }

        [Test]
        public void DialoguePlan_CountsWrappedLinesAgainstVisibleCapacity()
        {
            var page = new DialoguePage(new[]
            {
                new DialogueBlock(new[]
                {
                    new DialogueLine("A"),
                    new DialogueLine("Wrapped"),
                    new DialogueLine("B"),
                    new DialogueLine("C"),
                }),
            });

            var segments = DialoguePlaybackPlan.Create(
                page,
                4,
                line => line.Text == "Wrapped" ? 2 : 1);

            Assert.That(segments.Count, Is.EqualTo(2));
            Assert.That(segments[0].Text, Is.EqualTo("A\nWrapped\nB"));
            Assert.That(segments[1].Text, Is.EqualTo("Wrapped\nB\nC"));
            Assert.That(segments[1].RevealFromLineIndex, Is.EqualTo(2));
        }

        private static RectTransform CreateRect(
            string objectName,
            Transform parent,
            Vector2 size)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            return rect;
        }

        private static TMP_Text CreateText(string objectName, Transform parent)
        {
            var rect = CreateRect(objectName, parent, new Vector2(100f, 30f));
            return rect.gameObject.AddComponent<TextMeshProUGUI>();
        }

        private static Button CreateButton(string objectName, Transform parent)
        {
            var rect = CreateRect(objectName, parent, new Vector2(60f, 60f));
            rect.gameObject.AddComponent<Image>();
            return rect.gameObject.AddComponent<Button>();
        }
    }
}
