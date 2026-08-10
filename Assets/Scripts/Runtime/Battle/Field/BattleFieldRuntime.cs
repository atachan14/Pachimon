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

        internal BattleFieldEffectInstance(
            BattleFieldEffectAsset definition,
            BattleSide targetSide,
            BattleUnitState source,
            int value)
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
        public BattleFieldEffectAsset Definition { get; private set; }
        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int? RemainingTicks { get; private set; }
        public BattleDefenseSnapshot DefenseSnapshot { get; private set; }
        public decimal ApplicationWork { get; private set; }
        public decimal DecayWork { get; private set; }
        public bool IsExpired => Value <= 0
            || (EffectId == BattleFieldEffectId.FrozenGround
                && !_frozenGroundSources.Any(source => source.IsAlive))
            || (EffectId == BattleFieldEffectId.IceBlade
                && RemainingTicks <= 0)
            || (EffectId == BattleFieldEffectId.FireBarrier
                && (CurrentHp <= 0 || RemainingTicks <= 0));

        public string DisplayName => Definition?.DisplayName ?? EffectId switch
        {
            BattleFieldEffectId.Smog => "スモッグ",
            BattleFieldEffectId.FireBarrier => "炎の障壁",
            BattleFieldEffectId.FrozenGround => "氷の大地",
            BattleFieldEffectId.IceBlade => "氷の刃",
            _ => EffectId.ToString(),
        };

        public string Description => Definition?.Description ?? string.Empty;

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
                return new BattleFieldEffectInstance(
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
                return clone;
            }

            if (EffectId == BattleFieldEffectId.IceBlade)
            {
                return CreateIceBlade(
                    (IceBladeFieldEffectAsset)Definition,
                    TargetSide,
                    sourceClone,
                    RemainingTicks.GetValueOrDefault());
            }

            return new BattleFieldEffectInstance(
                Definition,
                TargetSide,
                sourceClone,
                Value)
            {
                ApplicationWork = ApplicationWork,
                DecayWork = DecayWork,
            };
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
            decimal preDefenseDamage)
        {
            ValidateSource(source);
            ValidateSource(target);
            if (preDefenseDamage < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(preDefenseDamage));
            }
            var barrier = FindEnemyAttackBarrier(source, target);
            if (barrier == null)
            {
                return default;
            }

            var reducedDamage = preDefenseDamage
                * SignedStatMath.ReductionMultiplier(
                    barrier.DefenseSnapshot.GetAttribute(attribute))
                * SignedStatMath.ReductionMultiplier(
                    barrier.DefenseSnapshot.ResistBonus);
            return ApplyBarrierDamage(source, barrier, reducedDamage);
        }

        public BattleFieldInterceptionResult InterceptTrueAttack(
            BattleUnitState source,
            BattleUnitState target,
            int damage)
        {
            ValidateSource(source);
            ValidateSource(target);
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            var barrier = FindEnemyAttackBarrier(source, target);
            return barrier == null
                ? default
                : ApplyBarrierDamage(source, barrier, damage);
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

        private BattleFieldEffectInstance FindEnemyAttackBarrier(
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
            BattleFieldEffectInstance barrier,
            decimal unroundedDamage)
        {
            var incomingDamage = AttributeDamageCalculator.FinalizeNormalDamage(
                unroundedDamage);
            var overflow = barrier.ApplyFireBarrierDamage(incomingDamage);
            var absorbed = incomingDamage - overflow;
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
