using System;
using System.Collections.Generic;
using Pachimon.Reward;
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
            new(PachimonDisplayStat.Fire, "F"),
            new(PachimonDisplayStat.Dragon, "D"),
            new(PachimonDisplayStat.Ice, "I"),
            new(PachimonDisplayStat.Poison, "P"),
            new(PachimonDisplayStat.Aqua, "A"),
            new(PachimonDisplayStat.Wind, "W"),
            new(PachimonDisplayStat.Electric, "E"),
            new(PachimonDisplayStat.Leaf, "L"),
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
            SetPreferredHeight(graphicArea, 300f);
            var graphic = CreateObject(
                graphicArea.transform,
                "FrontGraphic",
                typeof(CanvasRenderer),
                typeof(Image));
            var graphicRect = (RectTransform)graphic.transform;
            graphicRect.anchorMin = graphicRect.anchorMax = new Vector2(0.5f, 0.5f);
            graphicRect.pivot = new Vector2(0.5f, 0.5f);
            graphicRect.sizeDelta = new Vector2(280f, 280f);
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
            var statSlots = new List<PachimonStatSlotView>(StatVisuals.Length);
            for (var index = 0; index < StatVisuals.Length; index++)
            {
                statSlots.Add(CreateStatSlot(
                    attributeStatsGrid.transform,
                    StatVisuals[index]));
            }
            attributeStatsGrid.GetComponent<ResponsiveGridLayout>().RefreshLayout();

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

            var equipmentSection = CreateSection(
                content.transform,
                "EquipmentSection",
                "装備",
                GameUiPalette.SkillSection);
            var equipmentGrid = CreateGrid(
                equipmentSection.transform,
                "EquipmentGrid",
                3,
                80f,
                58f);
            var equipmentTemplate = CreateChip(
                equipmentGrid.transform,
                "EquipmentTemplate",
                "なし",
                GameUiPalette.ButtonNeutral);
            equipmentTemplate.gameObject.SetActive(false);

            var engravingSection = CreateSection(
                content.transform,
                "EngravingSection",
                "刻印",
                GameUiPalette.PassiveSection);
            var engravingGrid = CreateGrid(
                engravingSection.transform,
                "EngravingGrid",
                3,
                80f,
                58f);
            var engravingTemplate = CreateChip(
                engravingGrid.transform,
                "EngravingTemplate",
                "なし",
                GameUiPalette.ItemChip);
            engravingTemplate.gameObject.SetActive(false);
            statusGrid.GetComponent<ResponsiveGridLayout>().RefreshLayout();
            passiveGrid.GetComponent<ResponsiveGridLayout>().RefreshLayout();
            equipmentGrid.GetComponent<ResponsiveGridLayout>().RefreshLayout();
            engravingGrid.GetComponent<ResponsiveGridLayout>().RefreshLayout();

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
                passiveTemplate,
                equipmentGrid.transform,
                equipmentTemplate,
                engravingGrid.transform,
                engravingTemplate);
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
            var iconColor = GetStatColor(visual.Stat);
            var isAttribute = AttributeRichText.IsAttribute(visual.Stat);
            icon.GetComponent<Image>().color = isAttribute
                ? GameUiPalette.Transparent
                : iconColor;
            var iconLayout = icon.GetComponent<LayoutElement>();
            iconLayout.minWidth = 84f;
            iconLayout.preferredWidth = 84f;
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
            var iconTextRect = (RectTransform)iconText.transform;
            iconTextRect.anchorMin = Vector2.zero;
            iconTextRect.anchorMax = new Vector2(0.5f, 1f);
            iconTextRect.offsetMin = Vector2.zero;
            iconTextRect.offsetMax = Vector2.zero;

            var subStatBadge = CreateObject(
                icon.transform,
                "SubStatBadge",
                typeof(CanvasRenderer),
                typeof(Image));
            var badgeRect = (RectTransform)subStatBadge.transform;
            badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(1f, 0.5f);
            badgeRect.pivot = new Vector2(1f, 0.5f);
            badgeRect.anchoredPosition = Vector2.zero;
            badgeRect.sizeDelta = new Vector2(40f, 40f);
            var subStatIcon = subStatBadge.GetComponent<Image>();
            subStatIcon.color = Color.white;
            subStatIcon.preserveAspect = true;
            subStatIcon.raycastTarget = false;
            var subStatText = CreateText(
                subStatBadge.transform,
                "Label",
                8f,
                FontStyles.Bold);
            subStatText.text = "DB";
            subStatText.color = AttributeCardPalette.GetReadableTextColor(
                new[] { RewardElementPalette.TimingColor });
            subStatText.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)subStatText.transform);
            subStatText.gameObject.SetActive(false);

            var value = CreateText(root.transform, "Value", 17f, FontStyles.Bold);
            value.text = "0";
            value.alignment = TextAlignmentOptions.MidlineRight;
            var valueLayout = value.gameObject.AddComponent<LayoutElement>();
            valueLayout.flexibleWidth = 1f;

            var view = root.GetComponent<PachimonStatSlotView>();
            view.Configure(
                visual.Stat,
                value,
                subStatBadge,
                subStatText,
                subStatIcon);
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

        private static Color GetStatColor(PachimonDisplayStat stat)
        {
            return stat switch
            {
                PachimonDisplayStat.Fire =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Fire),
                PachimonDisplayStat.Aqua =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Aqua),
                PachimonDisplayStat.Leaf =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Leaf),
                PachimonDisplayStat.Electric =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Electric),
                PachimonDisplayStat.Poison =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Poison),
                PachimonDisplayStat.Ice =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Ice),
                PachimonDisplayStat.Wind =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Wind),
                PachimonDisplayStat.Dragon =>
                    RewardElementPalette.GetAttributeColor(PachimonAttribute.Dragon),
                PachimonDisplayStat.DamageBonus
                    or PachimonDisplayStat.GenerationPower
                    or PachimonDisplayStat.Haste
                    or PachimonDisplayStat.Speed
                    or PachimonDisplayStat.ResistBonus
                    or PachimonDisplayStat.SustainPower
                    or PachimonDisplayStat.StatusMastery
                    or PachimonDisplayStat.StatusResistance =>
                    RewardElementPalette.TimingColor,
                _ => GameUiPalette.StatCard,
            };
        }

        private readonly struct StatVisual
        {
            public StatVisual(PachimonDisplayStat stat, string label)
            {
                Stat = stat;
                Label = label;
            }

            public PachimonDisplayStat Stat { get; }
            public string Label { get; }
        }
    }
}
