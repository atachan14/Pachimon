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

            if (skill.Description?.Contains("{", StringComparison.Ordinal) == true
                && SkillDescriptionValueProviderRegistry.TryCreateContext(
                    skill,
                    owner,
                    out var templateContext))
            {
                return DescriptionTemplateFormatter.Format(
                    skill.Description,
                    templateContext);
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

            if (skill is DragonJabSkillAsset dragonJab
                && owner?.IsRevealed == true
                && owner.TryGetStat(
                    PachimonDisplayStat.Dragon,
                    out var dragon))
            {
                var damage = SignedStatMath.FloorNonNegative(
                    SignedStatMath.ScaleFromBase(
                        dragonJab.BaseDragonDamage,
                        dragon,
                        dragonJab.DragonDamageRatio));
                var icon = AttributeRichText.GetIcon(AllocationType.Dragon);
                return $"\u6575\u306E\u5148\u982D\u306B{icon}"
                    + $"{AttributeRichText.Colorize(AllocationType.Dragon, damage)}"
                    + "\u306E\u7ADC\u30C0\u30E1\u30FC\u30B8\u3092\u4E0E\u3048\u3001"
                    + $"\u30EF\u30F3\u30FB\u30C4\u30FCValue\u3092{dragonJab.OneTwoValue}\u7372\u5F97\u3059\u308B\u3002";
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
                    + "両者が生存している間、MNを追加消費せず再発動する。";
            }

            if (skill is ChainBurnSkillAsset chainBurn
                && owner?.IsRevealed == true
                && owner.TryGetStat(
                    PachimonDisplayStat.Fire,
                    out var chainBurnFire))
            {
                var baseDamage = SignedStatMath.FloorNonNegative(
                    SignedStatMath.ScaleFromBase(
                        chainBurn.BasePower,
                        chainBurnFire,
                        chainBurn.FireScalingPercent));
                var fireIcon = AttributeRichText.GetIcon(AllocationType.Fire);
                var hitCount = chainBurn.BaseChainCount + 1;
                return $"先頭から後方へ往復し、{hitCount}回連鎖する。"
                    + $"初撃は{fireIcon}"
                    + AttributeRichText.Colorize(
                        AllocationType.Fire,
                        baseDamage)
                    + "、以降は連鎖順に減衰する。"
                    + "使用するたびにアドチェインが"
                    + AddChainRuntime.FormatUnits(
                        chainBurn.AddChainGainUnits)
                    + "増加する。整数部分が追加連鎖回数になる。";
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

            if (skill is NeurotoxinSkillAsset neurotoxin
                && owner?.IsRevealed == true
                && owner.TryGetStat(
                    PachimonDisplayStat.Poison,
                    out var neurotoxinPoison)
                && owner.TryGetStat(
                    PachimonDisplayStat.Electric,
                    out var neurotoxinElectric))
            {
                var stunTicks = NeurotoxinMath.CalculateStunTicks(
                    neurotoxin,
                    neurotoxinPoison,
                    neurotoxinElectric);
                var toxinValue = NeurotoxinMath.CalculateToxinValue(
                    neurotoxin,
                    neurotoxinPoison);
                var poisonIcon =
                    AttributeRichText.GetIcon(AllocationType.Poison);
                var electricIcon =
                    AttributeRichText.GetIcon(AllocationType.Electric);
                return "敵の最後尾に"
                    + $"{stunTicks}tickのStun"
                    + $"（{poisonIcon}{neurotoxinPoison} / "
                    + $"{electricIcon}{neurotoxinElectric}参照）と、"
                    + $"Value {toxinValue}の毒素を付与する。";
            }

            if (skill is ToxinTransferSkillAsset toxinTransfer)
            {
                return "最も毒素が多い敵から"
                    + $"{toxinTransfer.RemovalPercent}%を取り除き、"
                    + "その対象を除く毒素が最も少ない敵へ"
                    + $"除去量の{toxinTransfer.ApplicationPercent}%を付与する。"
                    + "敵が1体の場合は同じ対象へ付与する。";
            }

            if (skill is ToxinExplosionSkillAsset toxinExplosion
                && owner?.IsRevealed == true
                && owner.TryGetStat(
                    PachimonDisplayStat.Poison,
                    out var explosionPoison)
                && owner.TryGetStat(
                    PachimonDisplayStat.Fire,
                    out var explosionFire))
            {
                var fixedDamage = SignedStatMath.FloorNonNegative(
                    ToxinExplosionMath.CalculateBaseDamage(
                        toxinExplosion,
                        consumedToxin: 0,
                        explosionPoison,
                        explosionFire));
                return "最も毒素が多い敵の毒素をすべて消費する。"
                    + $"消費Valueの{toxinExplosion.ToxinConversionPercent}%"
                    + $"と、現在Statによる{fixedDamage}を合計した"
                    + "Poisonダメージを敵全体へ与える（軽減前）。";
            }

            if (skill is PoisonShieldSkillAsset poisonShield
                && owner?.IsRevealed == true
                && owner.TryGetStat(
                    PachimonDisplayStat.Poison,
                    out var shieldPoison))
            {
                var shieldValue = PoisonShieldMath.CalculateShieldValue(
                    poisonShield,
                    shieldPoison);
                var reductionPercent =
                    PoisonShieldMath.CalculateToxinReductionPercent(
                        poisonShield,
                        shieldPoison);
                return $"自身に{shieldValue}のShieldを"
                    + $"{poisonShield.DurationTicks}tick付与する。"
                    + $"自身の毒素を{reductionPercent:0.##}%取り除く。";
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
                && owner.TryGetStat(PachimonDisplayStat.Wind, out var wind))
            {
                var icon = AttributeRichText.GetIcon(AllocationType.Wind);
                if (skill is FlyingAttackSkillAsset flyingAttack)
                {
                    var damage = SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                            flyingAttack.BaseWindDamage,
                            wind,
                            flyingAttack.WindDamageRatio));
                    var speed = SignedStatMath.FloorNonNegative(
                        wind * (flyingAttack.FlyingStatus?.WindSpeedRatio ?? 0)
                        / 100m);
                    return $"発生中は飛行して対象指定不可となり、Speed +{speed}。"
                        + $"発動時、敵の先頭へ{icon}{damage}のWind Damageを与える。";
                }

                if (skill is WindErosionSkillAsset erosion)
                {
                    var value = SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                            erosion.BaseErosionValue,
                            wind,
                            erosion.WindValueRatio));
                    return $"敵全体へValue {value}の風化を与える。"
                        + "風化はRBをValueだけ減少させ、毎tick1減少する。";
                }

                if (skill is HealingWindSkillAsset healingWind)
                {
                    int Scale(int baseValue) => SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                            baseValue,
                            wind,
                            healingWind.WindRatio));
                    return "HP割合が最も低い味方のHPを"
                        + $"{Scale(healingWind.BaseHealing)}回復し、"
                        + $"{healingWind.DurationTicks}tickの間Windを"
                        + $"{Scale(healingWind.BaseWindBonus)}、Speedを"
                        + $"{Scale(healingWind.BaseSpeedBonus)}増加させる。";
                }

                if (skill is SecondWindSkillAsset secondWind)
                {
                    var shield = SignedStatMath.FloorNonNegative(
                        wind * secondWind.WindShieldRatio / 100m);
                    return $"自身へ{shield}のShieldを付与し、"
                        + $"{secondWind.DurationTicks}tickの間、最終Windを0にする。";
                }
            }
            if (skill is InitialAttributeDamageSkillAsset initial
                && owner?.IsRevealed == true
                && AttributeRichText.TryGetDisplayStat(
                    skill.AllocationType,
                    out var initialDisplayStat)
                && owner.TryGetStat(initialDisplayStat, out var initialAttribute))
            {
                var context = new DescriptionTemplateContext()
                    .Set("damage", SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(
                            initial.BaseDamage,
                            initialAttribute,
                            initial.DamageRatio), 1))
                    .Set("baseDamage", initial.BaseDamage)
                    .Set("attribute", initialAttribute)
                    .Set("ratio", initial.DamageRatio);
                if (initial is ElectricShockSkillAsset electricInitial)
                {
                    var ice = owner.TryGetStat(PachimonDisplayStat.Ice, out var value)
                        ? value
                        : 0;
                    context.Set("statusValue", checked(
                        SignedStatMath.FloorNonNegative(SignedStatMath.ScaleFromBase(
                            electricInitial.ElectricParalysisBaseValue,
                            initialAttribute,
                            electricInitial.ElectricParalysisRatio))
                        + SignedStatMath.FloorNonNegative(SignedStatMath.ScaleFromBase(
                            electricInitial.IceParalysisBaseValue,
                            ice,
                            electricInitial.IceParalysisRatio))));
                }
                else if (initial is PoisonNeedleSkillAsset poison)
                {
                    context.Set("statusValue", SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(poison.ToxinBaseValue,
                            initialAttribute, poison.ToxinRatio)));
                }
                else if (initial is ColdHandSkillAsset cold)
                {
                    context.Set("statusValue", SignedStatMath.FloorNonNegative(
                        SignedStatMath.ScaleFromBase(cold.ChillBaseValue,
                            initialAttribute, cold.ChillRatio)));
                }
                return DescriptionTemplateFormatter.Format(skill.Description, context);
            }

            if (skill is PlaceholderSkillAsset placeholder
                && owner?.IsRevealed == true
                && AttributeRichText.TryGetDisplayStat(
                    skill.AllocationType,
                    out var displayStat)
                && owner.TryGetStat(displayStat, out var attributeValue))
            {
                var context = new DescriptionTemplateContext()
                    .Set("damage", SignedStatMath.FloorNonNegative(
                        placeholder.BaseDamage
                        * SignedStatMath.AmplificationMultiplier(attributeValue),
                        1))
                    .Set("baseDamage", placeholder.BaseDamage)
                    .Set("attribute", attributeValue)
                    .Set("ratio", 100);
                if (placeholder.StatusBaseValue > 0)
                {
                    context.Set(
                        "statusValue",
                        SignedStatMath.FloorNonNegative(
                            SignedStatMath.ScaleFromBase(
                                placeholder.StatusBaseValue,
                                attributeValue,
                                placeholder.StatusScalingPercent),
                            1));
                }

                return DescriptionTemplateFormatter.Format(
                    skill.Description,
                    context);
            }

            return string.IsNullOrWhiteSpace(skill.Description)
                ? "説明未設定"
                : skill.Description;
        }
    }
}
