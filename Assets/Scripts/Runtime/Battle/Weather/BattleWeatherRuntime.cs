using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Reward;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public sealed class BattleWeatherInstance
    {
        internal BattleWeatherInstance(
            BattleWeatherRuntime runtime,
            BattleWeatherAsset definition,
            BattleUnitState source,
            int value)
        {
            Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            if (BattleWeatherRuntime.IsSignedAxis(definition.WeatherId)
                ? value == 0
                : value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            Value = value;
        }

        internal BattleWeatherRuntime Runtime { get; }
        public BattleWeatherAsset Definition { get; }
        public BattleWeatherId WeatherId => Definition.WeatherId;
        public BattleUnitState Source { get; private set; }
        public int Value { get; private set; }
        public decimal DecayWork { get; private set; }
        public decimal LeakAccumulationWork { get; private set; }
        public int ApplicationWork { get; private set; }
        public bool IsSnow { get; private set; }
        public string DisplayName
        {
            get
            {
                if (Definition is RainWeatherAsset precipitation)
                {
                    if (Value < 0) return precipitation.SunnyDisplayName;
                    if (IsSnow) return precipitation.SnowDisplayName;
                }
                if (Value < 0
                    && Definition is PairedAttributeEnvironmentAsset paired)
                {
                    return paired.NegativeDisplayName;
                }
                return Definition.DisplayName;
            }
        }
        public string Description => Definition.Description;

        internal void AddValue(BattleUnitState source, int value)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = checked(Value + value);
        }

        internal void AddSignedValue(BattleUnitState source, int value)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            if (value == 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = checked(Value + value);
            DecayWork = 0m;
        }

        internal void Advance(decimal decay)
        {
            DecayWork += decay;
            var amount = Math.Min(
                Math.Abs(Value),
                SignedStatMath.FloorNonNegative(DecayWork));
            DecayWork -= amount;
            Value += Value > 0 ? -amount : amount;
        }

        internal void SetSnowPresentation(bool isSnow)
        {
            IsSnow = isSnow;
        }

        internal int AccumulateLeak(decimal value)
        {
            if (value < 0m) throw new ArgumentOutOfRangeException(nameof(value));
            LeakAccumulationWork += value;
            var applied = SignedStatMath.FloorNonNegative(LeakAccumulationWork);
            LeakAccumulationWork -= applied;
            return applied;
        }

        internal void ResetLeakAccumulationWork()
        {
            LeakAccumulationWork = 0m;
        }

        internal bool AdvanceApplication(int intervalTicks)
        {
            if (intervalTicks <= 0)
                throw new ArgumentOutOfRangeException(nameof(intervalTicks));
            ApplicationWork++;
            if (ApplicationWork < intervalTicks) return false;
            ApplicationWork -= intervalTicks;
            return true;
        }

        internal BattleWeatherInstance CreateSimulationClone(
            BattleWeatherRuntime runtime,
            BattleUnitState sourceClone)
        {
            return new BattleWeatherInstance(runtime, Definition, sourceClone, Value)
            {
                DecayWork = DecayWork,
                LeakAccumulationWork = LeakAccumulationWork,
                ApplicationWork = ApplicationWork,
                IsSnow = IsSnow,
            };
        }
    }

    public sealed class BattleWeatherRuntime
    {
        public const decimal DamageDrivenGrowthK = 25m;

        private readonly BattleState _state;
        private readonly BattleEnvironmentDefinitions _definitions;
        private readonly List<BattleWeatherInstance> _weather = new();
        private decimal _temperatureDamageCarry;
        private decimal _moistureDamageCarry;
        private decimal _plasmaDamageCarry;
        private decimal _windDamageCarry;
        private decimal _precipitationMoistureCarry;
        private decimal _sunnyTemperatureCarry;
        private decimal _sunnyMoistureCarry;

        public BattleWeatherRuntime(
            BattleState state,
            BattleEnvironmentDefinitions definitions = null)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _definitions = definitions;
        }

        public BattleEnvironmentDefinitions Definitions => _definitions;
        public IReadOnlyList<BattleWeatherInstance> Weather => _weather;
        public int ActiveWeatherTypeCount => _weather.Count(item =>
            IsSignedAxis(item.WeatherId)
                ? item.Value != 0
                : item.Value > 0);
        private BattleWeatherInstance Rain => _weather.FirstOrDefault(item =>
            item.WeatherId == BattleWeatherId.Rain);
        private BattleWeatherInstance Wind => _weather.FirstOrDefault(item =>
            item.WeatherId == BattleWeatherId.Wind);
        private BattleWeatherInstance Thunder => _weather.FirstOrDefault(item =>
            item.WeatherId == BattleWeatherId.Thunder);
        public int Temperature => _weather.FirstOrDefault(item =>
            item.WeatherId == BattleWeatherId.Temperature)?.Value ?? 0;
        public int Moisture => _weather.FirstOrDefault(item =>
            item.WeatherId == BattleWeatherId.Moisture)?.Value ?? 0;
        public int Plasma => _weather.FirstOrDefault(item =>
            item.WeatherId == BattleWeatherId.Plasma)?.Value ?? 0;
        public bool IsSnowing => Temperature < 0 && (Rain?.Value ?? 0) > 0;
        public bool IsRaining => Temperature >= 0 && (Rain?.Value ?? 0) > 0;
        public bool IsSunny => (Rain?.Value ?? 0) < 0;

        internal static bool IsSignedAxis(BattleWeatherId weatherId)
        {
            return weatherId == BattleWeatherId.Temperature
                || weatherId == BattleWeatherId.Rain
                || weatherId == BattleWeatherId.Moisture
                || weatherId == BattleWeatherId.Plasma;
        }

        public bool Has(BattleWeatherId weatherId)
        {
            return _weather.Any(item =>
                item.WeatherId == weatherId
                && (IsSignedAxis(weatherId)
                    ? item.Value != 0
                    : item.Value > 0));
        }

        public BattleWeatherInstance Get(BattleWeatherId weatherId)
        {
            return _weather.FirstOrDefault(item =>
                item.WeatherId == weatherId
                && (IsSignedAxis(weatherId)
                    ? item.Value != 0
                    : item.Value > 0));
        }

        public BattleWeatherInstance CreateOrAdd(
            BattleUnitState source,
            BattleWeatherAsset definition,
            int value)
        {
            ValidateSource(source);
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (definition.WeatherId == BattleWeatherId.Temperature
                || definition.WeatherId == BattleWeatherId.Moisture
                || definition.WeatherId == BattleWeatherId.Plasma)
            {
                throw new ArgumentException(
                    "Use a signed environment method for this axis.",
                    nameof(definition));
            }
            value = ApplyGenerationPower(source, value);
            var existing = _weather.FirstOrDefault(item =>
                item.WeatherId == definition.WeatherId);
            if (existing != null)
            {
                if (!ReferenceEquals(existing.Definition, definition))
                {
                    throw new InvalidOperationException(
                        "A Weather recast must use the same Definition.");
                }
                existing.AddValue(source, value);
                if (definition.WeatherId == BattleWeatherId.Rain)
                {
                    RefreshDerivedStatuses();
                }
                NotifyContextChanged();
                return existing;
            }

            var created = new BattleWeatherInstance(this, definition, source, value);
            _weather.Add(created);
            if (definition.WeatherId == BattleWeatherId.Rain)
            {
                RefreshDerivedStatuses();
            }
            NotifyContextChanged();
            return created;
        }

        public int AddTemperature(
            BattleUnitState source,
            SunnyWeatherAsset definition,
            int amount)
        {
            ValidateSource(source);
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition.WeatherId != BattleWeatherId.Temperature)
            {
                throw new ArgumentException(
                    "Temperature Definition must use Temperature ID.",
                    nameof(definition));
            }
            if (amount == 0) return Temperature;
            amount = ApplyGenerationPowerToSignedValue(source, amount);

            return AddSignedRaw(source, definition, amount);
        }

        public int AddPrecipitation(
            BattleUnitState source,
            RainWeatherAsset definition,
            int amount)
        {
            ValidateSource(source);
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition.WeatherId != BattleWeatherId.Rain)
                throw new ArgumentException("Precipitation Definition must use Rain ID.", nameof(definition));
            if (amount == 0) return Rain?.Value ?? 0;
            amount = ApplyGenerationPowerToSignedValue(source, amount);
            return AddSignedRaw(source, definition, amount);
        }

        private int AddSignedRaw(
            BattleUnitState source,
            BattleWeatherAsset definition,
            int amount)
        {
            ValidateSource(source);
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!IsSignedAxis(definition.WeatherId))
                throw new ArgumentException("Definition is not a signed environment axis.", nameof(definition));
            if (amount == 0) return GetAxisValue(definition.WeatherId);

            var wasRaining = IsRaining;
            var wasSnowing = IsSnowing;
            var existing = _weather.FirstOrDefault(item =>
                item.WeatherId == definition.WeatherId);
            if (existing == null)
            {
                _weather.Add(new BattleWeatherInstance(this, definition, source, amount));
            }
            else
            {
                if (!ReferenceEquals(existing.Definition, definition))
                    throw new InvalidOperationException("Environment changes must use the same Definition.");
                existing.AddSignedValue(source, amount);
                if (existing.Value == 0) _weather.Remove(existing);
            }

            if (definition.WeatherId == BattleWeatherId.Temperature
                || definition.WeatherId == BattleWeatherId.Rain
                || wasRaining != IsRaining
                || wasSnowing != IsSnowing)
            {
                RefreshDerivedStatuses();
            }
            NotifyContextChanged();
            return GetAxisValue(definition.WeatherId);
        }

        private int GetAxisValue(BattleWeatherId weatherId)
        {
            return _weather.FirstOrDefault(item => item.WeatherId == weatherId)?.Value ?? 0;
        }

        private BattleWeatherInstance AddPositiveRaw(
            BattleUnitState source,
            BattleWeatherAsset definition,
            int amount)
        {
            if (amount <= 0) return Get(definition.WeatherId);
            ValidateSource(source);
            var existing = _weather.FirstOrDefault(item => item.WeatherId == definition.WeatherId);
            if (existing != null)
            {
                existing.AddValue(source, amount);
                NotifyContextChanged();
                return existing;
            }
            var created = new BattleWeatherInstance(this, definition, source, amount);
            _weather.Add(created);
            NotifyContextChanged();
            return created;
        }

        private static int ApplyGenerationPower(
            BattleUnitState source,
            int value)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            var attribute = source.SubStatBindings.GetAttribute(
                PachimonStatType.GenerationPower);
            var attributeValue = source.GetBattleStatValue(attribute);
            return Math.Max(
                1,
                SignedStatMath.FloorNonNegative(
                    SignedStatMath.ReplacePreAppliedAmplification(
                        value,
                        attributeValue,
                        source.GetBattleStatValue(
                            PachimonStatType.GenerationPower))));
        }

        private static int ApplyGenerationPowerToSignedValue(
            BattleUnitState source,
            int value)
        {
            if (value == 0) return 0;
            var magnitude = ApplyGenerationPower(source, Math.Abs(value));
            return value > 0 ? magnitude : -magnitude;
        }

        public decimal GetAttributeRatioMultiplier(PachimonAttribute attribute)
        {
            var multiplier = 1m;
            foreach (var weather in _weather.Where(item =>
                         item.WeatherId == BattleWeatherId.Temperature))
            {
                var temperature = weather.Definition as SunnyWeatherAsset;
                if (temperature == null) continue;
                if (weather.Value > 0)
                {
                    multiplier *= attribute switch
                    {
                        PachimonAttribute.Fire =>
                            SignedStatMath.AmplificationMultiplier(
                                weather.Value
                                * temperature.FireRatioScalingPercent / 100m),
                        PachimonAttribute.Ice =>
                            SignedStatMath.ReductionMultiplier(
                                weather.Value
                                * temperature.IceRatioScalingPercent / 100m),
                        _ => 1m,
                    };
                    continue;
                }

                var cold = Math.Abs((decimal)weather.Value);
                multiplier *= attribute switch
                {
                    PachimonAttribute.Fire =>
                        SignedStatMath.ReductionMultiplier(
                            cold
                            * temperature.ColdFireRatioScalingPercent / 100m),
                    PachimonAttribute.Ice =>
                        SignedStatMath.AmplificationMultiplier(
                            cold
                            * temperature.ColdIceRatioScalingPercent / 100m),
                    _ => 1m,
                };
            }

            foreach (var axis in _weather.Where(item =>
                         item.Definition is PairedAttributeEnvironmentAsset))
            {
                var definition = (PairedAttributeEnvironmentAsset)axis.Definition;
                var magnitude = Math.Abs((decimal)axis.Value);
                if (axis.Value > 0)
                {
                    if (attribute == definition.PositiveAttribute)
                    {
                        multiplier *= SignedStatMath.AmplificationMultiplier(
                            magnitude * definition.PositiveAmplificationPercent / 100m);
                    }
                    else if (attribute == definition.NegativeAttribute)
                    {
                        multiplier *= SignedStatMath.ReductionMultiplier(
                            magnitude * definition.PositiveReductionPercent / 100m);
                    }
                }
                else
                {
                    if (attribute == definition.NegativeAttribute)
                    {
                        multiplier *= SignedStatMath.AmplificationMultiplier(
                            magnitude * definition.NegativeAmplificationPercent / 100m);
                    }
                    else if (attribute == definition.PositiveAttribute)
                    {
                        multiplier *= SignedStatMath.ReductionMultiplier(
                            magnitude * definition.NegativeReductionPercent / 100m);
                    }
                }
            }

            if (Wind?.Definition is WindWeatherAsset windDefinition
                && attribute == PachimonAttribute.Wind)
            {
                multiplier *= SignedStatMath.AmplificationMultiplier(
                    Wind.Value
                    * windDefinition.WindRatioScalingPercent / 100m);
            }

            var rain = Rain;
            if (rain?.Definition is RainWeatherAsset rainDefinition
                && (IsRaining || IsSnowing))
            {
                var effectiveRainValue = GetEffectiveRainValue();
                multiplier *= IsRaining
                    ? attribute switch
                    {
                        PachimonAttribute.Aqua =>
                            SignedStatMath.AmplificationMultiplier(
                                effectiveRainValue
                                * rainDefinition.AquaRatioScalingPercent / 100m),
                        PachimonAttribute.Fire =>
                            SignedStatMath.ReductionMultiplier(
                                effectiveRainValue
                                * rainDefinition.FireRatioScalingPercent / 100m),
                        _ => 1m,
                    }
                    : attribute switch
                    {
                        PachimonAttribute.Ice =>
                            SignedStatMath.AmplificationMultiplier(
                                effectiveRainValue
                                * rainDefinition.SnowIceRatioScalingPercent / 100m),
                        PachimonAttribute.Fire =>
                            SignedStatMath.ReductionMultiplier(
                                effectiveRainValue
                                * rainDefinition.SnowFireRatioScalingPercent / 100m),
                        _ => 1m,
                    };
            }
            else if (rain?.Definition is RainWeatherAsset sunnyDefinition
                     && IsSunny)
            {
                var sunnyValue = Math.Abs((decimal)rain.Value);
                multiplier *= attribute switch
                {
                    PachimonAttribute.Fire =>
                        SignedStatMath.AmplificationMultiplier(
                            sunnyValue
                            * sunnyDefinition.SunnyFireRatioScalingPercent / 100m),
                    PachimonAttribute.Aqua =>
                        SignedStatMath.ReductionMultiplier(
                            sunnyValue
                            * sunnyDefinition.SunnyAquaRatioScalingPercent / 100m),
                    _ => 1m,
                };
            }
            if (Thunder?.Definition is ThunderWeatherAsset thunderDefinition
                && attribute == PachimonAttribute.Electric)
            {
                multiplier *= SignedStatMath.AmplificationMultiplier(
                    Thunder.Value
                    * thunderDefinition.ElectricRatioScalingPercent / 100m);
            }
            return multiplier;
        }

        public decimal GetEffectiveRainValue()
        {
            return Rain == null
                ? 0m
                : Rain.Value * GetRainEffectMultiplier();
        }

        public IEnumerable<IStatModifier> CreateStatModifiers(
            BattleUnitState unit)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (Wind?.Definition is WindWeatherAsset windDefinition)
            {
                yield return new DerivedStatModifier(
                    PachimonStatType.Speed,
                    StatModifierOperation.DerivedAdditive,
                    stats => stats.GetValue(PachimonStatType.Wind)
                        * windDefinition.SpeedFromWindRatio / 100m,
                    new StatModifierSource(
                        StatModifierSourceType.FieldEffect,
                        "weather:wind",
                        Wind.DisplayName));
            }
            if (Thunder?.Definition is ThunderWeatherAsset thunderDefinition)
            {
                yield return new DerivedStatModifier(
                    PachimonStatType.Speed,
                    StatModifierOperation.DerivedAdditive,
                    stats => stats.GetValue(PachimonStatType.Electric)
                        * thunderDefinition.SpeedFromElectricRatio / 100m,
                    new StatModifierSource(
                        StatModifierSourceType.FieldEffect,
                        "weather:thunder",
                        Thunder.DisplayName));
            }
        }

        public void HandleAttributeDamage(
            BattleUnitState source,
            BattleUnitState target,
            PachimonAttribute attribute,
            decimal preDefenseDamage)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (preDefenseDamage <= 0m) return;
            source ??= target;

            switch (attribute)
            {
                case PachimonAttribute.Fire:
                    ApplyDamageDrivenSignedChange(
                        ref _temperatureDamageCarry,
                        preDefenseDamage * (decimal)(_definitions?.Temperature?.DamageChangePercent ?? 0f) / 100m,
                        source,
                        _definitions?.Temperature);
                    ApplyDamageDrivenSignedChange(
                        ref _moistureDamageCarry,
                        -preDefenseDamage * (decimal)(_definitions?.Moisture?.DamageChangePercent ?? 0f) / 100m,
                        source,
                        _definitions?.Moisture);
                    break;
                case PachimonAttribute.Ice:
                    ApplyDamageDrivenSignedChange(
                        ref _temperatureDamageCarry,
                        -preDefenseDamage * (decimal)(_definitions?.Temperature?.DamageChangePercent ?? 0f) / 100m,
                        source,
                        _definitions?.Temperature);
                    break;
                case PachimonAttribute.Wind:
                    ApplyFractionalPositiveChange(
                        ref _windDamageCarry,
                        preDefenseDamage * (_definitions?.Wind?.DamageChangePercent ?? 0) / 100m,
                        source,
                        _definitions?.Wind);
                    break;
                case PachimonAttribute.Electric:
                    ApplyDamageDrivenSignedChange(
                        ref _plasmaDamageCarry,
                        preDefenseDamage * (decimal)(_definitions?.Plasma?.DamageChangePercent ?? 0f) / 100m,
                        source,
                        _definitions?.Plasma);
                    break;
                case PachimonAttribute.Leaf:
                    ApplyDamageDrivenSignedChange(
                        ref _plasmaDamageCarry,
                        -preDefenseDamage * (decimal)(_definitions?.Plasma?.DamageChangePercent ?? 0f) / 100m,
                        source,
                        _definitions?.Plasma);
                    break;
                case PachimonAttribute.Aqua:
                    ApplyDamageDrivenSignedChange(
                        ref _moistureDamageCarry,
                        preDefenseDamage * (decimal)(_definitions?.Moisture?.DamageChangePercent ?? 0f) / 100m,
                        source,
                        _definitions?.Moisture);
                    break;
            }
        }

        private void ApplyDamageDrivenSignedChange(
            ref decimal carry,
            decimal rawChange,
            BattleUnitState source,
            BattleWeatherAsset definition)
        {
            if (definition == null || rawChange == 0m) return;
            var change = CalculateDamageDrivenSignedChange(
                GetAxisValue(definition.WeatherId),
                rawChange);
            ApplyFractionalSignedChange(ref carry, change, source, definition);
        }

        public static decimal CalculateDamageDrivenSignedChange(
            int currentValue,
            decimal rawChange)
        {
            var growsCurrentDirection = currentValue > 0 && rawChange > 0m
                || currentValue < 0 && rawChange < 0m;
            if (!growsCurrentDirection) return rawChange;

            return rawChange * DamageDrivenGrowthK
                / (DamageDrivenGrowthK + Math.Abs((decimal)currentValue));
        }

        private void ApplyFractionalSignedChange(
            ref decimal carry,
            decimal change,
            BattleUnitState source,
            BattleWeatherAsset definition)
        {
            if (definition == null || change == 0m) return;
            carry += change;
            var whole = decimal.ToInt32(decimal.Truncate(carry));
            if (whole == 0) return;
            carry -= whole;
            AddSignedRaw(source, definition, whole);
        }

        private void ApplyFractionalPositiveChange(
            ref decimal carry,
            decimal change,
            BattleUnitState source,
            BattleWeatherAsset definition)
        {
            if (definition == null || change <= 0m) return;
            carry += change;
            var whole = SignedStatMath.FloorNonNegative(carry);
            if (whole <= 0) return;
            carry -= whole;
            AddPositiveRaw(source, definition, whole);
        }

        public void HandleDamageApplied(DamageAppliedEvent damageEvent)
        {
            if (damageEvent == null)
            {
                throw new ArgumentNullException(nameof(damageEvent));
            }
            if (!IsSnowing
                || !damageEvent.Target.IsAlive
                || damageEvent.ReceivedDamage <= 0
                || damageEvent.OriginKind == DamageOriginKind.Status
                || damageEvent.OriginKind == DamageOriginKind.Field
                || damageEvent.Attribute == PachimonAttribute.Fire)
            {
                return;
            }

            var rain = Rain;
            if (rain?.Definition is not RainWeatherAsset definition
                || definition.ChillStatus == null)
            {
                return;
            }

            var scaledTemperature = Math.Abs((decimal)Temperature)
                * definition.SnowChillTemperatureRatio / 100m
                * GetRainEffectMultiplier();
            var chillValue = SignedStatMath.FloorNonNegative(
                definition.SnowChillBaseValue
                * SignedStatMath.AmplificationMultiplier(scaledTemperature));
            if (chillValue <= 0)
            {
                return;
            }

            _state.Statuses.ApplyStatus(
                damageEvent.Target,
                BattleStatusFactory.CreateSlow(
                    rain.Source,
                    chillValue,
                    definition.ChillStatus));
        }

        internal void AdvanceTime(int ticks)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
            for (var elapsed = 0; elapsed < ticks; elapsed++)
            {
                var contextChanged = false;
                var refreshDerivedStatuses = false;
                foreach (var item in _weather.ToArray())
                {
                    if (item.WeatherId == BattleWeatherId.Temperature
                        || item.WeatherId == BattleWeatherId.Moisture
                        || item.WeatherId == BattleWeatherId.Plasma)
                    {
                        continue;
                    }
                    var beforeDecay = new BeforeWeatherDecayEvent(
                        _state,
                        item,
                        decayPerTick: 1m);
                    _state.Events.Publish(beforeDecay);
                    var previousValue = item.Value;
                    item.Advance(beforeDecay.DecayPerTick);
                    contextChanged |= item.Value != previousValue;
                    if (item.Value > 0
                        && item.Definition is ThunderWeatherAsset thunder
                        && item.AdvanceApplication(thunder.AttackIntervalTicks))
                    {
                        ApplyThunderDamage(item, thunder);
                    }
                    if (item.WeatherId == BattleWeatherId.Rain
                        && item.Definition is RainWeatherAsset precipitation
                        && item.Value != 0
                        && item.AdvanceApplication(
                            precipitation.EnvironmentIntervalTicks))
                    {
                        ApplyPrecipitationEnvironment(item, precipitation);
                    }
                    if (item.Value == 0)
                    {
                        _weather.Remove(item);
                        contextChanged = true;
                        refreshDerivedStatuses |=
                            item.WeatherId == BattleWeatherId.Rain;
                    }
                }
                if (contextChanged)
                {
                    if (refreshDerivedStatuses)
                    {
                        RefreshDerivedStatuses();
                    }
                    NotifyContextChanged();
                }

                AccumulateLeakOneTick();
            }
        }

        private void ApplyPrecipitationEnvironment(
            BattleWeatherInstance precipitation,
            RainWeatherAsset definition)
        {
            if (precipitation.Value > 0)
            {
                var moistureChange = GetEffectiveRainValue()
                    * definition.EnvironmentChangePercent / 100m;
                ApplyFractionalSignedChange(
                    ref _precipitationMoistureCarry,
                    moistureChange,
                    precipitation.Source,
                    _definitions?.Moisture);
                return;
            }

            var sunnyChange = Math.Abs((decimal)precipitation.Value)
                * definition.EnvironmentChangePercent / 100m;
            ApplyFractionalSignedChange(
                ref _sunnyTemperatureCarry,
                sunnyChange,
                precipitation.Source,
                _definitions?.Temperature);
            ApplyFractionalSignedChange(
                ref _sunnyMoistureCarry,
                -sunnyChange,
                precipitation.Source,
                _definitions?.Moisture);
        }

        private void ApplyThunderDamage(
            BattleWeatherInstance thunder,
            ThunderWeatherAsset definition)
        {
            var damage = thunder.Value / definition.DamageDivisor;
            if (damage <= 0) return;
            _state.AddLog($"{thunder.DisplayName}の攻撃！");
            foreach (var target in GetAllUnits().Where(unit => unit.IsAlive).ToArray())
            {
                BattleAttributeDamageService.Apply(
                    _state,
                    thunder.Source,
                    target,
                    new DamageContext(
                        DamageOriginKind.Field,
                        (int)BattleWeatherId.Thunder,
                        damage,
                        thunder.Source.GetBattleStats(),
                        target.GetBattleStats(),
                        PachimonAttribute.Electric,
                        isAttack: false,
                        applyAttackerAttributeMultiplier: false,
                        applyDamageBonusMultiplier: false,
                        applyOutgoingModifiers: false));
            }
        }

        internal void CopyForSimulation(
            BattleWeatherRuntime original,
            IReadOnlyDictionary<BattleUnitState, BattleUnitState> unitMap)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (unitMap == null) throw new ArgumentNullException(nameof(unitMap));
            _weather.Clear();
            foreach (var item in original.Weather)
            {
                _weather.Add(item.CreateSimulationClone(
                    this,
                    unitMap[item.Source]));
            }
            _temperatureDamageCarry = original._temperatureDamageCarry;
            _moistureDamageCarry = original._moistureDamageCarry;
            _plasmaDamageCarry = original._plasmaDamageCarry;
            _windDamageCarry = original._windDamageCarry;
            _precipitationMoistureCarry = original._precipitationMoistureCarry;
            _sunnyTemperatureCarry = original._sunnyTemperatureCarry;
            _sunnyMoistureCarry = original._sunnyMoistureCarry;
            NotifyContextChanged();
        }

        private void RefreshDerivedStatuses()
        {
            var rain = Rain;
            rain?.SetSnowPresentation(IsSnowing);
            if (IsRaining)
            {
                return;
            }

            rain?.ResetLeakAccumulationWork();
        }

        private void AccumulateLeakOneTick()
        {
            var rain = Rain;
            if (!IsRaining
                || rain?.Definition is not RainWeatherAsset definition)
            {
                return;
            }

            var increment = rain.AccumulateLeak(
                GetEffectiveRainValue()
                * definition.LeakValueRatioPerTick / 10000m);
            if (increment <= 0)
            {
                return;
            }

            foreach (var unit in GetAllUnits().Where(unit => unit.IsAlive))
            {
                _state.Statuses.ApplyStatus(
                    unit,
                    new BattleStatusInstance(
                        BattleStatusId.Leak,
                        BattleStatusCategory.Leak,
                        rain.Source,
                        increment));
            }
        }

        private IEnumerable<BattleUnitState> GetAllUnits()
        {
            return _state.Player.Units.Concat(_state.Enemy.Units);
        }

        private decimal GetRainEffectMultiplier()
        {
            return Wind?.Definition is WindWeatherAsset definition
                ? SignedStatMath.AmplificationMultiplier(
                    Wind.Value
                    * definition.RainEffectRatioScalingPercent / 100m)
                : 1m;
        }

        private void NotifyContextChanged()
        {
            foreach (var unit in GetAllUnits())
            {
                unit.NotifyBattleContextChanged();
            }
        }

        private void ValidateSource(BattleUnitState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!_state.Player.Units.Contains(source)
                && !_state.Enemy.Units.Contains(source))
            {
                throw new ArgumentException(
                    "The Weather source does not belong to this Battle.",
                    nameof(source));
            }
        }
    }
}
