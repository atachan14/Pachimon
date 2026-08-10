using System;
using System.Collections.Generic;
using Pachimon.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.Editor.UI
{
    public static class PachimonTabLayoutSetup
    {
        private const string MenuPath = "Tools/Pachimon/UI/Upgrade Pachimon Tab Layouts";

        private static readonly StatVisual[] StatVisuals =
        {
            new(PachimonDisplayStat.Fire, "F", "#E84B3C"),
            new(PachimonDisplayStat.Poison, "P", "#FFA7DF"),
            new(PachimonDisplayStat.Aqua, "A", "#356AE0"),
            new(PachimonDisplayStat.Ice, "I", "#62D5E6"),
            new(PachimonDisplayStat.Leaf, "L", "#288A47"),
            new(PachimonDisplayStat.Wind, "W", "#91C83E"),
            new(PachimonDisplayStat.Electric, "E", "#F2C94C"),
            new(PachimonDisplayStat.Dragon, "D", "#707887"),
            new(PachimonDisplayStat.Speed, "SPD", "#49A078"),
            new(PachimonDisplayStat.Haste, "HST", "#3AAFB9"),
            new(PachimonDisplayStat.DamageBonus, "DB", "#D97945"),
            new(PachimonDisplayStat.ResistBonus, "RB", "#6B7280"),
        };

        [MenuItem(MenuPath)]
        private static void UpgradeFromMenu()
        {
            var tabs = UnityEngine.Object.FindObjectsByType<PachimonTabView>(
                FindObjectsInactive.Include);
            if (tabs.Length == 0)
            {
                Debug.LogError("PachimonTabView was not found in the open Scene.");
                return;
            }

            Undo.SetCurrentGroupName("Upgrade Pachimon Tab Layouts");
            var undoGroup = Undo.GetCurrentGroup();
            foreach (var tab in tabs) Rebuild(tab);

            EditorSceneManager.MarkSceneDirty(tabs[0].gameObject.scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = tabs[0].gameObject;
            Debug.Log($"Upgraded {tabs.Length} Pachimon tab layouts.", tabs[0]);
        }

        public static void Rebuild(PachimonTabView tab)
        {
            if (tab == null) throw new ArgumentNullException(nameof(tab));
            var scrollRect = tab.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                Debug.LogError($"{tab.name} requires ScrollRect.", tab);
                return;
            }

            var viewport = scrollRect.viewport != null
                ? scrollRect.viewport
                : tab.transform.Find("Viewport") as RectTransform;
            if (viewport == null)
            {
                Debug.LogError($"{tab.name} requires Viewport.", tab);
                return;
            }

            for (var index = viewport.childCount - 1; index >= 0; index--)
            {
                Undo.DestroyObjectImmediate(viewport.GetChild(index).gameObject);
            }

            var content = CreateObject(
                viewport,
                "Content",
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            var contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(12, 12, 14, 18);
            contentLayout.spacing = 12f;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRect;

            var graphicArea = CreateObject(content.transform, "GraphicArea", typeof(LayoutElement));
            SetPreferredHeight(graphicArea, 174f);
            var graphic = CreateObject(
                graphicArea.transform,
                "FrontGraphic",
                typeof(CanvasRenderer),
                typeof(Image));
            var graphicRect = (RectTransform)graphic.transform;
            graphicRect.anchorMin = graphicRect.anchorMax = new Vector2(0.5f, 0.5f);
            graphicRect.pivot = new Vector2(0.5f, 0.5f);
            graphicRect.sizeDelta = new Vector2(160f, 160f);
            graphicRect.anchoredPosition = Vector2.zero;
            var frontImage = graphic.GetComponent<Image>();
            frontImage.preserveAspect = true;
            frontImage.raycastTarget = false;

            var nameText = CreateText(content.transform, "Name", 25f, FontStyles.Bold);
            nameText.alignment = TextAlignmentOptions.Center;
            SetPreferredHeight(nameText.gameObject, 38f);
            var hpText = CreateText(content.transform, "Hp", 21f, FontStyles.Bold);
            hpText.alignment = TextAlignmentOptions.Center;
            SetPreferredHeight(hpText.gameObject, 34f);
            var mnText = CreateText(content.transform, "Mn", 21f, FontStyles.Bold);
            mnText.alignment = TextAlignmentOptions.Center;
            SetPreferredHeight(mnText.gameObject, 34f);

            var statsSection = CreateObject(
                content.transform,
                "StatsSection",
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            var statsLayout = statsSection.GetComponent<VerticalLayoutGroup>();
            statsLayout.spacing = 19f;
            statsLayout.childControlWidth = true;
            statsLayout.childControlHeight = true;
            statsLayout.childForceExpandWidth = true;
            statsLayout.childForceExpandHeight = false;
            statsSection.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            var attributeStatsGrid = CreateGrid(
                statsSection.transform,
                "AttributeStatsGrid",
                2,
                120f,
                38f);
            var generalStatsGrid = CreateGrid(
                statsSection.transform,
                "GeneralStatsGrid",
                2,
                120f,
                38f);
            var statSlots = new List<PachimonStatSlotView>(StatVisuals.Length);
            for (var index = 0; index < StatVisuals.Length; index++)
            {
                var parent = index < 8
                    ? attributeStatsGrid.transform
                    : generalStatsGrid.transform;
                statSlots.Add(CreateStatSlot(parent, StatVisuals[index]));
            }
            attributeStatsGrid.GetComponent<ResponsiveGridLayout>().RefreshLayout();
            generalStatsGrid.GetComponent<ResponsiveGridLayout>().RefreshLayout();

            var statusSection = CreateSection(
                content.transform,
                "StatusSection",
                "状態",
                GameUiPalette.StatusSection);
            var statusGrid = CreateGrid(statusSection.transform, "StatusGrid", 0, 92f, 34f);
            var statusTemplate = CreateChip(
                statusGrid.transform,
                "StatusTemplate",
                "なし",
                GameUiPalette.StatusChip);
            statusTemplate.gameObject.SetActive(false);

            var skillSection = CreateSection(
                content.transform,
                "SkillSection",
                "スキル一覧",
                GameUiPalette.SkillSection);
            var skillGrid = CreateGrid(skillSection.transform, "SkillGrid", 3, 80f, 42f);
            var skillSlots = new TextChipView[PachimonTabView.SkillSlotCount];
            for (var index = 0; index < skillSlots.Length; index++)
            {
                skillSlots[index] = CreateChip(
                    skillGrid.transform,
                    $"Skill{index + 1}",
                    "---",
                    GameUiPalette.SkillChip);
            }
            skillGrid.GetComponent<ResponsiveGridLayout>().RefreshLayout();

            var passiveSection = CreateSection(
                content.transform,
                "PassiveSection",
                "パッシヴ一覧",
                GameUiPalette.PassiveSection);
            var passiveGrid = CreateGrid(passiveSection.transform, "PassiveGrid", 3, 80f, 42f);
            var passiveTemplate = CreateChip(
                passiveGrid.transform,
                "PassiveTemplate",
                "なし",
                GameUiPalette.PassiveChip);
            passiveTemplate.gameObject.SetActive(false);
            statusGrid.GetComponent<ResponsiveGridLayout>().RefreshLayout();
            passiveGrid.GetComponent<ResponsiveGridLayout>().RefreshLayout();

            Undo.RecordObject(tab, "Configure Pachimon Tab");
            tab.Configure(
                frontImage,
                nameText,
                hpText,
                mnText,
                statSlots.ToArray(),
                statusGrid.transform,
                statusTemplate,
                skillSlots,
                passiveGrid.transform,
                passiveTemplate);
            EditorUtility.SetDirty(tab);
        }

        private static PachimonStatSlotView CreateStatSlot(Transform parent, StatVisual visual)
        {
            var root = CreateObject(
                parent,
                visual.Stat.ToString(),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(HorizontalLayoutGroup),
                typeof(PachimonStatSlotView));
            root.GetComponent<Image>().color = GameUiPalette.StatCard;
            var layout = root.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 8, 4, 4);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var icon = CreateObject(
                root.transform,
                "Icon",
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement));
            ColorUtility.TryParseHtmlString(visual.ColorHex, out var iconColor);
            var isAttribute = AttributeRichText.IsAttribute(visual.Stat);
            icon.GetComponent<Image>().color = isAttribute
                ? GameUiPalette.Transparent
                : iconColor;
            var iconLayout = icon.GetComponent<LayoutElement>();
            iconLayout.minWidth = 48f;
            iconLayout.preferredWidth = 48f;
            iconLayout.flexibleWidth = 0f;
            var iconText = CreateText(icon.transform, "Label", 11f, FontStyles.Bold);
            iconText.richText = true;
            iconText.text = isAttribute
                ? AttributeRichText.GetIcon(visual.Stat)
                : visual.Label;
            iconText.fontSize = isAttribute
                ? AttributeRichText.StatLabelIconFontSize
                : 11f;
            iconText.color = isAttribute
                ? Color.white
                : GetContrastingTextColor(iconColor);
            iconText.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)iconText.transform);

            var value = CreateText(root.transform, "Value", 17f, FontStyles.Bold);
            value.text = "0";
            value.alignment = TextAlignmentOptions.MidlineRight;
            var valueLayout = value.gameObject.AddComponent<LayoutElement>();
            valueLayout.flexibleWidth = 1f;

            var view = root.GetComponent<PachimonStatSlotView>();
            view.Configure(visual.Stat, value);
            return view;
        }

        private static GameObject CreateSection(
            Transform parent,
            string objectName,
            string title,
            Color backgroundColor)
        {
            var section = CreateObject(
                parent,
                objectName,
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            section.GetComponent<Image>().color = backgroundColor;
            var outline = section.GetComponent<Outline>();
            outline.effectColor = GameUiPalette.Border;
            outline.effectDistance = new Vector2(1f, -1f);
            var layout = section.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 9, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            section.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var titleText = CreateText(section.transform, "Title", 18f, FontStyles.Bold);
            titleText.text = title;
            SetPreferredHeight(titleText.gameObject, 28f);
            return section;
        }

        private static GameObject CreateGrid(
            Transform parent,
            string objectName,
            int fixedColumns,
            float minimumCellWidth,
            float cellHeight)
        {
            var grid = CreateObject(
                parent,
                objectName,
                typeof(GridLayoutGroup),
                typeof(LayoutElement),
                typeof(ResponsiveGridLayout));
            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.spacing = new Vector2(7f, 7f);
            layout.childAlignment = TextAnchor.UpperLeft;
            grid.GetComponent<ResponsiveGridLayout>().Configure(
                fixedColumns,
                minimumCellWidth,
                cellHeight);
            return grid;
        }

        private static TextChipView CreateChip(
            Transform parent,
            string objectName,
            string label,
            Color color)
        {
            var chip = CreateObject(
                parent,
                objectName,
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(TextChipView));
            chip.GetComponent<Image>().color = color;
            var text = CreateText(chip.transform, "Label", 14f, FontStyles.Normal);
            text.text = label;
            text.color = GetContrastingTextColor(color);
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            Stretch((RectTransform)text.transform);
            var view = chip.GetComponent<TextChipView>();
            view.Configure(text);
            return view;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string objectName,
            float fontSize,
            FontStyles fontStyle)
        {
            var target = CreateObject(parent, objectName, typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var text = target.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = GameUiPalette.PrimaryText;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateObject(
            Transform parent,
            string objectName,
            params Type[] components)
        {
            var target = new GameObject(objectName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(target, $"Create {objectName}");
            Undo.SetTransformParent(target.transform, parent, $"Parent {objectName}");
            target.layer = parent.gameObject.layer;
            foreach (var component in components) Undo.AddComponent(target, component);
            return target;
        }

        private static void SetPreferredHeight(GameObject target, float height)
        {
            var layout = target.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(target);
            layout.minHeight = 0f;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Color GetContrastingTextColor(Color background)
        {
            var luminance = (0.299f * background.r)
                + (0.587f * background.g)
                + (0.114f * background.b);
            return luminance > 0.62f ? new Color(0.08f, 0.09f, 0.09f, 1f) : Color.white;
        }

        private readonly struct StatVisual
        {
            public StatVisual(PachimonDisplayStat stat, string label, string colorHex)
            {
                Stat = stat;
                Label = label;
                ColorHex = colorHex;
            }

            public PachimonDisplayStat Stat { get; }
            public string Label { get; }
            public string ColorHex { get; }
        }
    }
}
