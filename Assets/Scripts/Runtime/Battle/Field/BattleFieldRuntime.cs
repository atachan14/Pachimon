using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Reward;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public enum BattleFieldEffectId
    {
        Smog = 1,
        FireBarrier = 2,
        FrozenGround = 3,
        IceBlade = 4,
        WaterVeil = 5,
        BeatVine = 6,
        FireVine = 7,
        PoisonMist = 8,
        ResponsivePlant = 9,
    }

    public sealed class BattleDefenseSnapshot
    {
        private readonly decimal[] _attributes;

        private BattleDefenseSnapshot(decimal[] attributes, decimal resistBonus)
        {
            _attributes = attributes;
            ResistBonus = resistBonus;
        }

        public decimal ResistBonus { get; }

        public decimal GetAttribute(PachimonAttribute attribute)
        {
            return _attributes[(int)attribute];
        }

        public static BattleDefenseSnapshot Capture(
            EffectivePachimonStats stats,
            int ratio)
        {
            if (stats == null) throw new ArgumentNullException(nameof(stats));
            if (ratio < 0) throw new ArgumentOutOfRangeException(nameof(ratio));
            var attributes = Enum.GetValues(typeof(PachimonAttribute))
                .Cast<PachimonAttribute>()
                .Select(attribute =>
                    stats.GetValue(PachimonStatTypeUtility.FromAttribute(attribute))
                    * ratio / 100m)
                .ToArray();
            return new BattleDefenseSnapshot(
                attributes,
                stats.ResistBonus * ratio / 100m);
        }
    }

    public readonly struct BattleFieldInterceptionResult
    {
        public BattleFieldInterceptionResult(
            BattleFieldEffectInstance fieldEffect,
            int incomingDamage,
            int absorbedDamage,
            int overflowDamage)
        {
            FieldEffect = fieldEffect;
            IncomingDamage = incomingDamage;
            AbsorbedDamage = absorbedDamage;
            OverflowDamage = overflowDamage;
        }

        public BattleFieldEffectInstance FieldEffect { get; }
        public int IncomingDamage { get; }
        public int AbsorbedDamage { get; }
        public int OverflowDamage { get; }
        public bool WasIntercepted => FieldEffect != null;
    }

    public sealed class BattleFieldEffectInstance
    {
        private readonly List<BattleUnitState> _frozenGroundSources = new();
        private readonly List<BattleStatusInstance> _statuses = new();

        internal BattleFieldEffectInstance(
            BattleFieldEffectAsset definition,
            BattleSide targetSide,
            BattleUnitState source,
            int value,
            int secondaryValue = 0)
        {
            Definition = definition
                ?? throw new ArgumentNullException(nameof(definition));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            EffectId = definition.EffectId;
            TargetSide = targetSide;
            Source = source;
            _frozenGroundSources.Add(source);
            _value = value;
            SecondaryValue = secondaryValue;
        }

        private BattleFieldEffectInstance(
            FireBarrierFieldEffectAsset definition,
            BattleSide targetSide,
            BattleUnitState source,
            int value,
            int hp,
            int durationTicks,
            BattleDefenseSnapshot defenseSnapshot)
            : this(definition, targetSide, source, value)
        {
            if (hp <= 0) throw new ArgumentOutOfRangeException(nameof(hp));
            if (durationTicks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationTicks));
            }
            DefenseSnapshot = defenseSnapshot
                ?? throw new ArgumentNullException(nameof(defenseSnapshot));
            MaxHp = hp;
            CurrentHp = hp;
            RemainingTicks = durationTicks;
        }

        private BattleFieldEffectInstance(
            IceBladeFieldEffectAsset definition,
            BattleSide targetSide,
            BattleUnitState source,
            int durationTicks)
            : this(definition, targetSide, source, value: 1)
        {
            if (durationTicks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationTicks));
            }
            RemainingTicks = durationTicks;
        }

        private BattleFieldEffectInstance(
            PoisonMistFieldEffectAsset definition,
            BattleSide targetSide,
            BattleUnitState source,
            int value,
            int durationTicks)
            : this(definition, targetSide, source, value)
        {
            if (durationTicks <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationTicks));
            RemainingTicks = durationTicks;
        }

        public BattleFieldEffectId EffectId { get; }
        public BattleSide TargetSide { get; }
        public BattleUnitState Source { get; private set; }
        private int _value;

        public int Value => EffectId == BattleFieldEffectId.FrozenGround
            && Definition is FrozenGroundFieldEffectAsset frozenGround
                ? _frozenGroundSources
                    .Where(source => source.IsAlive)
                    .Sum(frozenGround.CalculateValue)
                : _value;
        public int SecondaryValue { get; private set; }
        public BattleFieldEffectAsset Definition { get; private set; }
        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int? RemainingTicks { get; private set; }
        public BattleDefenseSnapshot DefenseSnapshot { get; private set; }
        public IReadOnlyList<BattleStatusInstance> Statuses => _statuses;
        public decimal ApplicationWork { get; private set; }
        public decimal DecayWork { get; private set; }
        public bool IsExpired => Value <= 0
            || (EffectId == BattleFieldEffectId.FrozenGround
                && !_frozenGroundSources.Any(source => source.IsAlive))
            || (EffectId == BattleFieldEffectId.IceBlade
                && RemainingTicks <= 0)
            || (EffectId == BattleFieldEffectId.PoisonMist
                && RemainingTicks <= 0)
            || (EffectId == BattleFieldEffectId.FireBarrier
                && (CurrentHp <= 0 || RemainingTicks <= 0));

        public string DisplayName => Definition?.DisplayName ?? EffectId switch
        {
            BattleFieldEffectId.Smog => "スモッグ",
            BattleFieldEffectId.FireBarrier => "炎の障壁",
            BattleFieldEffectId.FrozenGround => "氷の大地",
            BattleFieldEffectId.IceBlade => "氷の刃",
            BattleFieldEffectId.ResponsivePlant => "呼応する植物",
            _ => EffectId.ToString(),
        };

        public string Description => Definition?.Description ?? string.Empty;

        public BattleStatusInstance GetStatus(BattleStatusId statusId)
        {
            return _statuses.FirstOrDefault(status => status.StatusId == statusId);
        }

        public decimal GetEffectiveResistBonus()
        {
            var erosion = GetStatus(BattleStatusId.WindErosion)?.Value ?? 0;
            return DefenseSnapshot.ResistBonus - erosion;
        }

        internal int AddOrMergeStatus(BattleStatusInstance status)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));
            if (EffectId != BattleFieldEffectId.FireBarrier)
            {
                throw new InvalidOperationException(
                    "Only Fire Barrier can receive Field Entity statuses.");
            }
            if (status.StatusId is not (
                    BattleStatusId.Toxin
                    or BattleStatusId.Weakness
                    or BattleStatusId.WindErosion))
            {
                return 0;
            }

            var appliedValue = status.Value;
            var existing = GetStatus(status.StatusId);
            if (status.StatusId == BattleStatusId.Toxin)
            {
                appliedValue = status.ToxinApplications.Sum(
                    application => application.AppliedValue);
                if (existing != null)
                {
                    if (!ReferenceEquals(existing.Definition, status.Definition))
                    {
                        throw new InvalidOperationException(
                            "A Field Toxin reapplication must use the same Definition.");
                    }
                    foreach (var application in status.ToxinApplications)
                    {
                        existing.AddToxinApplication(application);
                    }
                }
                else if (appliedValue > 0)
                {
                    _statuses.Add(status);
                }
                return appliedValue;
            }

            if (appliedValue <= 0)
            {
                return 0;
            }
            if (existing != null)
            {
                existing.AddValue(appliedValue);
            }
            else
            {
                _statuses.Add(status);
            }
            return appliedValue;
        }

        internal bool TryConsumeStatus(
            BattleStatusId statusId,
            out BattleStatusInstance status)
        {
            status = GetStatus(statusId);
            return status != null && _statuses.Remove(status);
        }

        internal void RemoveExpiredStatuses()
        {
            _statuses.RemoveAll(status => status.IsExpired);
        }

        internal void AddValue(BattleUnitState source, int value)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            Source = source;
            _value = checked(_value + value);
        }

        internal void AddFrozenGroundSource(BattleUnitState source)
        {
            if (EffectId != BattleFieldEffectId.FrozenGround)
            {
                throw new InvalidOperationException(
                    "Only Frozen Ground can add a source.");
            }
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!_frozenGroundSources.Contains(source))
            {
                _frozenGroundSources.Add(source);
            }
        }

        internal BattleUnitState GetActiveFrozenGroundSource()
        {
            if (EffectId != BattleFieldEffectId.FrozenGround)
            {
                throw new InvalidOperationException(
                    "Only Frozen Ground has active sources.");
            }
            return _frozenGroundSources.FirstOrDefault(source => source.IsAlive);
        }

        internal int AdvanceSmogOneTick()
        {
            if (EffectId != BattleFieldEffectId.Smog)
            {
                throw new InvalidOperationException(
                    "Only Smog can use the Smog tick policy.");
            }

            var definition = Definition as SmogFieldEffectAsset
                ?? throw new InvalidOperationException(
                    "Smog requires a Smog Field Effect Definition.");
            ApplicationWork += Value
                * definition.ToxinApplicationRatio / 100m;
            DecayWork += Value
                * definition.DecayPerTickRatio / 100m;
            var appliedValue = SignedStatMath.FloorNonNegative(ApplicationWork);
            var decay = Math.Min(
                Value,
                SignedStatMath.FloorNonNegative(DecayWork));
            ApplicationWork -= appliedValue;
            DecayWork -= decay;
            _value -= decay;
            return appliedValue;
        }

        internal void AddFireBarrierGeneration(
            BattleUnitState source,
            int value,
            int hp,
            int durationTicks,
            BattleDefenseSnapshot defenseSnapshot)
        {
            if (EffectId != BattleFieldEffectId.FireBarrier)
            {
                throw new InvalidOperationException(
                    "Only Fire Barrier can add a Barrier generation.");
            }
            AddValue(source, value);
            MaxHp = checked(MaxHp + hp);
            CurrentHp = checked(CurrentHp + hp);
            RemainingTicks = checked(RemainingTicks.GetValueOrDefault()
                + durationTicks);
            DefenseSnapshot = defenseSnapshot
                ?? throw new ArgumentNullException(nameof(defenseSnapshot));
        }

        internal int ApplyFireBarrierDamage(int damage)
        {
            if (EffectId != BattleFieldEffectId.FireBarrier)
            {
                throw new InvalidOperationException(
                    "Only Fire Barrier can receive Barrier damage.");
            }
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            var absorbed = Math.Min(CurrentHp, damage);
            CurrentHp -= absorbed;
            return damage - absorbed;
        }

        internal void AdvanceFireBarrierOneTick()
        {
            if (EffectId != BattleFieldEffectId.FireBarrier)
            {
                throw new InvalidOperationException(
                    "Only Fire Barrier can use the Barrier tick policy.");
            }
            RemainingTicks = Math.Max(
                0,
                RemainingTicks.GetValueOrDefault() - 1);
        }

        internal void AddIceBladeDuration(
            BattleUnitState source,
            int durationTicks)
        {
            if (EffectId != BattleFieldEffectId.IceBlade)
            {
                throw new InvalidOperationException(
                    "Only Ice Blade can add duration.");
            }
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (durationTicks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationTicks));
            }
            Source = source;
            RemainingTicks = checked(
                RemainingTicks.GetValueOrDefault() + durationTicks);
        }

        internal void AdvanceIceBladeOneTick()
        {
            if (EffectId != BattleFieldEffectId.IceBlade)
            {
                throw new InvalidOperationException(
                    "Only Ice Blade can advance its duration.");
            }
            RemainingTicks = Math.Max(
                0,
                RemainingTicks.GetValueOrDefault() - 1);
        }

        internal void AdvancePoisonMistOneTick()
        {
            if (EffectId != BattleFieldEffectId.PoisonMist)
                throw new InvalidOperationException(
                    "Only Poison Mist can advance its duration.");
            RemainingTicks = Math.Max(
                0,
                RemainingTicks.GetValueOrDefault() - 1);
        }

        internal int AdvanceWaterVeilOneTick()
        {
            if (EffectId != BattleFieldEffectId.WaterVeil)
            {
                throw new InvalidOperationException(
                    "Only Water Veil can use the Water Veil tick policy.");
            }
            var definition = Definition as WaterVeilFieldEffectAsset
                ?? throw new InvalidOperationException(
                    "Water Veil requires a Water Veil Definition.");
            var healing = definition.HealingPerTick;
            _value = Math.Max(0, _value - definition.DecayPerTick);
            return healing;
        }

        internal bool AdvanceBeatVineOneTick()
        {
            if (EffectId != BattleFieldEffectId.BeatVine)
            {
                throw new InvalidOperationException(
                    "Only Beat Vine can use the Beat Vine tick policy.");
            }
            var definition = Definition as BeatVineFieldEffectAsset
                ?? throw new InvalidOperationException(
                    "Beat Vine requires its Field Effect Definition.");
            ApplicationWork += 1m;
            if (ApplicationWork < definition.AttackIntervalTicks)
                return false;
            ApplicationWork -= definition.AttackIntervalTicks;
            return true;
        }

        internal static BattleFieldEffectInstance CreateFireBarrier(
            FireBarrierFieldEffectAsset definition,
            BattleSide targetSide,
            BattleUnitState source,
            int value,
            int hp,
            int durationTicks,
            BattleDefenseSnapshot defenseSnapshot)
        {
            return new BattleFieldEffectInstance(
                definition,
                targetSide,
                source,
                value,
                hp,
                durationTicks,
                defenseSnapshot);
        }

        internal static BattleFieldEffectInstance CreateIceBlade(
            IceBladeFieldEffectAsset definition,
            BattleSide targetSide,
            BattleUnitState source,
            int durationTicks)
        {
            return new BattleFieldEffectInstance(
                definition,
                targetSide,
                source,
                durationTicks);
        }

        internal static BattleFieldEffectInstance CreatePoisonMist(
            PoisonMistFieldEffectAsset definition,
            BattleSide targetSide,
            BattleUnitState source,
            int value,
            int durationTicks)
        {
            return new BattleFieldEffectInstance(
                definition,
                targetSide,
                source,
                value,
                durationTicks);
        }

        internal BattleFieldEffectInstance CreateSimulationClone(
            IReadOnlyDictionary<BattleUnitState, BattleUnitState> unitMap)
        {
            if (unitMap == null) throw new ArgumentNullException(nameof(unitMap));
            if (!unitMap.TryGetValue(Source, out var sourceClone))
            {
                throw new InvalidOperationException(
                    "A Field Effect source does not belong to the Battle.");
            }
            if (EffectId == BattleFieldEffectId.FireBarrier)
            {
                var clone = new BattleFieldEffectInstance(
                    (FireBarrierFieldEffectAsset)Definition,
                    TargetSide,
                    sourceClone,
                    Value,
                    MaxHp,
                    RemainingTicks.GetValueOrDefault(),
                    DefenseSnapshot)
                {
                    CurrentHp = CurrentHp,
                };
                CopyStatusesTo(clone, unitMap);
                return clone;
            }

            if (EffectId == BattleFieldEffectId.FrozenGround)
            {
                var clone = new BattleFieldEffectInstance(
                    Definition,
                    TargetSide,
                    sourceClone,
                    value: 1);
                foreach (var source in _frozenGroundSources.Skip(1))
                {
                    if (!unitMap.TryGetValue(source, out var mappedSource))
                    {
                        throw new InvalidOperationException(
                            "A Frozen Ground source does not belong to the Battle.");
                    }
                    clone.AddFrozenGroundSource(mappedSource);
                }
                CopyStatusesTo(clone, unitMap);
                return clone;
            }

            if (EffectId == BattleFieldEffectId.IceBlade)
            {
                var clone = CreateIceBlade(
                    (IceBladeFieldEffectAsset)Definition,
                    TargetSide,
                    sourceClone,
                    RemainingTicks.GetValueOrDefault());
                CopyStatusesTo(clone, unitMap);
                return clone;
            }

            if (EffectId == BattleFieldEffectId.PoisonMist)
            {
                var clone = CreatePoisonMist(
                    (PoisonMistFieldEffectAsset)Definition,
                    TargetSide,
                    sourceClone,
                    Value,
                    RemainingTicks.GetValueOrDefault());
                CopyStatusesTo(clone, unitMap);
                return clone;
            }

            var defaultClone = new BattleFieldEffectInstance(
                Definition,
                TargetSide,
                sourceClone,
                Value,
                SecondaryValue)
            {
                ApplicationWork = ApplicationWork,
                DecayWork = DecayWork,
            };
            CopyStatusesTo(defaultClone, unitMap);
            return defaultClone;
        }

        private void CopyStatusesTo(
            BattleFieldEffectInstance clone,
            IReadOnlyDictionary<BattleUnitState, BattleUnitState> unitMap)
        {
            foreach (var status in _statuses)
            {
                BattleUnitState sourceClone = null;
                if (status.Source != null
                    && !unitMap.TryGetValue(status.Source, out sourceClone))
                {
                    throw new InvalidOperationException(
                        "A Field Status source does not belong to the Battle.");
                }
                var clonedStatus = new BattleStatusInstance(
                    status.StatusId,
                    status.Categories,
                    sourceClone,
                    status.Value,
                    status.StackCount,
                    status.RemainingTicks,
                    status.RuntimeData,
                    status.Definition);
                clonedStatus.CopyToxinRuntimeFrom(status);
                clone._statuses.Add(clonedStatus);
            }
        }
    }

    public sealed class BattleFieldRuntime
    {
        private readonly BattleState _state;
        private readonly List<BattleFieldEffectInstance> _effects = new();

        public BattleFieldRuntime(BattleState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public IReadOnlyList<BattleFieldEffectInstance> Effects => _effects
            .Where(effect => !effect.IsExpired)
            .ToArray();

        public int CountEffects(
            BattleSide side,
            BattleFieldEffectCategory category)
        {
            return Effects.Count(effect =>
                effect.TargetSide == side
                && (effect.Definition.Categories & category) != 0);
        }

        public BattleFieldEffectInstance CreateBeatVine(
            BattleUnitState source,
            BeatVineFieldEffectAsset definition,
            int value)
        {
            return CreateIndependentPlant(source, definition, value, 0);
        }

        public BattleFieldEffectInstance CreateFireVine(
            BattleUnitState source,
            FireVineFieldEffectAsset definition,
            int leafValue,
            int fireValue)
        {
            if (fireValue <= 0)
                throw new ArgumentOutOfRangeException(nameof(fireValue));
            return CreateIndependentPlant(
                source,
                definition,
                leafValue,
                fireValue);
        }

        public BattleFieldEffectInstance CreateResponsivePlant(
            BattleUnitState source,
            ResponsivePlantFieldEffectAsset definition,
            int value)
        {
            return CreateIndependentPlant(source, definition, value, 0);
        }

        public void AttackAllPlants(
            BattleSide side,
            int damageBonusPercent)
        {
            if (damageBonusPercent < 0)
                throw new ArgumentOutOfRangeException(nameof(damageBonusPercent));
            var plants = Effects
                .Where(effect => effect.TargetSide == side
                    && (effect.Definition.Categories
                        & BattleFieldEffectCategory.Plant) != 0)
                .ToArray();
            foreach (var plant in plants)
            {
                var target = _state.GetOpposingSide(side).GetFrontLiving();
                if (target == null) break;
                AttackPlantAndRespond(plant, target, damageBonusPercent);
            }
        }

        private BattleFieldEffectInstance CreateIndependentPlant(
            BattleUnitState source,
            BattleFieldEffectAsset definition,
            int value,
            int secondaryValue)
        {
            ValidateSource(source);
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if ((definition.Categories & BattleFieldEffectCategory.Plant) == 0)
                throw new ArgumentException("A Plant Definition is required.", nameof(definition));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));

            var plant = new BattleFieldEffectInstance(
                definition,
                source.Side,
                source,
                value,
                secondaryValue);
            _effects.Add(plant);
            NotifyContextChanged();
            LogFieldEffectCreated(source, plant, source.Side);
            return plant;
        }

        public BattleFieldEffectInstance CreateOrAddSmog(
            BattleUnitState source,
            BattleSide targetSide,
            SmogFieldEffectAsset definition,
            int value)
        {
            ValidateSource(source);
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            var beforeApplied = new BeforeFieldEffectValueAppliedEvent(
                _state,
                source,
                BattleFieldEffectId.Smog,
                targetSide,
                value);
            _state.Events.Publish(beforeApplied);
            value = beforeApplied.Value;
            var existing = _effects.FirstOrDefault(effect =>
                effect.EffectId == BattleFieldEffectId.Smog
                && effect.TargetSide == targetSide);
            if (existing != null)
            {
                if (!ReferenceEquals(existing.Definition, definition))
                {
                    throw new InvalidOperationException(
                        "A Smog recast must use the same Definition.");
                }
                existing.AddValue(source, value);
                LogFieldEffectCreated(source, existing, targetSide);
                return existing;
            }

            var smog = new BattleFieldEffectInstance(
                definition,
                targetSide,
                source,
                value);
            _effects.Add(smog);
            LogFieldEffectCreated(source, smog, targetSide);
            return smog;
        }

        public BattleFieldEffectInstance CreateOrAddFireBarrier(
            BattleUnitState source,
            FireBarrierFieldEffectAsset definition,
            int value)
        {
            ValidateSource(source);
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));

            var hp = Math.Max(
                1,
                SignedStatMath.FloorNonNegative(
                    value * definition.ValueHpRatio / 100m));
            var durationTicks = Math.Max(
                1,
                SignedStatMath.CeilPositive(
                    value * definition.ValueDurationRatio / 100m));
            var defense = BattleDefenseSnapshot.Capture(
                source.GetBattleStats(),
                definition.DefenseSnapshotRatio);
            var existing = _effects.FirstOrDefault(effect =>
                effect.EffectId == BattleFieldEffectId.FireBarrier
                && effect.TargetSide == source.Side);
            if (existing != null)
            {
                existing.AddFireBarrierGeneration(
                    source,
                    value,
                    hp,
                    durationTicks,
                    defense);
                LogFieldEffectCreated(source, existing, source.Side);
                return existing;
            }

            var barrier = BattleFieldEffectInstance.CreateFireBarrier(
                definition,
                source.Side,
                source,
                value,
                hp,
                durationTicks,
                defense);
            _effects.Add(barrier);
            LogFieldEffectCreated(source, barrier, source.Side);
            return barrier;
        }

        public BattleFieldEffectInstance CreateFrozenGround(
            BattleUnitState source,
            FrozenGroundFieldEffectAsset definition)
        {
            ValidateSource(source);
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var existing = _effects.FirstOrDefault(effect =>
                effect.EffectId == BattleFieldEffectId.FrozenGround);
            if (existing != null)
            {
                if (!ReferenceEquals(existing.Definition, definition))
                {
                    throw new InvalidOperationException(
                        "Frozen Ground sources must use the same Definition.");
                }
                existing.AddFrozenGroundSource(source);
                LogFieldEffectCreated(source, existing, source.Side, isGlobal: true);
                return existing;
            }

            var field = new BattleFieldEffectInstance(
                definition,
                source.Side,
                source,
                value: 1);
            _effects.Add(field);
            LogFieldEffectCreated(source, field, source.Side, isGlobal: true);
            return field;
        }

        public BattleFieldEffectInstance CreateOrAddIceBlade(
            BattleUnitState source,
            IceBladeFieldEffectAsset definition,
            int durationTicks)
        {
            ValidateSource(source);
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (durationTicks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationTicks));
            }

            var existing = _effects.FirstOrDefault(effect =>
                effect.EffectId == BattleFieldEffectId.IceBlade
                && effect.TargetSide == source.Side);
            if (existing != null)
            {
                if (!ReferenceEquals(existing.Definition, definition))
                {
                    throw new InvalidOperationException(
                        "An Ice Blade recast must use the same Definition.");
                }
                existing.AddIceBladeDuration(source, durationTicks);
                LogFieldEffectCreated(source, existing, source.Side);
                return existing;
            }

            var field = BattleFieldEffectInstance.CreateIceBlade(
                definition,
                source.Side,
                source,
                durationTicks);
            _effects.Add(field);
            LogFieldEffectCreated(source, field, source.Side);
            return field;
        }

        public BattleFieldEffectInstance CreateOrAddWaterVeil(
            BattleUnitState source,
            WaterVeilFieldEffectAsset definition,
            int value)
        {
            ValidateSource(source);
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));

            var existing = _effects.FirstOrDefault(effect =>
                effect.EffectId == BattleFieldEffectId.WaterVeil
                && effect.TargetSide == source.Side);
            if (existing != null)
            {
                if (!ReferenceEquals(existing.Definition, definition))
                {
                    throw new InvalidOperationException(
                        "A Water Veil recast must use the same Definition.");
                }
                existing.AddValue(source, value);
                LogFieldEffectCreated(source, existing, source.Side);
                return existing;
            }

            var field = new BattleFieldEffectInstance(
                definition,
                source.Side,
                source,
                value);
            _effects.Add(field);
            LogFieldEffectCreated(source, field, source.Side);
            return field;
        }

        public BattleFieldEffectInstance CreatePoisonMist(
            BattleUnitState source,
            PoisonMistFieldEffectAsset definition,
            int value,
            int durationTicks)
        {
            ValidateSource(source);
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (durationTicks <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationTicks));

            var mist = BattleFieldEffectInstance.CreatePoisonMist(
                definition,
                source.Side,
                source,
                value,
                durationTicks);
            _effects.Add(mist);
            LogFieldEffectCreated(source, mist, source.Side);
            return mist;
        }

        public bool TryEvadeSkillAttack(
            BattleUnitState source,
            BattleUnitState target,
            DamageOriginKind originKind,
            decimal preDefenseDamage,
            SkillHit hit)
        {
            ValidateSource(source);
            ValidateSource(target);
            if (preDefenseDamage < 0m)
                throw new ArgumentOutOfRangeException(nameof(preDefenseDamage));
            if (hit == null
                || hit.WasEvaded
                || originKind != DamageOriginKind.Skill
                || source.Side == target.Side)
            {
                return hit?.WasEvaded ?? false;
            }

            var mist = Effects.FirstOrDefault(effect =>
                effect.EffectId == BattleFieldEffectId.PoisonMist
                && effect.TargetSide == target.Side
                && preDefenseDamage <= effect.Value);
            if (mist == null) return false;
            hit.Evade();
            _state.AddLog($"{target.DisplayName}は{mist.DisplayName}で攻撃を回避した！");
            return true;
        }

        private void LogFieldEffectCreated(
            BattleUnitState source,
            BattleFieldEffectInstance effect,
            BattleSide targetSide,
            bool isGlobal = false)
        {
            var location = isGlobal
                ? "フィールドに"
                : targetSide == source.Side
                    ? "自陣に"
                    : "敵陣に";
            _state.AddLog($"{location}{effect.DisplayName}を生成した！");
        }

        public decimal ApplyIncomingAttributeDamageReduction(
            BattleUnitState target,
            PachimonAttribute attribute,
            decimal damage)
        {
            ValidateSource(target);
            if (damage < 0m) throw new ArgumentOutOfRangeException(nameof(damage));
            if (attribute is not (PachimonAttribute.Aqua or PachimonAttribute.Fire))
            {
                return damage;
            }

            var veil = _effects.FirstOrDefault(effect =>
                effect.EffectId == BattleFieldEffectId.WaterVeil
                && effect.TargetSide == target.Side
                && !effect.IsExpired);
            return veil?.Definition is WaterVeilFieldEffectAsset definition
                ? damage * Math.Max(0, 100 - definition.DamageReductionPercent)
                    / 100m
                : damage;
        }

        public void HandleStatusValueApplied(StatusValueAppliedEvent statusEvent)
        {
            if (statusEvent == null)
            {
                throw new ArgumentNullException(nameof(statusEvent));
            }
            if (statusEvent.StatusId != BattleStatusId.Chill
                || statusEvent.AppliedValue <= 0)
            {
                return;
            }

            foreach (var blade in _effects
                         .Where(effect =>
                             effect.EffectId == BattleFieldEffectId.IceBlade
                             && effect.TargetSide != statusEvent.Target.Side
                             && !effect.IsExpired)
                         .ToArray())
            {
                _state.AddLog("氷の刃の攻撃！");
                BattleAttributeDamageService.Apply(
                    _state,
                    blade.Source,
                    statusEvent.Target,
                    new DamageContext(
                        DamageOriginKind.Field,
                        (int)BattleFieldEffectId.IceBlade,
                        statusEvent.AppliedValue,
                        blade.Source.GetBattleStats(),
                        statusEvent.Target.GetBattleStats(),
                        PachimonAttribute.Ice,
                        isAttack: false,
                        applyAttackerAttributeMultiplier: false,
                        applyDamageBonusMultiplier: false,
                        applyOutgoingModifiers: false));
            }
        }

        public void HandleAttributeDamageApplied(
            AttributeDamageAppliedEvent damageEvent)
        {
            if (damageEvent == null)
                throw new ArgumentNullException(nameof(damageEvent));
            if (damageEvent.Source == null
                || damageEvent.AppliedDamage
                    + damageEvent.ShieldAbsorbedDamage <= 0
                || damageEvent.Calculation.Context.OriginKind
                    == DamageOriginKind.Field
                || damageEvent.Attribute is not (
                    PachimonAttribute.Fire or PachimonAttribute.Leaf))
            {
                return;
            }

            foreach (var vine in Effects.Where(effect =>
                         effect.EffectId == BattleFieldEffectId.FireVine
                         && effect.TargetSide == damageEvent.Source.Side)
                     .ToArray())
            {
                if (!damageEvent.Target.IsAlive) break;
                AttackPlantAndRespond(
                    vine,
                    damageEvent.Target,
                    damageBonusPercent: 0);
            }
        }

        public bool TryTransformChillToFreeze(BattleUnitState target)
        {
            ValidateSource(target);
            if (!target.IsAlive)
            {
                return false;
            }

            var field = _effects
                .Where(effect =>
                    effect.EffectId == BattleFieldEffectId.FrozenGround
                    && !effect.IsExpired)
                .FirstOrDefault();
            var chill = target.GetStatus(BattleStatusId.Chill);
            if (field == null || chill == null)
            {
                return false;
            }

            var definition = (FrozenGroundFieldEffectAsset)field.Definition;
            var threshold = definition.CalculateFreezeThreshold(field.Value);
            if (chill.Value < threshold)
            {
                return false;
            }

            var freezeValue = chill.Value;
            _state.Statuses.TryConsumeStatus(
                target,
                BattleStatusId.Chill,
                out _);
            _state.Statuses.ApplyTransformedStatus(
                target,
                BattleStatusFactory.CreateFreeze(
                    field.GetActiveFrozenGroundSource(),
                    freezeValue,
                    definition.FreezeStatus));
            _state.AddLog(
                $"{target.DisplayName}の冷気が凍結に変化した！");
            return true;
        }

        public BattleFieldInterceptionResult InterceptAttributeAttack(
            BattleUnitState source,
            BattleUnitState target,
            PachimonAttribute attribute,
            decimal preDefenseDamage,
            DamageOriginKind originKind,
            int originId)
        {
            ValidateSource(source);
            ValidateSource(target);
            if (preDefenseDamage < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(preDefenseDamage));
            }
            var barrier = GetAttackBarrier(source, target);
            if (barrier == null)
            {
                return default;
            }

            var reducedDamage = preDefenseDamage
                * SignedStatMath.ReductionMultiplier(
                    barrier.DefenseSnapshot.GetAttribute(attribute))
                * SignedStatMath.ReductionMultiplier(
                    barrier.GetEffectiveResistBonus());
            return ApplyBarrierDamage(
                source,
                target,
                barrier,
                reducedDamage,
                originKind,
                originId,
                attribute);
        }

        public BattleFieldInterceptionResult InterceptTrueAttack(
            BattleUnitState source,
            BattleUnitState target,
            int damage,
            DamageOriginKind originKind,
            int originId)
        {
            ValidateSource(source);
            ValidateSource(target);
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            var barrier = GetAttackBarrier(source, target);
            return barrier == null
                ? default
                : ApplyBarrierDamage(
                    source,
                    target,
                    barrier,
                    damage,
                    originKind,
                    originId,
                    attribute: null);
        }

        public BattleFieldEffectInstance InterceptStatusAttack(
            BattleUnitState source,
            BattleUnitState target,
            DamageOriginKind originKind)
        {
            ValidateSource(source);
            ValidateSource(target);
            if (originKind != DamageOriginKind.Skill)
            {
                return null;
            }

            var barrier = GetAttackBarrier(source, target);
            if (barrier == null)
            {
                return null;
            }

            return barrier;
        }

        public bool TryApplyStatus(
            BattleFieldEffectInstance effect,
            BattleStatusInstance status)
        {
            if (effect == null) throw new ArgumentNullException(nameof(effect));
            if (status == null) throw new ArgumentNullException(nameof(status));
            if (!_effects.Contains(effect) || effect.IsExpired)
            {
                return false;
            }
            if (status.StatusId is not (
                    BattleStatusId.Toxin
                    or BattleStatusId.Weakness
                    or BattleStatusId.WindErosion))
            {
                _state.AddLog($"{effect.DisplayName}が状態攻撃を防いだ！");
                return false;
            }

            var reduced = ReduceFieldStatus(effect, status);
            var appliedValue = effect.AddOrMergeStatus(reduced);
            if (appliedValue <= 0)
            {
                return false;
            }

            var statusName = status.Definition?.DisplayName
                ?? status.StatusId.ToString();
            _state.AddLog(
                $"{effect.DisplayName}に{appliedValue}の{statusName}を与えた！");
            var source = status.Source ?? ResolveToxinSource(status);
            if (source != null)
            {
                _state.Events.Publish(new FieldEffectStatusAppliedEvent(
                    _state,
                    source,
                    effect,
                    status.StatusId,
                    appliedValue));
            }
            NotifyContextChanged();
            return true;
        }

        internal bool TryConsumeStatus(
            BattleFieldEffectInstance effect,
            BattleStatusId statusId,
            out BattleStatusInstance status)
        {
            if (effect == null) throw new ArgumentNullException(nameof(effect));
            if (!_effects.Contains(effect))
            {
                status = null;
                return false;
            }
            var consumed = effect.TryConsumeStatus(statusId, out status);
            if (consumed)
            {
                NotifyContextChanged();
            }
            return consumed;
        }

        public int RemoveShieldEffects(BattleSide side)
        {
            var barriers = _effects
                .Where(effect => effect.EffectId == BattleFieldEffectId.FireBarrier
                    && effect.TargetSide == side
                    && !effect.IsExpired)
                .ToArray();
            var removedHp = barriers.Sum(barrier => barrier.CurrentHp);
            foreach (var barrier in barriers)
            {
                _effects.Remove(barrier);
            }
            if (barriers.Length > 0)
            {
                NotifyContextChanged();
            }
            return removedHp;
        }

        internal void AdvanceTime(int ticks)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
            for (var elapsed = 0; elapsed < ticks; elapsed++)
            {
                AdvanceOneTick();
            }
        }

        internal void CopyForSimulation(
            BattleFieldRuntime original,
            IReadOnlyDictionary<BattleUnitState, BattleUnitState> unitMap)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (unitMap == null) throw new ArgumentNullException(nameof(unitMap));
            _effects.Clear();
            foreach (var effect in original.Effects)
            {
                _effects.Add(effect.CreateSimulationClone(unitMap));
            }
        }

        private void AdvanceOneTick()
        {
            foreach (var effect in _effects.ToArray())
            {
                if (effect.EffectId != BattleFieldEffectId.Smog)
                {
                    if (effect.EffectId == BattleFieldEffectId.FireBarrier)
                    {
                        AdvanceFireBarrierStatusesOneTick(effect);
                        if (effect.IsExpired)
                        {
                            _effects.Remove(effect);
                            _state.AddLog($"{effect.DisplayName}は壊れた！");
                            continue;
                        }
                        effect.AdvanceFireBarrierOneTick();
                        if (effect.IsExpired)
                        {
                            _effects.Remove(effect);
                        }
                    }
                    else if (effect.EffectId == BattleFieldEffectId.IceBlade)
                    {
                        effect.AdvanceIceBladeOneTick();
                        if (effect.IsExpired)
                        {
                            _effects.Remove(effect);
                        }
                    }
                    else if (effect.EffectId == BattleFieldEffectId.PoisonMist)
                    {
                        effect.AdvancePoisonMistOneTick();
                        if (effect.IsExpired)
                            _effects.Remove(effect);
                    }
                    else if (effect.EffectId == BattleFieldEffectId.WaterVeil)
                    {
                        var healing = effect.AdvanceWaterVeilOneTick();
                        var side = effect.TargetSide == BattleSide.Player
                            ? _state.Player
                            : _state.Enemy;
                        foreach (var target in side.GetAllLiving().ToArray())
                        {
                            _state.SupportEffects.RestoreHp(
                                effect.Source,
                                target,
                                healing,
                                isSharedEffect: true);
                        }
                        if (effect.IsExpired)
                        {
                            _effects.Remove(effect);
                        }
                    }
                    else if (effect.EffectId == BattleFieldEffectId.BeatVine)
                    {
                        if (effect.AdvanceBeatVineOneTick())
                        {
                            var target = _state.GetOpposingSide(effect.TargetSide)
                                .GetFrontLiving();
                            if (target != null)
                            {
                                AttackPlantAndRespond(
                                    effect,
                                    target,
                                    damageBonusPercent: 0);
                            }
                        }
                    }
                    continue;
                }

                var appliedValue = effect.AdvanceSmogOneTick();
                if (appliedValue > 0)
                {
                    var targetSide = effect.TargetSide == BattleSide.Player
                        ? _state.Player
                        : _state.Enemy;
                    foreach (var target in targetSide.GetAllLiving().ToArray())
                    {
                        _state.Statuses.ApplyStatus(
                            target,
                            BattleStatusFactory.CreateToxin(
                                effect.Source,
                                appliedValue,
                                ((SmogFieldEffectAsset)effect.Definition)
                                    .ToxinStatus));
                    }
                }

                if (effect.IsExpired)
                {
                    _effects.Remove(effect);
                }
            }
        }

        private void ApplyPlantDamage(
            BattleFieldEffectInstance plant,
            BattleUnitState target,
            int damage,
            PachimonAttribute attribute)
        {
            if (damage <= 0 || !target.IsAlive) return;
            BattleAttributeDamageService.Apply(
                _state,
                plant.Source,
                target,
                new DamageContext(
                    DamageOriginKind.Field,
                    (int)plant.EffectId,
                    damage,
                    plant.Source.GetBattleStats(),
                    target.GetBattleStats(),
                    attribute,
                    isAttack: false,
                    applyAttackerAttributeMultiplier: false,
                    applyDamageBonusMultiplier: false,
                    applyOutgoingModifiers: false));
        }

        private void AttackPlant(
            BattleFieldEffectInstance plant,
            BattleUnitState target,
            int damageBonusPercent)
        {
            if (plant == null) throw new ArgumentNullException(nameof(plant));
            if (target == null) throw new ArgumentNullException(nameof(target));
            var multiplier = 1m + damageBonusPercent / 100m;
            _state.AddLog($"{plant.DisplayName}の攻撃！");
            switch (plant.EffectId)
            {
                case BattleFieldEffectId.FireVine:
                    ApplyPlantDamage(plant, target,
                        SignedStatMath.FloorNonNegative(plant.Value * multiplier),
                        PachimonAttribute.Leaf);
                    if (target.IsAlive)
                    {
                        ApplyPlantDamage(plant, target,
                            SignedStatMath.FloorNonNegative(
                                plant.SecondaryValue * multiplier),
                            PachimonAttribute.Fire);
                    }
                    break;
                default:
                    ApplyPlantDamage(plant, target,
                        SignedStatMath.FloorNonNegative(plant.Value * multiplier),
                        PachimonAttribute.Leaf);
                    break;
            }
        }

        private void AttackPlantAndRespond(
            BattleFieldEffectInstance plant,
            BattleUnitState target,
            int damageBonusPercent)
        {
            AttackPlant(plant, target, damageBonusPercent);
            if (plant.EffectId == BattleFieldEffectId.ResponsivePlant)
                return;

            foreach (var responder in Effects.Where(effect =>
                         effect.TargetSide == plant.TargetSide
                         && effect.EffectId
                             == BattleFieldEffectId.ResponsivePlant)
                     .ToArray())
            {
                var responseTarget = _state
                    .GetOpposingSide(plant.TargetSide)
                    .GetFrontLiving();
                if (responseTarget == null) return;
                AttackPlant(responder, responseTarget, damageBonusPercent);
            }
        }

        private void HandleFieldEffectDamageApplied(
            FieldEffectDamageAppliedEvent damageEvent)
        {
            if (damageEvent == null)
                throw new ArgumentNullException(nameof(damageEvent));
            if (damageEvent.Source == null
                || damageEvent.ProtectedTarget == null
                || !damageEvent.ProtectedTarget.IsAlive
                || damageEvent.AppliedDamage <= 0
                || damageEvent.OriginKind == DamageOriginKind.Field
                || damageEvent.Attribute is not (
                    PachimonAttribute.Fire or PachimonAttribute.Leaf))
            {
                return;
            }

            foreach (var vine in Effects.Where(effect =>
                         effect.EffectId == BattleFieldEffectId.FireVine
                         && effect.TargetSide == damageEvent.Source.Side)
                     .ToArray())
            {
                if (!damageEvent.ProtectedTarget.IsAlive)
                {
                    break;
                }
                AttackPlantAndRespond(
                    vine,
                    damageEvent.ProtectedTarget,
                    damageBonusPercent: 0);
            }
        }

        private void AdvanceFireBarrierStatusesOneTick(
            BattleFieldEffectInstance barrier)
        {
            var toxin = barrier.GetStatus(BattleStatusId.Toxin);
            if (toxin?.Definition is ToxinStatusAsset toxinDefinition
                && toxin.Value > 0)
            {
                var baseDamage = toxin.Value
                    * toxinDefinition.DamagePerTickRatio / 100m;
                var unroundedDamage = baseDamage
                    * SignedStatMath.ReductionMultiplier(
                        barrier.DefenseSnapshot.GetAttribute(
                            PachimonAttribute.Poison))
                    * SignedStatMath.ReductionMultiplier(
                        barrier.GetEffectiveResistBonus());
                var decay = toxin.Value
                    * toxinDefinition.DecayPerTickRatio / 100m;
                var tick = toxin.AccumulateToxinTick(
                    unroundedDamage,
                    decay);
                if (tick.Damage > 0)
                {
                    var overflow = barrier.ApplyFireBarrierDamage(tick.Damage);
                    var applied = tick.Damage - overflow;
                    if (applied > 0)
                    {
                        _state.Events.Publish(new FieldEffectDamageAppliedEvent(
                            _state,
                            source: null,
                            protectedTarget: null,
                            barrier,
                            DamageOriginKind.Status,
                            (int)BattleStatusId.Toxin,
                            PachimonAttribute.Poison,
                            applied));
                    }
                }
            }

            var erosion = barrier.GetStatus(BattleStatusId.WindErosion);
            if (erosion != null)
            {
                var decay = erosion.Definition is WindErosionStatusAsset definition
                    ? definition.DecayPerTick
                    : 1;
                erosion.DecayValue(decay);
            }
            barrier.RemoveExpiredStatuses();
        }

        private void NotifyContextChanged()
        {
            foreach (var unit in _state.Player.Units.Concat(_state.Enemy.Units))
                unit.NotifyBattleContextChanged();
        }

        private BattleStatusInstance ReduceFieldStatus(
            BattleFieldEffectInstance effect,
            BattleStatusInstance status)
        {
            PachimonAttribute? defenseAttribute = status.StatusId switch
            {
                BattleStatusId.Toxin => PachimonAttribute.Poison,
                BattleStatusId.WindErosion => PachimonAttribute.Wind,
                _ => null,
            };
            if (!defenseAttribute.HasValue || status.Value <= 0)
            {
                return status;
            }

            var multiplier = SignedStatMath.ReductionMultiplier(
                effect.DefenseSnapshot.GetAttribute(defenseAttribute.Value));
            if (status.StatusId == BattleStatusId.Toxin)
            {
                var reducedToxin = new BattleStatusInstance(
                    BattleStatusId.Toxin,
                    status.Categories,
                    source: null,
                    value: 0,
                    status.StackCount,
                    status.RemainingTicks,
                    status.RuntimeData,
                    status.Definition);
                foreach (var application in status.ToxinApplications)
                {
                    var value = SignedStatMath.FloorNonNegative(
                        application.AppliedValue * multiplier);
                    if (value > 0)
                    {
                        reducedToxin.AddToxinApplication(
                            new ToxinApplicationRecord(
                                application.SourceInstanceId,
                                application.SourceDisplayName,
                                value));
                    }
                }
                return reducedToxin;
            }

            return new BattleStatusInstance(
                status.StatusId,
                status.Categories,
                status.Source,
                SignedStatMath.FloorNonNegative(status.Value * multiplier),
                status.StackCount,
                status.RemainingTicks,
                status.RuntimeData,
                status.Definition);
        }

        private BattleUnitState ResolveToxinSource(BattleStatusInstance toxin)
        {
            var sourceId = toxin.ToxinApplications
                .FirstOrDefault()?.SourceInstanceId;
            return sourceId == null
                ? null
                : _state.Player.Units.Concat(_state.Enemy.Units)
                    .FirstOrDefault(unit => unit.InstanceId == sourceId);
        }

        internal BattleFieldEffectInstance GetAttackBarrier(
            BattleUnitState source,
            BattleUnitState target)
        {
            if (source.Side == target.Side)
            {
                return null;
            }
            return _effects.FirstOrDefault(effect =>
                effect.EffectId == BattleFieldEffectId.FireBarrier
                && effect.TargetSide == target.Side
                && !effect.IsExpired);
        }

        private BattleFieldInterceptionResult ApplyBarrierDamage(
            BattleUnitState attacker,
            BattleUnitState protectedTarget,
            BattleFieldEffectInstance barrier,
            decimal unroundedDamage,
            DamageOriginKind originKind,
            int originId,
            PachimonAttribute? attribute)
        {
            var incomingDamage = AttributeDamageCalculator.FinalizeNormalDamage(
                unroundedDamage);
            var overflow = barrier.ApplyFireBarrierDamage(incomingDamage);
            var absorbed = incomingDamage - overflow;
            var damageEvent = new FieldEffectDamageAppliedEvent(
                _state,
                attacker,
                protectedTarget,
                barrier,
                originKind,
                originId,
                attribute,
                absorbed);
            if (barrier.Definition is FireBarrierFieldEffectAsset definition)
            {
                var burnValue = SignedStatMath.FloorNonNegative(
                    barrier.Value * definition.ValueBurnRatio / 100m);
                if (burnValue > 0)
                {
                    _state.Statuses.ApplyStatus(
                        attacker,
                        BattleStatusFactory.CreateBurn(
                            barrier.Source,
                            burnValue,
                            definition.BurnStatus));
                }
            }

            _state.AddLog($"{barrier.DisplayName}が{absorbed}のDamageを受けた！");
            if (barrier.IsExpired)
            {
                _effects.Remove(barrier);
                _state.AddLog($"{barrier.DisplayName}は壊れた！");
            }
            _state.Events.Publish(damageEvent);
            HandleFieldEffectDamageApplied(damageEvent);
            return new BattleFieldInterceptionResult(
                barrier,
                incomingDamage,
                absorbed,
                overflow);
        }

        private void ValidateSource(BattleUnitState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!_state.Player.Units.Contains(source)
                && !_state.Enemy.Units.Contains(source))
            {
                throw new ArgumentException(
                    "The Field Effect source does not belong to this Battle.",
                    nameof(source));
            }
        }
    }
}
