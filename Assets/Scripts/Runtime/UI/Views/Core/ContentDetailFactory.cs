using System;
using System.Globalization;
using System.Linq;
using Pachimon.Battle;
using Pachimon.Data;
using Pachimon.Items;
using Pachimon.Passives;
using Pachimon.Reward;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.UI
{
    public static class WeatherDetailDescriptionFormatter
    {
        public static string Format(BattleWeatherInstance weather)
        {
            if (weather == null) throw new ArgumentNullException(nameof(weather));
            if (weather.Definition is RainWeatherAsset precipitation)
            {
                return FormatPrecipitation(weather, precipitation);
            }
            if (weather.Definition is WindWeatherAsset wind)
            {
                return FormatWind(weather.Value, wind);
            }
            if (weather.Definition is ThunderWeatherAsset thunder)
            {
                return FormatThunder(weather.Value, thunder);
            }
            return Format(weather.Value, weather.Definition);
        }

        public static string Format(int value, BattleWeatherAsset definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            if (definition is SunnyWeatherAsset temperature)
            {
                return FormatTemperature(value, temperature);
            }

            if (definition is PairedAttributeEnvironmentAsset paired)
            {
                return FormatPairedEnvironment(value, paired);
            }

            return DescriptionTemplateFormatter.Format(definition.Description);
        }

        private static string FormatPrecipitation(
            BattleWeatherInstance weather,
            RainWeatherAsset definition)
        {
            if (weather.Value < 0)
            {
                return FormatSunny(Math.Abs((decimal)weather.Value), definition);
            }

            var runtime = weather.Runtime;
            var effectiveValue = Math.Abs(runtime.GetEffectiveRainValue());
            var rainEffectMultiplier = weather.Value == 0
                ? 1m
                : effectiveValue / Math.Abs((decimal)weather.Value);
            var environmentChange = effectiveValue
                * definition.EnvironmentChangePercent / 100m;
            var context = new DescriptionTemplateContext()
                .Set("environmentInterval", definition.EnvironmentIntervalTicks)
                .Set("moistureChange", FormatNumber(environmentChange));

            if (weather.IsSnow)
            {
                var chillReference = Math.Abs((decimal)runtime.Temperature)
                    * definition.SnowChillTemperatureRatio / 100m
                    * rainEffectMultiplier;
                var chillValue = SignedStatMath.FloorNonNegative(
                    definition.SnowChillBaseValue
                    * SignedStatMath.AmplificationMultiplier(chillReference));
                SetPercentageValues(
                    context,
                    effectiveValue * definition.SnowIceRatioScalingPercent / 100m,
                    effectiveValue * definition.SnowFireRatioScalingPercent / 100m);
                context.Set("chillValue", chillValue);
                return DescriptionTemplateFormatter.Format(
                    definition.SnowDescription,
                    context);
            }

            SetPercentageValues(
                context,
                effectiveValue * definition.AquaRatioScalingPercent / 100m,
                effectiveValue * definition.FireRatioScalingPercent / 100m);
            context.Set(
                "leakPerTick",
                FormatNumber(effectiveValue
                    * definition.LeakValueRatioPerTick / 10000m));
            return DescriptionTemplateFormatter.Format(
                definition.Description,
                context);
        }

        private static string FormatSunny(
            decimal magnitude,
            RainWeatherAsset definition)
        {
            var context = new DescriptionTemplateContext()
                .Set("environmentInterval", definition.EnvironmentIntervalTicks)
                .Set("environmentChange", FormatNumber(
                    magnitude * definition.EnvironmentChangePercent / 100m));
            SetPercentageValues(
                context,
                magnitude * definition.SunnyFireRatioScalingPercent / 100m,
                magnitude * definition.SunnyAquaRatioScalingPercent / 100m);
            return DescriptionTemplateFormatter.Format(
                definition.SunnyDescription,
                context);
        }

        private static string FormatWind(int value, WindWeatherAsset definition)
        {
            var magnitude = Math.Abs((decimal)value);
            var context = new DescriptionTemplateContext()
                .Set("speedRatio", definition.SpeedFromWindRatio)
                .Set("rainIncreasePercent", FormatPercent(
                    (SignedStatMath.AmplificationMultiplier(
                         magnitude
                         * definition.RainEffectRatioScalingPercent / 100m)
                     - 1m) * 100m))
                .Set("damageChangePercent", definition.DamageChangePercent);
            SetPercentageValues(
                context,
                magnitude * definition.WindRatioScalingPercent / 100m,
                0m);
            return DescriptionTemplateFormatter.Format(
                definition.Description,
                context);
        }

        private static string FormatThunder(
            int value,
            ThunderWeatherAsset definition)
        {
            var magnitude = Math.Abs((decimal)value);
            var context = new DescriptionTemplateContext()
                .Set("speedRatio", definition.SpeedFromElectricRatio)
                .Set("attackInterval", definition.AttackIntervalTicks)
                .Set("currentDamage", Math.Abs(value) / definition.DamageDivisor);
            SetPercentageValues(
                context,
                magnitude * definition.ElectricRatioScalingPercent / 100m,
                0m);
            return DescriptionTemplateFormatter.Format(
                definition.Description,
                context);
        }

        private static string FormatTemperature(
            int value,
            SunnyWeatherAsset definition)
        {
            var magnitude = Math.Abs((decimal)value);
            var positive = value >= 0;
            var amplificationInput = magnitude * (positive
                ? definition.FireRatioScalingPercent
                : definition.ColdIceRatioScalingPercent) / 100m;
            var reductionInput = magnitude * (positive
                ? definition.IceRatioScalingPercent
                : definition.ColdFireRatioScalingPercent) / 100m;
            var context = CreatePercentageContext(
                amplificationInput,
                reductionInput);
            var template = positive
                ? definition.Description
                : definition.NegativeDescription;
            return DescriptionTemplateFormatter.Format(template, context);
        }

        private static string FormatPairedEnvironment(
            int value,
            PairedAttributeEnvironmentAsset definition)
        {
            var magnitude = Math.Abs((decimal)value);
            var positive = value >= 0;
            var amplificationInput = magnitude * (positive
                ? definition.PositiveAmplificationPercent
                : definition.NegativeAmplificationPercent) / 100m;
            var reductionInput = magnitude * (positive
                ? definition.PositiveReductionPercent
                : definition.NegativeReductionPercent) / 100m;
            var amplifiedAttribute = positive
                ? definition.PositiveAttribute
                : definition.NegativeAttribute;
            var reducedAttribute = positive
                ? definition.NegativeAttribute
                : definition.PositiveAttribute;
            var context = CreatePercentageContext(
                    amplificationInput,
                    reductionInput)
                .Set("increaseIcon", GetAttributeIcon(amplifiedAttribute))
                .Set("decreaseIcon", GetAttributeIcon(reducedAttribute));
            var template = positive
                ? definition.Description
                : definition.NegativeDescription;
            return DescriptionTemplateFormatter.Format(template, context);
        }

        private static DescriptionTemplateContext CreatePercentageContext(
            decimal amplificationInput,
            decimal reductionInput)
        {
            return SetPercentageValues(
                new DescriptionTemplateContext(),
                amplificationInput,
                reductionInput);
        }

        private static DescriptionTemplateContext SetPercentageValues(
            DescriptionTemplateContext context,
            decimal amplificationInput,
            decimal reductionInput)
        {
            return context
                .Set("increasePercent", FormatPercent(
                    (SignedStatMath.AmplificationMultiplier(amplificationInput)
                     - 1m) * 100m))
                .Set("decreasePercent", FormatPercent(
                    (1m - SignedStatMath.ReductionMultiplier(reductionInput))
                    * 100m));
        }

        private static string GetAttributeIcon(PachimonAttribute attribute)
        {
            return AttributeRichText.GetIcon(
                (AllocationType)((int)attribute + 1));
        }

        private static string FormatPercent(decimal value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string FormatNumber(decimal value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }

    public sealed class ContentDetailFactory
    {
        private SkillCatalog _skillCatalog;
        private PassiveCatalog _passiveCatalog;

        public void Configure(
            SkillCatalog skillCatalog,
            PassiveCatalog passiveCatalog)
        {
            _skillCatalog = skillCatalog;
            _passiveCatalog = passiveCatalog;
        }

        public ContentDetailOverlayContent CreateStatus(BattleStatusInstance status)
        {
            var definition = status.Definition;
            var descriptionTemplate = status.Description;
            var description = string.IsNullOrWhiteSpace(descriptionTemplate)
                ? "説明未設定。"
                : descriptionTemplate;
            if (definition != null
                && descriptionTemplate.Contains(
                    "{",
                    System.StringComparison.Ordinal)
                && StatusDescriptionValueProviderRegistry.TryCreateContext(
                    status,
                    out var context))
            {
                description = DescriptionTemplateFormatter.Format(
                    descriptionTemplate,
                    context);
            }

            var remaining = status.RemainingTicks.HasValue
                ? $"残り  {status.RemainingTicks.Value}tick"
                : "Battle中継続";
            var source = status.Source?.DisplayName ?? "状態効果";
            return new ContentDetailOverlayContent(
                ContentDetailKind.Status,
                status.DisplayName,
                $"Value  {status.Value}    Stack  {status.StackCount}"
                + $"    {remaining}    付与者  {source}",
                DescriptionTemplateFormatter.Format(description),
                GameUiPalette.StatusChip);
        }

        public ContentDetailOverlayContent CreateSkill(
            PachimonAbilityPreview ability,
            PachimonPreviewContent owner)
        {
            var skill = _skillCatalog?.Get(ability.Id);
            if (skill == null)
            {
                return new ContentDetailOverlayContent(
                    ContentDetailKind.Skill,
                    ability.DisplayName,
                    $"ID  {ability.Id}",
                    "詳細データが見つかりません。",
                    GameUiPalette.SkillChip);
            }

            return new ContentDetailOverlayContent(
                ContentDetailKind.Skill,
                ability.DisplayName,
                SkillDisplayTextFormatter.FormatTiming(
                    skill,
                    ability.UpgradeLevel),
                DescriptionTemplateFormatter.Format(
                    SkillDetailDescriptionFormatter.Format(skill, owner)),
                GameUiPalette.SkillChip);
        }

        public ContentDetailOverlayContent CreatePassive(
            PachimonAbilityPreview ability,
            PachimonPreviewContent owner)
        {
            var passive = _passiveCatalog?.Get(ability.Id);
            var description = passive == null
                || string.IsNullOrWhiteSpace(passive.Description)
                    ? "説明未設定。"
                    : passive.Description;
            if (passive?.Description?.Contains("{", System.StringComparison.Ordinal)
                    == true
                && PassiveDescriptionValueProviderRegistry.TryCreateContext(
                    passive,
                    owner,
                    out var templateContext))
            {
                description = DescriptionTemplateFormatter.Format(
                    passive.Description,
                    templateContext);
            }
            else if (passive is DerivedAdditivePassiveAsset statDefinition)
            {
                description = CreateDerivedPassiveDescription(
                    statDefinition,
                    owner?.StatCalculation);
            }
            else if (passive is FieldValueAmplificationPassiveAsset fieldDefinition)
            {
                var currentMultiplier = owner != null
                    && owner.TryGetStat(PachimonDisplayStat.Poison, out var poison)
                        ? SignedStatMath.AmplificationMultiplier(
                            poison
                            * fieldDefinition.PoisonScalingPercent
                            / 100m)
                        : (decimal?)null;
                var currentText = currentMultiplier.HasValue
                    ? $"現在の増幅率は{currentMultiplier.Value:0.##}倍。"
                    : string.Empty;
                description = "自身が生成物を生成するとき、"
                    + "生成予定ValueをPoisonに応じて増幅する。"
                    + currentText;
            }
            else if (passive is ToxinGrowthPassiveAsset toxinGrowthDefinition)
            {
                description = "自身が毒素を付与するたび、Battle中のPoisonが"
                    + $"{toxinGrowthDefinition.PoisonPercentPerApplication}%増加する。"
                    + "複数回発動した増加率は加算してから適用する。";
            }
            else if (passive is PoisonKnightPassiveAsset poisonKnightDefinition)
            {
                decimal? currentSharePercent = null;
                if (owner != null
                    && owner.TryGetStat(PachimonDisplayStat.Poison, out var poison))
                {
                    currentSharePercent = SignedStatMath.ScaleFromBase(
                        poisonKnightDefinition.BaseSharePercent,
                        poison,
                        poisonKnightDefinition.PoisonScalingPercent);
                }

                var currentText = currentSharePercent.HasValue
                    ? $"現在の共有率は{currentSharePercent.Value:0.##}%。"
                    : string.Empty;
                description = "自身が受けたShieldと実際のHP回復量の一部を、"
                    + "生存中の他の味方全員にも与える。"
                    + currentText;
            }
            else if (passive is FireGrowthOnDamagePassiveAsset fireGrowthDefinition)
            {
                description = "Damageを受けるたび、Battle中のFireが"
                    + $"{fireGrowthDefinition.FireIncreasePerDamage}増加する。"
                    + "HPとShieldのどちらへ適用されたDamageでも発動する。";
            }
            else if (passive is DarkFlamePassiveAsset darkFlameDefinition)
            {
                decimal? currentConversionPercent = null;
                if (owner != null
                    && owner.TryGetStat(PachimonDisplayStat.Poison, out var poison))
                {
                    currentConversionPercent =
                        darkFlameDefinition.BaseConversionPercent
                        * SignedStatMath.AmplificationMultiplier(
                            poison
                            * darkFlameDefinition.PoisonScalingPercent
                            / 100m);
                }

                var currentText = currentConversionPercent.HasValue
                    ? $"現在の変換率は{currentConversionPercent.Value:0.##}%。"
                    : string.Empty;
                description = "Fire Damageを与えたとき、その軽減前Valueを基に"
                    + "同じ対象へ追加Poison Damageを与える。"
                    + currentText;
            }
            else if (passive is FireArcherPassiveAsset fireArcherDefinition)
            {
                decimal? currentMissingHpPercent = null;
                if (owner != null
                    && owner.TryGetStat(PachimonDisplayStat.Fire, out var fire))
                {
                    currentMissingHpPercent =
                        fireArcherDefinition.MissingHpPercent
                        * SignedStatMath.AmplificationMultiplier(
                            fire
                            * fireArcherDefinition.FireScalingPercent
                            / 100m);
                }

                var currentText = currentMissingHpPercent.HasValue
                    ? "現在は対象の減少HPの"
                      + $"{currentMissingHpPercent.Value:0.##}%をBaseDamageにする。"
                    : string.Empty;
                description = "Skill Damageを与えたとき、対象の減少HPとFireに"
                    + "応じた追加Fire Damageを同じ対象へ与える。"
                    + currentText;
            }
            else if (passive is ComboMasterPassiveAsset comboMasterDefinition)
            {
                description = "Battle中に完了した最大追加連鎖回数1回につき、"
                    + "DamageBonusが"
                    + $"{comboMasterDefinition.DamageBonusPerChain}増加する。";
            }
            else if (PassiveLogicRegistry.TryGetPlaceholderAttribute(
                    ability.Id,
                    _passiveCatalog,
                    out var attribute))
            {
                var attributeLabel = GetAttributeLabel(attribute);
                var allocationType = (AllocationType)((int)attribute + 1);
                var icon = AttributeRichText.GetIcon(allocationType);
                description =
                    $"与える{icon}{attributeLabel}ダメージが"
                    + $"{OutgoingAttributeDamagePassiveLogic.DefaultDamagePercent - 100}%増加する。";
            }

            return new ContentDetailOverlayContent(
                ContentDetailKind.Passive,
                ability.DisplayName,
                string.Empty,
                DescriptionTemplateFormatter.Format(description),
                GameUiPalette.PassiveChip);
        }

        public ContentDetailOverlayContent CreateItem(
            ItemAsset item,
            GeneratedItemData generatedData = null)
        {
            var category = item.Category switch
            {
                ItemCategory.Pharmacy => "薬局",
                ItemCategory.Other => "その他",
                ItemCategory.SkillMachine => "技マシーン",
                ItemCategory.Engraving => "刻印屋",
                ItemCategory.Equipment => "装備品",
                _ => "未分類",
            };
            var description = item.Description;
            if (item is HealingItemAsset healingItem)
            {
                var recoveryAmount = generatedData?.PrimaryEffectValue
                    ?? healingItem.RecoveryAmount;
                if (healingItem.DefeatedOnly)
                {
                    description =
                        $"戦闘不能の味方パチモンを{recoveryAmount}のHPで復活させる。";
                }
                else
                {
                    var resource = healingItem.ResourceType == RecoveryResourceType.Hp
                        ? "最大HP"
                        : "最大MN";
                    description = $"{resource}を{recoveryAmount}回復する。";
                }
            }
            else if (item is SkillMachineItemAsset machine
                     && machine.Skill != null)
            {
                description = SkillDisplayTextFormatter.FormatTiming(machine.Skill)
                    + "\n\n"
                    + SkillDisplayTextFormatter.FormatBaseDescription(machine.Skill);
            }
            else if (item is EngravingItemAsset
                     && generatedData?.StatChanges.Count == 2)
            {
                var main = generatedData.StatChanges.First(change => change.Amount > 0);
                var downside = generatedData.StatChanges.First(change => change.Amount < 0);
                description = $"対象の{EngravingStatName.Get(main.StatType)}を"
                    + $"{main.Amount}増加させる。\n"
                    + $"副作用：{EngravingStatName.Get(downside.StatType)} "
                    + $"{downside.Amount}";
            }
            else if (item is EquipmentItemAsset equipment
                     && generatedData?.StatChanges.Count >= 2)
            {
                var slot = equipment.Slot switch
                {
                    EquipmentSlot.Head => "頭 / 冠",
                    EquipmentSlot.Body => "胴 / 勾玉",
                    EquipmentSlot.Feet => "足 / 靴",
                    _ => equipment.Slot.ToString(),
                };
                var effects = generatedData.StatChanges.Select(change =>
                {
                    var amount = $"{(change.Amount >= 0 ? "+" : string.Empty)}{change.Amount}";
                    return PachimonSubStatBindings.IsSubStat(change.StatType)
                        ? $"{EngravingStatName.Get(change.StatType)}対応率 {amount}%"
                        : $"{EngravingStatName.Get(change.StatType)} {amount}";
                });
                description = $"装備部位：{slot}\n"
                    + string.Join("\n", effects)
                    + "\n\n購入時に選択したパチモンへ装着する。";
            }

            return new ContentDetailOverlayContent(
                ContentDetailKind.Item,
                ItemDisplayNameFormatter.Format(item, generatedData),
                $"カテゴリ  {category}    基準価格  {item.BasePrice} Gold",
                description,
                GameUiPalette.ItemChip);
        }

        public ContentDetailOverlayContent CreateFieldEffect(
            BattleFieldEffectInstance effect)
        {
            var side = effect.EffectId == BattleFieldEffectId.FrozenGround
                ? "全体生成物"
                : effect.TargetSide == BattleSide.Player
                    ? "自陣生成物"
                    : "敵陣生成物";
            var description = !string.IsNullOrWhiteSpace(effect.Description)
                ? effect.Description
                : effect.EffectId switch
            {
                BattleFieldEffectId.Smog =>
                    "毎tick、現在Valueの1%を対象陣営の生存パチモン全員へ"
                    + "毒素として付与する。\n"
                    + "毎tick、現在Valueの1%ずつ減衰する。",
                _ => "説明未設定",
            };
            var runtimeValues = effect.EffectId == BattleFieldEffectId.FireBarrier
                ? $"Value  {effect.Value}    毎tick -1"
                : effect.EffectId == BattleFieldEffectId.PoisonMist
                    ? $"Value  {effect.Value}    最小Value  {effect.SecondaryValue}"
                        + $"    残り  {effect.RemainingTicks}tick"
                : effect.Definition is FrozenGroundFieldEffectAsset frozenGround
                    ? $"Value  {effect.Value}    冷気持続 x"
                        + $"{1m / frozenGround.CalculateChillDecayMultiplier(effect.Value):0.##}"
                : $"Value  {effect.Value}";
            if (effect.Statuses.Count > 0)
            {
                runtimeValues += "\n状態  " + string.Join(
                    " / ",
                    effect.Statuses.Select(status => status.DisplayName));
            }
            return new ContentDetailOverlayContent(
                ContentDetailKind.FieldEffect,
                effect.DisplayName,
                $"{side}    {runtimeValues}    生成者  {effect.Source.DisplayName}",
                description,
                BattleFieldInfoView.GetAccentColor(effect.EffectId));
        }

        public ContentDetailOverlayContent CreateWeather(
            BattleWeatherInstance weather)
        {
            var runtimeValues = weather.WeatherId == BattleWeatherId.Temperature
                ? $"気温  {weather.Value:+#;-#;0}"
                : $"Value  {System.Math.Abs(weather.Value)}";
            return new ContentDetailOverlayContent(
                ContentDetailKind.FieldEffect,
                weather.DisplayName,
                $"全体環境    {runtimeValues}    最終変更者  {weather.Source.DisplayName}",
                string.IsNullOrWhiteSpace(weather.Description)
                    ? "説明未設定"
                    : WeatherDetailDescriptionFormatter.Format(weather),
                BattleFieldInfoView.GetWeatherAccentColor(
                    weather.WeatherId,
                    weather.IsSnow ? -weather.Value : weather.Value));
        }

        private static string CreateDerivedPassiveDescription(
            DerivedAdditivePassiveAsset definition,
            StatCalculationResult calculation)
        {
            var referenceLabel = GetStatLabel(definition.ReferenceStat);
            var targetLabel = GetStatLabel(definition.TargetStat);
            var contribution = calculation?
                .GetContributions(definition.TargetStat)
                .FirstOrDefault(item =>
                    item.Source.SourceId == $"passive:{definition.PassiveId}")?
                .Value;
            var actualValue = contribution.HasValue
                ? $"現在の加算値は{contribution.Value:0.##}。"
                : string.Empty;
            return $"{referenceLabel}の{definition.Percent}%を"
                + $"{targetLabel}へ加算する。{actualValue}";
        }

        private static string GetStatLabel(PachimonStatType statType)
        {
            if (PachimonStatTypeUtility.TryGetAttribute(statType, out var attribute))
            {
                var allocationType = (AllocationType)((int)attribute + 1);
                return AttributeRichText.GetIcon(allocationType)
                    + GetAttributeLabel(attribute);
            }

            return statType.ToString();
        }

        private static string GetAllocationTypeLabel(AllocationType type)
        {
            return type switch
            {
                AllocationType.Fire => "炎",
                AllocationType.Aqua => "水",
                AllocationType.Leaf => "草",
                AllocationType.Electric => "電気",
                AllocationType.Poison => "毒",
                AllocationType.Ice => "氷",
                AllocationType.Wind => "風",
                AllocationType.Dragon => "竜",
                _ => "なし",
            };
        }

        private static string GetAttributeLabel(PachimonAttribute attribute)
        {
            return GetAllocationTypeLabel(
                (AllocationType)((int)attribute + 1));
        }
    }
}
