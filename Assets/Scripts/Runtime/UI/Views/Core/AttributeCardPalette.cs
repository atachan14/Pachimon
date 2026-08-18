using System.Collections.Generic;
using Pachimon.Battle;
using Pachimon.Data;
using Pachimon.Reward;
using Pachimon.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace Pachimon.UI
{
    public static class AttributeCardPalette
    {
        private const string GradientObjectName = "AttributeGradient";

        public static IReadOnlyList<Color> GetSkillColors(SkillAsset skill)
        {
            if (skill == null)
            {
                return System.Array.Empty<Color>();
            }

            var attributes = skill switch
            {
                KachofugetsuSkillAsset => new[]
                {
                    PachimonAttribute.Fire,
                    PachimonAttribute.Aqua,
                    PachimonAttribute.Wind,
                },
                AquaShockSkillAsset => new[]
                {
                    PachimonAttribute.Electric,
                    PachimonAttribute.Aqua,
                },
                BackfireSkillAsset => new[]
                {
                    PachimonAttribute.Fire,
                    PachimonAttribute.Poison,
                },
                ElectricQuickAttackSkillAsset => new[]
                {
                    PachimonAttribute.Electric,
                    PachimonAttribute.Fire,
                    PachimonAttribute.Wind,
                },
                ElectricExplosionSkillAsset => new[]
                {
                    PachimonAttribute.Electric,
                    PachimonAttribute.Fire,
                },
                NeurotoxinSkillAsset => new[]
                {
                    PachimonAttribute.Poison,
                    PachimonAttribute.Electric,
                },
                ToxinExplosionSkillAsset => new[]
                {
                    PachimonAttribute.Poison,
                    PachimonAttribute.Fire,
                },
                FireVineSkillAsset => new[]
                {
                    PachimonAttribute.Leaf,
                    PachimonAttribute.Fire,
                },
                _ => TryGetAttribute(skill.AllocationType, out var attribute)
                    ? new[] { attribute }
                    : System.Array.Empty<PachimonAttribute>(),
            };

            return ToColors(attributes);
        }

        public static IReadOnlyList<Color> GetStatusColors(
            BattleStatusInstance status)
        {
            if (status == null)
            {
                return System.Array.Empty<Color>();
            }

            var attributes = status.StatusId switch
            {
                BattleStatusId.Leak or BattleStatusId.StoredCharge
                    or BattleStatusId.Paralysis or BattleStatusId.Charge
                    or BattleStatusId.ElectricShield =>
                    new[] { PachimonAttribute.Electric },
                BattleStatusId.Chill or BattleStatusId.Freeze
                    or BattleStatusId.IceGrowth
                    or BattleStatusId.FrozenBreakSelf =>
                    new[] { PachimonAttribute.Ice },
                BattleStatusId.Toxin or BattleStatusId.ToxinGrowth
                    or BattleStatusId.PoisonMagicianGrowth =>
                    new[] { PachimonAttribute.Poison },
                BattleStatusId.FireGrowth or BattleStatusId.AddChain
                    or BattleStatusId.Burn =>
                    new[] { PachimonAttribute.Fire },
                BattleStatusId.LaunchCeremony =>
                    new[] { PachimonAttribute.Aqua },
                BattleStatusId.LeafGrowth =>
                    new[] { PachimonAttribute.Leaf },
                BattleStatusId.Flying or BattleStatusId.WindErosion
                    or BattleStatusId.HealingWind or BattleStatusId.StillAir
                    or BattleStatusId.WindRiderGrowth
                    or BattleStatusId.WindMagicianGrowth =>
                    new[] { PachimonAttribute.Wind },
                BattleStatusId.OneTwo or BattleStatusId.DragonBoxer
                    or BattleStatusId.Footwork or BattleStatusId.SweetScience
                    or BattleStatusId.DragonDance
                    or BattleStatusId.DragonCranker
                    or BattleStatusId.DragonDefense =>
                    new[] { PachimonAttribute.Dragon },
                BattleStatusId.BurningFlowerLeaf =>
                    new[] { PachimonAttribute.Leaf, PachimonAttribute.Fire },
                BattleStatusId.BurningFlowerFire =>
                    new[] { PachimonAttribute.Fire, PachimonAttribute.Leaf },
                _ => System.Array.Empty<PachimonAttribute>(),
            };

            return ToColors(attributes);
        }

        public static Color Apply(GameObject target, IReadOnlyList<Color> colors)
        {
            if (target == null || colors == null || colors.Count == 0)
            {
                return GameUiPalette.PrimaryText;
            }

            var baseImage = target.GetComponent<Image>();
            if (baseImage != null)
            {
                baseImage.color = Color.clear;
            }

            var gradientTransform = target.transform.Find(GradientObjectName);
            AttributeGradientGraphic gradient;
            if (gradientTransform == null)
            {
                var gradientObject = new GameObject(
                    GradientObjectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(AttributeGradientGraphic));
                gradientObject.layer = target.layer;
                gradientObject.transform.SetParent(target.transform, false);
                gradientTransform = gradientObject.transform;
                var rect = gradientObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                gradient = gradientObject.GetComponent<AttributeGradientGraphic>();
                gradient.raycastTarget = false;
            }
            else
            {
                gradient = gradientTransform.GetComponent<AttributeGradientGraphic>();
            }

            gradientTransform.SetAsFirstSibling();
            gradient.SetColors(colors);
            gradient.enabled = true;
            return GetReadableTextColor(colors);
        }

        public static Color GetReadableTextColor(IReadOnlyList<Color> colors)
        {
            if (colors == null || colors.Count == 0)
            {
                return GameUiPalette.PrimaryText;
            }

            var luminance = 0f;
            for (var index = 0; index < colors.Count; index++)
            {
                var color = colors[index];
                luminance += color.r * 0.299f
                    + color.g * 0.587f
                    + color.b * 0.114f;
            }

            luminance /= colors.Count;
            return GetReadableTextColorFromLuminance(luminance);
        }

        public static Color GetReadableTextColor(Color background)
        {
            if (background.a <= 0.1f)
            {
                return GameUiPalette.PrimaryText;
            }

            var luminance = background.r * 0.299f
                + background.g * 0.587f
                + background.b * 0.114f;
            return GetReadableTextColorFromLuminance(luminance);
        }

        private static Color GetReadableTextColorFromLuminance(float luminance)
        {
            return luminance >= 0.58f
                ? new Color(0.04f, 0.05f, 0.06f, 1f)
                : Color.white;
        }

        private static Color[] ToColors(IReadOnlyList<PachimonAttribute> attributes)
        {
            var colors = new Color[attributes.Count];
            for (var index = 0; index < attributes.Count; index++)
            {
                colors[index] = RewardElementPalette.GetAttributeColor(
                    attributes[index]);
            }
            return colors;
        }

        private static bool TryGetAttribute(
            AllocationType type,
            out PachimonAttribute attribute)
        {
            if (type < AllocationType.Fire || type > AllocationType.Dragon)
            {
                attribute = default;
                return false;
            }

            attribute = (PachimonAttribute)((int)type - 1);
            return true;
        }
    }
}
