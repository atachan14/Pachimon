using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public sealed class PachimonSelectionOverlayView : MonoBehaviour
    {
        public static PachimonSelectionOverlayView CreateRuntime(Transform parent)
        {
            var root = new GameObject(
                "PachimonSelectionOverlay",
                typeof(RectTransform),
                typeof(Image),
                typeof(PachimonSelectionOverlayView));
            root.layer = parent.gameObject.layer;
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetAnchors(rect, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));
            rect.SetAsLastSibling();
            root.GetComponent<Image>().color = new Color32(245, 248, 242, 255);
            return root.GetComponent<PachimonSelectionOverlayView>();
        }

        public void Present(
            string title,
            string confirmLabel,
            IReadOnlyList<CityPachimonOption> options,
            Func<CityPachimonOption, string> getUnavailableReason,
            Action<string> onConfirm)
        {
            string selectedId = null;
            var heading = CreateText("Title", transform, title, 28f);
            SetAnchors(heading.rectTransform, new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.97f));

            var gridObject = new GameObject("PachimonGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridObject.layer = gameObject.layer;
            gridObject.transform.SetParent(transform, false);
            SetAnchors(
                gridObject.GetComponent<RectTransform>(),
                new Vector2(0.05f, 0.25f),
                new Vector2(0.95f, 0.82f));
            var grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(190f, 250f);
            grid.spacing = new Vector2(18f, 0f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            var bindings = new List<(Button Button, CityPachimonOption Option, string Reason)>();
            foreach (var option in options.Reverse())
            {
                var reason = getUnavailableReason?.Invoke(option);
                bindings.Add((CreateOption(gridObject.transform, option, reason), option, reason));
            }

            var back = CreateButton("Back", transform, "← 戻る");
            SetAnchors(back.GetComponent<RectTransform>(), new Vector2(0.2f, 0.07f), new Vector2(0.46f, 0.19f));
            back.onClick.AddListener(() => Destroy(gameObject));
            var confirm = CreateButton("Confirm", transform, confirmLabel);
            SetAnchors(confirm.GetComponent<RectTransform>(), new Vector2(0.54f, 0.07f), new Vector2(0.8f, 0.19f));
            confirm.interactable = false;

            foreach (var binding in bindings)
            {
                if (!string.IsNullOrEmpty(binding.Reason)) continue;
                binding.Button.onClick.AddListener(() =>
                {
                    selectedId = binding.Option.InstanceId;
                    foreach (var candidate in bindings)
                    {
                        candidate.Button.targetGraphic.color = candidate.Option.InstanceId == selectedId
                            ? GameUiPalette.ButtonAccent
                            : string.IsNullOrEmpty(candidate.Reason)
                                ? GameUiPalette.StatCard
                                : GameUiPalette.MissingGraphic;
                    }
                    confirm.interactable = true;
                });
            }

            confirm.onClick.AddListener(() =>
            {
                if (selectedId == null) return;
                onConfirm?.Invoke(selectedId);
                if (this != null) Destroy(gameObject);
            });
        }

        public void PresentSkillForget(
            string title,
            int price,
            IReadOnlyList<CityPachimonOption> options,
            Action<CitySkillOption> showDetails,
            Action<string, int> onConfirm)
        {
            void RenderPachimonSelection()
            {
                ClearContent();
                string selectedId = null;
                var heading = CreateText("Title", transform, title, 28f);
                SetAnchors(
                    heading.rectTransform,
                    new Vector2(0.04f, 0.84f),
                    new Vector2(0.96f, 0.97f));

                var gridObject = CreateGrid(
                    "PachimonGrid",
                    new Vector2(0.05f, 0.25f),
                    new Vector2(0.95f, 0.82f),
                    new Vector2(190f, 250f));
                var bindings = options.Reverse().Select(option =>
                {
                    var reason = option.SkillCount == 0
                        ? "忘れられる技がない"
                        : null;
                    return (Button: CreateOption(gridObject.transform, option, reason),
                        Option: option,
                        Reason: reason);
                }).ToArray();

                var back = CreateButton("Back", transform, "← 戻る");
                SetAnchors(
                    back.GetComponent<RectTransform>(),
                    new Vector2(0.2f, 0.07f),
                    new Vector2(0.46f, 0.19f));
                back.onClick.AddListener(() => Destroy(gameObject));
                var next = CreateButton("Next", transform, "スキルを選択");
                SetAnchors(
                    next.GetComponent<RectTransform>(),
                    new Vector2(0.54f, 0.07f),
                    new Vector2(0.8f, 0.19f));
                next.interactable = false;

                foreach (var binding in bindings)
                {
                    if (!string.IsNullOrEmpty(binding.Reason)) continue;
                    binding.Button.onClick.AddListener(() =>
                    {
                        selectedId = binding.Option.InstanceId;
                        foreach (var candidate in bindings)
                        {
                            candidate.Button.targetGraphic.color =
                                candidate.Option.InstanceId == selectedId
                                    ? GameUiPalette.ButtonAccent
                                    : GameUiPalette.StatCard;
                        }
                        next.interactable = true;
                    });
                }

                next.onClick.AddListener(() =>
                {
                    var selected = options.FirstOrDefault(
                        option => option.InstanceId == selectedId);
                    if (selected != null)
                    {
                        RenderSkillSelection(selected);
                    }
                });
            }

            void RenderSkillSelection(CityPachimonOption pachimon)
            {
                ClearContent();
                CitySkillOption selected = null;
                var heading = CreateText(
                    "Title",
                    transform,
                    $"{pachimon.DisplayName}が忘れる技を選択",
                    28f);
                SetAnchors(
                    heading.rectTransform,
                    new Vector2(0.04f, 0.84f),
                    new Vector2(0.96f, 0.97f));

                var gridObject = CreateGrid(
                    "SkillGrid",
                    new Vector2(0.05f, 0.27f),
                    new Vector2(0.95f, 0.82f),
                    new Vector2(190f, 105f));
                var bindings = pachimon.Skills.Select(skill =>
                {
                    var button = CreateButton("Skill", gridObject.transform, skill.DisplayName);
                    button.targetGraphic.color = GameUiPalette.SkillChip;
                    button.GetComponentInChildren<TMP_Text>().color =
                        AttributeCardPalette.GetReadableTextColor(GameUiPalette.SkillChip);
                    return (Button: button, Skill: skill);
                }).ToArray();

                var back = CreateButton("Back", transform, "← 戻る");
                SetAnchors(
                    back.GetComponent<RectTransform>(),
                    new Vector2(0.04f, 0.07f),
                    new Vector2(0.3f, 0.19f));
                back.onClick.AddListener(RenderPachimonSelection);

                var details = CreateButton("Details", transform, "詳細を見る");
                SetAnchors(
                    details.GetComponent<RectTransform>(),
                    new Vector2(0.37f, 0.07f),
                    new Vector2(0.63f, 0.19f));
                details.interactable = false;

                var confirm = CreateButton(
                    "Confirm",
                    transform,
                    $"忘れる  {price} Gold");
                SetAnchors(
                    confirm.GetComponent<RectTransform>(),
                    new Vector2(0.7f, 0.07f),
                    new Vector2(0.96f, 0.19f));
                confirm.interactable = false;

                foreach (var binding in bindings)
                {
                    binding.Button.onClick.AddListener(() =>
                    {
                        selected = binding.Skill;
                        foreach (var candidate in bindings)
                        {
                            candidate.Button.targetGraphic.color =
                                candidate.Skill.SlotId == selected.SlotId
                                    ? GameUiPalette.ButtonAccent
                                    : GameUiPalette.SkillChip;
                        }
                        details.interactable = true;
                        confirm.interactable = true;
                    });
                }

                details.onClick.AddListener(() =>
                {
                    if (selected != null)
                    {
                        showDetails?.Invoke(selected);
                    }
                });
                confirm.onClick.AddListener(() =>
                {
                    if (selected == null) return;
                    onConfirm?.Invoke(pachimon.InstanceId, selected.SlotId);
                    if (this != null) Destroy(gameObject);
                });
            }

            RenderPachimonSelection();
        }

        private RectTransform CreateGrid(
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 cellSize)
        {
            var gridObject = new GameObject(name, typeof(RectTransform), typeof(GridLayoutGroup));
            gridObject.layer = gameObject.layer;
            gridObject.transform.SetParent(transform, false);
            var rect = gridObject.GetComponent<RectTransform>();
            SetAnchors(rect, anchorMin, anchorMax);
            var grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = cellSize;
            grid.spacing = new Vector2(18f, 8f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            return rect;
        }

        private void ClearContent()
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        private static Button CreateOption(
            Transform parent,
            CityPachimonOption option,
            string reason)
        {
            var button = CreateButton("Pachimon", parent, string.Empty);
            button.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
            button.targetGraphic.color = string.IsNullOrEmpty(reason)
                ? GameUiPalette.StatCard
                : GameUiPalette.MissingGraphic;
            button.interactable = string.IsNullOrEmpty(reason);
            var vertical = button.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(8, 8, 8, 8);
            vertical.spacing = 5f;
            vertical.childAlignment = TextAnchor.MiddleCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            var graphicObject = new GameObject("Graphic", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            graphicObject.layer = parent.gameObject.layer;
            graphicObject.transform.SetParent(button.transform, false);
            var graphic = graphicObject.GetComponent<Image>();
            graphic.sprite = option.FrontSprite;
            graphic.preserveAspect = true;
            graphic.enabled = option.FrontSprite != null;
            graphicObject.GetComponent<LayoutElement>().preferredHeight = 155f;
            var name = CreateText("Name", button.transform, option.DisplayName, 19f);
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
            if (!string.IsNullOrEmpty(reason))
            {
                var unavailable = CreateText("Unavailable", button.transform, reason, 15f);
                unavailable.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
            }
            return button;
        }

        private static Button CreateButton(string name, Transform parent, string label)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            root.layer = parent.gameObject.layer;
            root.transform.SetParent(parent, false);
            root.GetComponent<Image>().color = GameUiPalette.StatCard;
            var button = root.GetComponent<Button>();
            var text = CreateText("Label", root.transform, label, 18f);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.one * 5f;
            text.rectTransform.offsetMax = Vector2.one * -5f;
            return button;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string value,
            float size)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            root.layer = parent.gameObject.layer;
            root.transform.SetParent(parent, false);
            var text = root.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = GameUiPalette.PrimaryText;
            return text;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
