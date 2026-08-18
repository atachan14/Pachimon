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
            BattleWeatherAsset definition,
            BattleUnitState source,
            int value)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            if (definition.WeatherId == BattleWeatherId.Temperature
                ? value == 0
                : value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            Value = value;
        }

        public BattleWeatherAsset Definition { get; }
        public BattleWeatherId WeatherId => Definition.WeatherId;
        public BattleUnitState Source { get; private set; }
        public int Value { get; private set; }
        public decimal DecayWork { get; private set; }
        public decimal LeakAccumulationWork { get; private set; }
        public int ApplicationWork { get; private set; }
        public bool IsSnow { get; private set; }
        public string DisplayName => IsSnow
            && Definition is RainWeatherAsset rain
                ? rain.SnowDisplayName
                : Definition.DisplayName;
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
            var amount = Math.Min(Value, SignedStatMath.FloorNonNegative(DecayWork));
            DecayWork -= amount;
            Value -= amount;
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
            BattleUnitState sourceClone)
        {
            return new BattleWeatherInstance(Definition, sourceClone, Value)
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
        private readonly BattleState _state;
        private readonly List<BattleWeatherInstance> _weather = new();

        public BattleWeatherRuntime(BattleState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public IReadOnlyList<BattleWeatherInstance> Weather => _weather;
        public int ActiveWeatherTypeCount => _weather.Count(item =>
            item.WeatherId == BattleWeatherId.Temperature
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
        public bool IsSnowing => Temperature < 0 && Has(BattleWeatherId.Rain);
        public bool IsRaining => Temperature >= 0 && Has(BattleWeatherId.Rain);

        public bool Has(BattleWeatherId weatherId)
        {
            return _weather.Any(item =>
                item.WeatherId == weatherId
                && (weatherId == BattleWeatherId.Temperature
                    ? item.Value != 0
                    : item.Value > 0));
        }

        public BattleWeatherInstance Get(BattleWeatherId weatherId)
        {
            return _weather.FirstOrDefault(item =>
                item.WeatherId == weatherId
                && (weatherId == BattleWeatherId.Temperature
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
            if (definition.WeatherId == BattleWeatherId.Temperature)
            {
                throw new ArgumentException(
                    "Use AddTemperature for the signed Temperature axis.",
                    nameof(definition));
            }
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

            var created = new BattleWeatherInstance(definition, source, value);
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

            var wasRaining = IsRaining;
            var wasSnowing = IsSnowing;

            var existing = _weather.FirstOrDefault(item =>
                item.WeatherId == BattleWeatherId.Temperature);
            if (existing == null)
            {
                _weather.Add(new BattleWeatherInstance(definition, source, amount));
            }
            else
            {
                if (!ReferenceEquals(existing.Definition, definition))
                {
                    throw new InvalidOperationException(
                        "Temperature changes must use the same Definition.");
                }
                existing.AddSignedValue(source, amount);
                if (existing.Value == 0)
                {
                    _weather.Remove(existing);
                }
            }

            if (wasRaining != IsRaining || wasSnowing != IsSnowing)
            {
                RefreshDerivedStatuses();
            }
            NotifyContextChanged();
            return Temperature;
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
                        PachimonAttribute.Aqua =>
                            SignedStatMath.ReductionMultiplier(
                                weather.Value
                                * temperature.AquaRatioScalingPercent / 100m),
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

            if (Wind?.Definition is WindWeatherAsset windDefinition
                && attribute == PachimonAttribute.Wind)
            {
                multiplier *= SignedStatMath.AmplificationMultiplier(
                    Wind.Value
                    * windDefinition.WindRatioScalingPercent / 100m);
            }

            var rain = Rain;
            if (rain?.Definition is RainWeatherAsset rainDefinition && IsRaining)
            {
                var effectiveRainValue = GetEffectiveRainValue();
                multiplier *= attribute switch
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
                    if (item.WeatherId == BattleWeatherId.Temperature)
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
                    if (item.Value <= 0)
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
                _weather.Add(item.CreateSimulationClone(unitMap[item.Source]));
            }
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
