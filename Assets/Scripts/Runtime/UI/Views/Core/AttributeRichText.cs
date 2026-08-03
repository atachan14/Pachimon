using System;
using Pachimon.Run;
using Pachimon.Data;
using Pachimon.Reward;
using Pachimon.Skills;
using Pachimon.Battle;

namespace Pachimon.UI
{
    public static class AttributeRichText
    {
        private const string SpriteAssetName = "AttributeIcons";
        public const float StatLabelIconFontSize = 40f;

        public static string GetIcon(AllocationType type)
        {
            return TryGetAttribute(type, out var attribute)
                ? $"<sprite=\"{SpriteAssetName}\" name=\"{attribute}\">"
                : string.Empty;
        }

        public static string GetIcon(PachimonDisplayStat stat)
        {
            return TryGetAllocationType(stat, out var type)
                ? GetIcon(type)
                : string.Empty;
        }

        public static bool IsAttribute(PachimonDisplayStat stat)
        {
            return TryGetAllocationType(stat, out _);
        }

        public static string Colorize(AllocationType type, object value)
        {
            if (!TryGetAttribute(type, out var attribute))
            {
                return value?.ToString() ?? string.Empty;
            }

            return $"<color={RewardElementPalette.GetAttributeColorHex(attribute)}>"
                + $"{value}</color>";
        }

        public static bool TryGetDisplayStat(
            AllocationType type,
            out PachimonDisplayStat displayStat)
        {
            displayStat = type switch
            {
                AllocationType.Fire => PachimonDisplayStat.Fire,
                AllocationType.Aqua => PachimonDisplayStat.Aqua,
                AllocationType.Leaf => PachimonDisplayStat.Leaf,
                AllocationType.Electric => PachimonDisplayStat.Electric,
                AllocationType.Poison => PachimonDisplayStat.Poison,
                AllocationType.Ice => PachimonDisplayStat.Ice,
                AllocationType.Wind => PachimonDisplayStat.Wind,
                AllocationType.Dragon => PachimonDisplayStat.Dragon,
                _ => default,
            };
            return type != AllocationType.Unassigned;
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

        private static bool TryGetAllocationType(
            PachimonDisplayStat stat,
            out AllocationType type)
        {
            type = stat switch
            {
                PachimonDisplayStat.Fire => AllocationType.Fire,
                PachimonDisplayStat.Aqua => AllocationType.Aqua,
                PachimonDisplayStat.Leaf => AllocationType.Leaf,
                PachimonDisplayStat.Electric => AllocationType.Electric,
                PachimonDisplayStat.Poison => AllocationType.Poison,
                PachimonDisplayStat.Ice => AllocationType.Ice,
                PachimonDisplayStat.Wind => AllocationType.Wind,
                PachimonDisplayStat.Dragon => AllocationType.Dragon,
                _ => AllocationType.Unassigned,
            };
            return type != AllocationType.Unassigned;
        }
    }

    public static class SkillDetailDescriptionFormatter
    {
        public static string Format(
            SkillAsset skill,
            PachimonPreviewContent owner)
        {
            if (skill == null)
            {
                return "説明未設定";
            }

            if (skill is BackfireSkillAsset backfire
                && owner?.IsRevealed == true
                && owner.TryGetStat(
                    PachimonDisplayStat.Fire,
                    out var backfireFire)
                && owner.TryGetStat(
                    PachimonDisplayStat.Poison,
                    out var backfirePoison))
            {
                var displayedDamage = SignedStatMath.FloorNonNegative(
                    BackfireMath.CalculateBaseDamage(
                        backfire,
                        backfireFire));
                var penetration = BackfireMath.CalculatePenetrationPercent(
                    backfire,
                    backfirePoison);
                var fireIcon = AttributeRichText.GetIcon(AllocationType.Fire);
                var poisonIcon =
                    AttributeRichText.GetIcon(AllocationType.Poison);
                return $"敵の最後尾に{fireIcon}"
                    + $"{AttributeRichText.Colorize(AllocationType.Fire, displayedDamage)}"
                    + "のFireダメージを与える。"
                    + $"{poisonIcon}{backfirePoison}"
                    + $"により貫通率は{penetration:0.##}%。";
            }

            if (skill is FireArrowSkillAsset fireArrow
                && owner?.IsRevealed == true
                && owner.TryGetStat(
                    PachimonDisplayStat.Fire,
                    out var fireArrowFire))
            {
                var displayedDamage = SignedStatMath.FloorNonNegative(
                    FireArrowMath.CalculateBaseDamage(
                        fireArrow,
                        fireArrowFire));
                var fireIcon = AttributeRichText.GetIcon(AllocationType.Fire);
                return "CurrentHPが最も低い敵に"
                    + $"{fireIcon}"
                    + $"{AttributeRichText.Colorize(AllocationType.Fire, displayedDamage)}"
                    + "のFireダメージを与える。"
                    + "戦闘不能にした場合、"
                    + $"MNを{fireArrow.BaseManaCost}消費して再発動する。";
            }

            if (skill is CombustionSkillAsset combustion
                && owner?.IsRevealed == true
                && owner.TryGetStat(
                    PachimonDisplayStat.Fire,
                    out var combustionFire)
                && owner.TryGetStat(
                    PachimonDisplayStat.ResistBonus,
                    out var combustionResistBonus)
                && owner.TryGetStat(
                    PachimonDisplayStat.DamageBonus,
                    out var combustionDamageBonus))
            {
                var baseDamage =
                    CombustionMath.CalculateBaseDamage(
                        combustion,
                        combustionFire);
                var preDefenseDamage = baseDamage
                    * SignedStatMath.AmplificationMultiplier(
                        combustionDamageBonus);
                var enemyDamage = SignedStatMath.FloorNonNegative(
                    preDefenseDamage);
                var selfDamage = AttributeDamageCalculator.FinalizeNormalDamage(
                    preDefenseDamage
                    * SignedStatMath.ReductionMultiplier(combustionFire)
                    * SignedStatMath.ReductionMultiplier(
                        combustionResistBonus));
                var fireIcon = AttributeRichText.GetIcon(AllocationType.Fire);
                return $"先頭の敵に{fireIcon}{enemyDamage}"
                    + $"（軽減前）、自身に{fireIcon}{selfDamage}"
                    + "のFireダメージを与える。"
                    + $"両者が生存している間、MNを{combustion.BaseManaCost}"
                    + "消費して再発動する。";
            }

            if (skill is AquaShockSkillAsset aquaShock
                && owner?.IsRevealed == true
                && owner.TryGetStat(
                    PachimonDisplayStat.Electric,
                    out var aquaShockElectric)
                && owner.TryGetStat(
                    PachimonDisplayStat.Aqua,
                    out var aquaShockAqua))
            {
                var electricDamage = SignedStatMath.FloorNonNegative(
                    AquaShockMath.CalculateElectricBaseDamage(
                        aquaShock,
                        aquaShockElectric));
                var aquaDamage = SignedStatMath.FloorNonNegative(
                    AquaShockMath.CalculateAquaBaseDamage(
                        aquaShock,
                        aquaShockAqua));
                var leakValue = AquaShockMath.CalculateLeakValue(
                    aquaShock,
                    aquaShockAqua);
                var electricIcon =
                    AttributeRichText.GetIcon(AllocationType.Electric);
                var aquaIcon =
                    AttributeRichText.GetIcon(AllocationType.Aqua);
                return $"敵の先頭に{electricIcon}{electricDamage}と"
                    + $"{aquaIcon}{aquaDamage}のダメージを与える。"
                    + $"その後、値{leakValue}の漏電を付与する。";
            }

            if (skill is ElectricExplosionSkillAsset electricExplosion
                && owner?.IsRevealed == true
                && owner.TryGetStat(
                    PachimonDisplayStat.Electric,
                    out var electric)
                && owner.TryGetStat(
                    PachimonDisplayStat.Fire,
                    out var fire))
            {
                var displayedDamage = SignedStatMath.FloorNonNegative(
                    ElectricExplosionMath.CalculateBaseDamage(
                        electricExplosion,
                        electric,
                        fire));
                var penetration =
                    ElectricExplosionMath.CalculatePenetrationPercent(
                        electricExplosion,
                        fire);
                var electricIcon =
                    AttributeRichText.GetIcon(AllocationType.Electric);
                var fireIcon =
                    AttributeRichText.GetIcon(AllocationType.Fire);
                return $"敵の先頭に{electricIcon}"
                    + $"{AttributeRichText.Colorize(AllocationType.Electric, displayedDamage)}"
                    + "のElectricダメージを与える。"
                    + $"{fireIcon}{fire}により貫通率は{penetration:0.##}%。";
            }

            if (skill is ElectricQuickAttackSkillAsset quickAttack
                && owner?.IsRevealed == true
                && owner.TryGetStat(
                    PachimonDisplayStat.Electric,
                    out var quickElectric)
                && owner.TryGetStat(
                    PachimonDisplayStat.Fire,
                    out var quickFire)
                && owner.TryGetStat(
                    PachimonDisplayStat.Wind,
                    out var quickWind)
                && owner.TryGetStat(
                    PachimonDisplayStat.Speed,
                    out var quickSpeed)
                && owner.TryGetStat(
                    PachimonDisplayStat.Haste,
                    out var quickHaste))
            {
                var electricDamage = SignedStatMath.FloorNonNegative(
                    ElectricQuickAttackMath.CalculateElectricBaseDamage(
                        quickAttack,
                        quickElectric));
                var fireDamage = SignedStatMath.FloorNonNegative(
                    ElectricQuickAttackMath.CalculateFireBaseDamage(
                        quickAttack,
                        quickFire));
                var windMultiplier =
                    SkillTimingCalculator.CalculateWindMultiplier(
                        quickAttack,
                        quickWind);
                var recovery = BattleTickMath.GetEffectiveRecovery(
                    quickAttack.BaseRecoveryTicks,
                    quickSpeed,
                    windMultiplier);
                var cooldown = BattleTickMath.GetEffectiveCooldown(
                    quickAttack.BaseCooldownTicks,
                    quickHaste,
                    windMultiplier);
                var electricIcon =
                    AttributeRichText.GetIcon(AllocationType.Electric);
                var fireIcon =
                    AttributeRichText.GetIcon(AllocationType.Fire);
                return $"敵の先頭に{electricIcon}{electricDamage}と"
                    + $"{fireIcon}{fireDamage}のDamageを与える。"
                    + $"現在の硬直は{recovery}、CDは{cooldown}。";
            }

            if (skill is ElectromagneticCannonSkillAsset cannon
                && owner?.IsRevealed == true
                && owner.TryGetStat(
                    PachimonDisplayStat.Electric,
                    out var cannonElectric)
                && owner.TryGetStat(
                    PachimonDisplayStat.DamageBonus,
                    out var cannonDamageBonus)
                && owner.TryGetStat(
                    PachimonDisplayStat.Speed,
                    out var cannonSpeed)
                && owner.TryGetStat(
                    PachimonDisplayStat.Haste,
                    out var cannonHaste))
            {
                var preDefenseDamage = SignedStatMath.FloorNonNegative(
                    cannon.BasePower
                    * SignedStatMath.AmplificationMultiplier(cannonElectric)
                    * SignedStatMath.AmplificationMultiplier(cannonDamageBonus));
                var startup = BattleTickMath.GetEffectiveStartup(
                    cannon.BaseStartupTicks,
                    cannonSpeed);
                var recovery = BattleTickMath.GetEffectiveRecovery(
                    cannon.BaseRecoveryTicks,
                    cannonSpeed);
                var cooldown = BattleTickMath.GetEffectiveCooldown(
                    cannon.BaseCooldownTicks,
                    cannonHaste);
                var electricIcon =
                    AttributeRichText.GetIcon(AllocationType.Electric);
                return $"{startup}tick後、敵の先頭に{electricIcon}"
                    + $"{AttributeRichText.Colorize(AllocationType.Electric, preDefenseDamage)}"
                    + "（軽減前）のElectricダメージを与える。"
                    + "戦闘不能にした場合、超過分を次の先頭へ引き継ぐ。"
                    + $"現在の硬直は{recovery}、CDは{cooldown}。";
            }

            if (owner?.IsRevealed == true
                && AttributeRichText.TryGetDisplayStat(
                    skill.AllocationType,
                    out var displayStat)
                && owner.TryGetStat(displayStat, out var attributeValue))
            {
                const int baseDamage = 100;
                var displayedDamage = SignedStatMath.FloorNonNegative(
                    baseDamage * SignedStatMath.AmplificationMultiplier(attributeValue),
                    1);
                var icon = AttributeRichText.GetIcon(skill.AllocationType);
                return $"敵の先頭に{AttributeRichText.Colorize(skill.AllocationType, displayedDamage)}"
                    + $"（{baseDamage} + {icon}"
                    + $"{AttributeRichText.Colorize(skill.AllocationType, attributeValue)}"
                    + $" × {baseDamage}%）の{icon}ダメージを与える。";
            }

            return string.IsNullOrWhiteSpace(skill.Description)
                ? "説明未設定"
                : skill.Description;
        }
    }
}
