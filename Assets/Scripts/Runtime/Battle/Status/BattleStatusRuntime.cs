using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Reward;
using Pachimon.Run;

namespace Pachimon.Battle
{
    public readonly struct SkillStatusConsumptionSnapshot
    {
        public SkillStatusConsumptionSnapshot(
            int burnValue,
            BattleStatusInstance launchCeremony,
            int oneTwoValue)
        {
            if (burnValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(burnValue));
            }
            if (oneTwoValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(oneTwoValue));
            }

            BurnValue = burnValue;
            LaunchCeremony = launchCeremony;
            OneTwoValue = oneTwoValue;
        }

        public int BurnValue { get; }
        public BattleStatusInstance LaunchCeremony { get; }
        public int OneTwoValue { get; }
    }

    public sealed class BattleStatusRuntime
    {
        private const int MaxReactionDepth = 32;

        private readonly BattleState _state;
        private readonly List<SkillEffectResult> _collectedEffects = new();
        private bool _isCollecting;
        private int _reactionDepth;

        public BattleStatusRuntime(BattleState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void ApplyStatus(
            BattleUnitState target,
            BattleStatusInstance status)
        {
            ApplyStatusCore(
                target,
                status,
                reduceIncomingValue: true,
                logAttackApplication: false);
        }

        public void ApplyAttackStatus(
            BattleUnitState target,
            BattleStatusInstance status)
        {
            ValidateTarget(target);
            if (status == null) throw new ArgumentNullException(nameof(status));
            if (status.Source != null && status.Source.Side != target.Side)
            {
                var hit = new SkillHit(
                    _state,
                    status.Source,
                    target,
                    DamageOriginKind.Status,
                    (int)status.StatusId);
                ApplyAttackStatus(hit, status);
                return;
            }

            ApplyStatusCore(
                target,
                status,
                reduceIncomingValue: true,
                logAttackApplication: true);
        }

        public void HandleAttackReceived(AttackReceivedEvent attackEvent)
        {
            if (attackEvent == null)
                throw new ArgumentNullException(nameof(attackEvent));
            if (attackEvent.OriginKind == DamageOriginKind.Self)
                return;

            foreach (var status in attackEvent.Target
                         .GetStatuses(BattleStatusId.ElectricShield)
                         .ToArray())
            {
                if (status.RuntimeData is not ElectricShieldRuntimeData runtime
                    || status.Definition is not ElectricShieldStatusAsset definition
                    || !attackEvent.ActiveShieldApplicationOrders.Contains(
                        runtime.ShieldApplicationOrder))
                {
                    continue;
                }

                ApplyStatus(
                    attackEvent.Source,
                    BattleStatusFactory.CreateSlow(
                        attackEvent.Target,
                        status.Value,
                        definition.ParalysisStatus));
                _state.AddLog(
                    $"{attackEvent.Source.DisplayName}に麻痺を{status.Value}付与した！");

                if (!attackEvent.Target.Shields.Any(shield =>
                        shield.ApplicationOrder == runtime.ShieldApplicationOrder))
                {
                    attackEvent.Target.TryRemoveStatusInstance(status);
                }
            }
        }

        internal bool ApplyAttackStatus(
            SkillHit hit,
            BattleStatusInstance status)
        {
            if (hit == null) throw new ArgumentNullException(nameof(hit));
            if (status == null) throw new ArgumentNullException(nameof(status));
            if (!ReferenceEquals(hit.State, _state))
            {
                throw new ArgumentException(
                    "SkillHit belongs to another Battle.",
                    nameof(hit));
            }
            if (hit.WasEvaded)
                return false;
            if (!hit.DamageWasResolved)
            {
                var barrier = _state.Fields.InterceptStatusAttack(
                    hit.Source,
                    hit.Target,
                    hit.OriginKind);
                if (barrier != null)
                {
                    hit.BlockStatus(barrier);
                }
            }
            if (!hit.CanApplyStatus)
            {
                return hit.InterceptedFieldEffect != null
                    && _state.Fields.TryApplyStatus(
                        hit.InterceptedFieldEffect,
                        status);
            }

            ApplyStatusCore(
                hit.Target,
                status,
                reduceIncomingValue: true,
                logAttackApplication: true);
            return true;
        }

        internal void ApplyTransformedStatus(
            BattleUnitState target,
            BattleStatusInstance status)
        {
            ApplyStatusCore(
                target,
                status,
                reduceIncomingValue: false,
                logAttackApplication: false);
        }

        private void ApplyStatusCore(
            BattleUnitState target,
            BattleStatusInstance status,
            bool reduceIncomingValue,
            bool logAttackApplication)
        {
            ValidateTarget(target);
            if (status == null) throw new ArgumentNullException(nameof(status));
            if (reduceIncomingValue)
            {
                status = ApplyIncomingValueReduction(target, status);
            }
            if (status.Value == 0
                && (status.Categories
                    & (BattleStatusCategory.Slow
                        | BattleStatusCategory.Leak
                        | BattleStatusCategory.Toxin)) != 0)
            {
                return;
            }
            if (status.StatusId == BattleStatusId.Freeze && status.Value == 0)
            {
                return;
            }

            if ((status.Categories & BattleStatusCategory.Toxin) != 0)
            {
                var applications = status.ToxinApplications.ToArray();
                var existing = target.GetStatus(BattleStatusId.Toxin);
                if (existing != null)
                {
                    if (!ReferenceEquals(existing.Definition, status.Definition))
                    {
                        throw new InvalidOperationException(
                            "A Toxin reapplication must use the same Definition.");
                    }
                    foreach (var application in applications)
                    {
                        existing.AddToxinApplication(application);
                    }
                    target.NotifyStatusValueChanged();
                }
                else
                {
                    target.AddStatusInstance(status);
                }

                PublishToxinAppliedEvents(target, applications);
            }
            else if ((status.Categories & BattleStatusCategory.Slow) != 0)
            {
                var existing = target.GetStatus(status.StatusId);
                if (existing != null)
                {
                    existing.AddValue(status.Value);
                    target.NotifyStatusValueChanged();
                }
                else
                {
                    target.AddStatusInstance(status);
                }
            }
            else if ((status.Categories & BattleStatusCategory.Leak) != 0)
            {
                var existing = target.GetStatus(status.StatusId);
                if (existing != null)
                {
                    existing.AddValue(status.Value);
                    target.NotifyStatusValueChanged();
                }
                else
                {
                    target.AddStatusInstance(status);
                }
            }
            else if ((status.Categories & BattleStatusCategory.Burn) != 0)
            {
                var existing = target.GetStatus(BattleStatusId.Burn);
                if (existing != null)
                {
                    existing.AddValue(status.Value);
                    target.NotifyStatusValueChanged();
                }
                else
                {
                    target.AddStatusInstance(status);
                }
            }
            else if (status.StatusId == BattleStatusId.WindErosion)
            {
                var existing = target.GetStatus(BattleStatusId.WindErosion);
                if (existing != null)
                {
                    existing.AddValue(status.Value);
                    target.NotifyStatusValueChanged();
                }
                else
                {
                    target.AddStatusInstance(status);
                }
            }
            else if (status.StatusId == BattleStatusId.DragonCranker)
            {
                var existing = target.GetStatus(BattleStatusId.DragonCranker);
                if (existing != null)
                {
                    existing.AddValue(status.Value);
                    target.NotifyStatusValueChanged();
                }
                else
                {
                    target.AddStatusInstance(status);
                }
            }
            else if (status.StatusId == BattleStatusId.OneTwo)
            {
                var existing = target.GetStatus(BattleStatusId.OneTwo);
                if (existing != null)
                {
                    existing.AddValue(status.Value);
                    target.NotifyStatusValueChanged();
                }
                else
                {
                    target.AddStatusInstance(status);
                }
            }
            else if (status.StatusId == BattleStatusId.Freeze)
            {
                var existing = target.GetStatus(BattleStatusId.Freeze);
                if (existing != null)
                {
                    existing.AddValue(status.Value);
                    if (status.RemainingTicks.HasValue)
                    {
                        existing.AddDuration(status.RemainingTicks.Value);
                    }
                    target.NotifyStatusValueChanged();
                }
                else
                {
                    target.AddStatusInstance(status);
                }
            }
            else if (status.IsTimed)
            {
                target.AddStatusInstance(status);
            }
            else
            {
                target.ApplyOrReplaceStatus(status);
            }
            RefreshActionClockPause(target);
            if ((status.Categories & BattleStatusCategory.Slow) != 0)
            {
                var appliedEvent = new StatusValueAppliedEvent(
                    _state,
                    status.Source,
                    target,
                    status.StatusId,
                    status.Value);
                _state.Events.Publish(appliedEvent);
                _state.Fields.HandleStatusValueApplied(appliedEvent);
            }
            if (logAttackApplication)
            {
                LogAndPublishSkillStatusApplication(target, status);
            }
            if (status.StatusId == BattleStatusId.Chill)
            {
                _state.Fields.TryTransformChillToFreeze(target);
            }
        }

        public bool TryEvadeAttack(
            BattleUnitState source,
            BattleUnitState target,
            DamageOriginKind originKind,
            int originId)
        {
            ValidateTarget(target);
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!target.TryConsumeStatus(BattleStatusId.Footwork, out _))
            {
                return false;
            }

            _state.Presentation.RecordLog($"{target.DisplayName}は攻撃を回避した！");
            _state.Events.Publish(new AttackEvadedEvent(
                _state,
                source,
                target,
                originKind,
                originId));
            return true;
        }

        public void ApplyIncomingDamageModifiers(
            BeforeAttributeDamageEvent damageEvent)
        {
            if (damageEvent == null)
                throw new ArgumentNullException(nameof(damageEvent));

            if (damageEvent.WeaknessValue > 0)
            {
                damageEvent.MultiplyDamage(
                    1m + damageEvent.WeaknessValue / 100m);
            }

            if (damageEvent.Calculation.Context.Attribute
                    == PachimonAttribute.Dragon
                && damageEvent.Target.TryConsumeStatus(
                    BattleStatusId.DragonCranker,
                    out var cranker))
            {
                damageEvent.MultiplyDamage(1m + cranker.Value / 100m);
            }
        }

        public int ClampIncomingDamage(BattleUnitState target, int damage)
        {
            ValidateTarget(target);
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            return damage > 0
                && target.GetStatus(BattleStatusId.Intangible) != null
                    ? 1
                    : damage;
        }

        public void HandleDamageApplied(DamageAppliedEvent damageEvent)
        {
            if (damageEvent == null)
                throw new ArgumentNullException(nameof(damageEvent));

            var knockout = damageEvent.Target.GetStatus(BattleStatusId.Knockout);
            if (knockout?.Definition is not KnockoutStatusAsset definition
                || damageEvent.FinalDamage <= 0)
            {
                return;
            }

            var extension = SignedStatMath.FloorNonNegative(
                damageEvent.FinalDamage
                * definition.DamageDurationRatio / 100m);
            knockout.AddRemainingTicks(extension);
            damageEvent.Target.NotifyStatusValueChanged();
        }

        public BattleUnitState ResolveAttackTarget(
            BattleUnitState source,
            BattleUnitState intendedTarget,
            bool isAttack)
        {
            ValidateTarget(intendedTarget);
            if (!isAttack
                || source == null
                || source.Side == intendedTarget.Side
                || intendedTarget.GetStatus(BattleStatusId.DragonDefense) != null)
            {
                return intendedTarget;
            }

            var side = intendedTarget.Side == BattleSide.Player
                ? _state.Player
                : _state.Enemy;
            var protector = side.Units
                .Where(unit => unit.IsAlive
                    && !ReferenceEquals(unit, intendedTarget)
                    && unit.GetStatus(BattleStatusId.DragonDefense) != null)
                .OrderBy(unit => unit.SlotIndex)
                .FirstOrDefault();
            if (protector == null)
                return intendedTarget;

            _state.Presentation.RecordLog(
                $"{protector.DisplayName}が{intendedTarget.DisplayName}をかばった！");
            return protector;
        }

        private void LogAndPublishSkillStatusApplication(
            BattleUnitState target,
            BattleStatusInstance status)
        {
            var statusName = status.Definition?.DisplayName
                ?? status.StatusId switch
                {
                    BattleStatusId.Leak => "漏電",
                    BattleStatusId.Stun => "Stun",
                    BattleStatusId.Slow => "Slow",
                    BattleStatusId.Paralysis => "麻痺",
                    BattleStatusId.Chill => "冷気",
                    BattleStatusId.Freeze => "凍結",
                    BattleStatusId.Toxin => "毒素",
                    BattleStatusId.Burn => "火傷",
                    _ => status.StatusId.ToString(),
                };
            if ((status.Categories & BattleStatusCategory.Toxin) != 0)
            {
                foreach (var application in status.ToxinApplications)
                {
                    var source = GetAllUnits().FirstOrDefault(unit =>
                        unit.InstanceId == application.SourceInstanceId);
                    if (source == null || application.AppliedValue <= 0)
                    {
                        continue;
                    }

                    _state.AddLog(
                        $"{target.DisplayName}に{application.AppliedValue}の"
                        + $"{statusName}を与えた！");
                    _state.Events.Publish(new SkillStatusAppliedEvent(
                        _state,
                        source,
                        target,
                        status.StatusId,
                        application.AppliedValue));
                }
                return;
            }

            var appliedValue = status.RemainingTicks ?? status.Value;
            if (appliedValue <= 0)
            {
                return;
            }

            _state.AddLog(
                $"{target.DisplayName}に{appliedValue}の{statusName}を与えた！");
            _state.Events.Publish(new SkillStatusAppliedEvent(
                _state,
                status.Source,
                target,
                status.StatusId,
                appliedValue));
        }

        private void PublishToxinAppliedEvents(
            BattleUnitState target,
            IEnumerable<ToxinApplicationRecord> applications)
        {
            foreach (var application in applications)
            {
                var source = GetAllUnits().FirstOrDefault(unit =>
                    unit.InstanceId == application.SourceInstanceId);
                if (source == null)
                {
                    throw new InvalidOperationException(
                        $"Toxin source '{application.SourceInstanceId}' does not belong to this Battle.");
                }

                _state.Events.Publish(new ToxinAppliedEvent(
                    _state,
                    source,
                    target,
                    application.AppliedValue));
            }
        }

        private static BattleStatusInstance ApplyIncomingValueReduction(
            BattleUnitState target,
            BattleStatusInstance status)
        {
            var defenseStat = (status.Definition as SlowStatusAsset)?.DefenseStat
                ?? status.StatusId switch
                {
                    BattleStatusId.Paralysis => PachimonStatType.Electric,
                    BattleStatusId.Chill => PachimonStatType.Ice,
                    BattleStatusId.Burn => PachimonStatType.Fire,
                    BattleStatusId.Toxin => PachimonStatType.Poison,
                    BattleStatusId.Freeze => PachimonStatType.Ice,
                    _ => (PachimonStatType?)null,
                };
            if (!defenseStat.HasValue || status.Value <= 0)
            {
                return status;
            }

            var multiplier = SignedStatMath.ReductionMultiplier(
                target.GetBattleStatValue(defenseStat.Value));
            if (status.StatusId == BattleStatusId.Toxin)
            {
                return CreateReducedToxin(status, multiplier);
            }

            var reducedValue = ReduceValue(status.Value, multiplier);
            int? reducedDuration = status.RemainingTicks;
            if (status.StatusId == BattleStatusId.Freeze
                && reducedDuration.HasValue)
            {
                reducedDuration = Math.Max(1, reducedValue);
            }
            return new BattleStatusInstance(
                status.StatusId,
                status.Categories,
                status.Source,
                reducedValue,
                status.StackCount,
                reducedDuration,
                status.RuntimeData,
                status.Definition);
        }

        private static BattleStatusInstance CreateReducedToxin(
            BattleStatusInstance status,
            decimal multiplier)
        {
            var reduced = new BattleStatusInstance(
                status.StatusId,
                status.Categories,
                source: null,
                value: 0,
                stackCount: status.StackCount,
                durationTicks: status.RemainingTicks,
                runtimeData: status.RuntimeData,
                definition: status.Definition);
            foreach (var application in status.ToxinApplications)
            {
                var reducedValue = ReduceValue(
                    application.AppliedValue,
                    multiplier);
                if (reducedValue == 0)
                {
                    continue;
                }

                reduced.AddToxinApplication(new ToxinApplicationRecord(
                    application.SourceInstanceId,
                    application.SourceDisplayName,
                    reducedValue));
            }

            return reduced;
        }

        private static int ReduceValue(int value, decimal multiplier)
        {
            return SignedStatMath.FloorNonNegative(value * multiplier);
        }

        public SkillStatusConsumptionSnapshot CaptureSkillStatusConsumption(
            BattleUnitState unit)
        {
            ValidateTarget(unit);
            return new SkillStatusConsumptionSnapshot(
                unit.GetStatus(BattleStatusId.Burn)?.Value ?? 0,
                unit.GetStatus(BattleStatusId.LaunchCeremony),
                unit.GetStatus(BattleStatusId.OneTwo)?.Value ?? 0);
        }

        public void CompleteSkillStatusConsumption(
            BattleUnitState unit,
            SkillStatusConsumptionSnapshot snapshot)
        {
            ValidateTarget(unit);
            ReduceStatusValue(
                unit,
                BattleStatusId.Burn,
                snapshot.BurnValue);
            if (snapshot.LaunchCeremony != null)
            {
                unit.TryRemoveStatusInstance(snapshot.LaunchCeremony);
            }
            ReduceStatusValue(
                unit,
                BattleStatusId.OneTwo,
                snapshot.OneTwoValue);
        }

        public bool TryConsumeStatus(
            BattleUnitState target,
            BattleStatusId statusId,
            out BattleStatusInstance status)
        {
            ValidateTarget(target);
            var consumed = target.TryConsumeStatus(statusId, out status);
            if (consumed)
            {
                RefreshActionClockPause(target);
            }

            return consumed;
        }

        public int ReduceStatusValue(
            BattleUnitState target,
            BattleStatusId statusId,
            int amount)
        {
            ValidateTarget(target);
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            var status = target.GetStatus(statusId);
            if (status == null || amount == 0)
            {
                return 0;
            }

            var removed = Math.Min(status.Value, amount);
            status.DecayValue(removed);
            if (status.Value == 0 || status.IsExpired)
            {
                TryConsumeStatus(target, statusId, out _);
            }
            else
            {
                target.NotifyStatusValueChanged();
            }

            return removed;
        }

        public int GetNextExpirationTicks()
        {
            return GetAllUnits()
                .SelectMany(unit =>
                    unit.Statuses
                        .Where(status => status.RemainingTicks.HasValue)
                        .Select(status => status.RemainingTicks.Value)
                        .Concat(unit.Shields
                            .Where(shield => shield.RemainingTicks.HasValue)
                            .Select(shield => shield.RemainingTicks.Value)))
                .DefaultIfEmpty(int.MaxValue)
                .Min();
        }

        internal void AdvanceTime(int ticks)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
            if (ticks == 0)
            {
                return;
            }

            foreach (var unit in GetAllUnits())
            {
                AdvanceToxinOneTick(unit);
                AdvanceFrozenBreak(unit, ticks);
                unit.AdvanceShields(ticks);
                unit.AdvanceStatuses(ticks);
                RefreshActionClockPause(unit);
            }
        }

        private void AdvanceToxinOneTick(BattleUnitState target)
        {
            if (!target.IsAlive)
            {
                return;
            }

            var toxin = target.GetStatus(BattleStatusId.Toxin);
            if (toxin == null || toxin.Value <= 0)
            {
                return;
            }

            var definition = toxin.Definition as ToxinStatusAsset
                ?? throw new InvalidOperationException(
                    "Toxin requires a Toxin Status Definition.");
            var damageAmount = toxin.Value
                * definition.DamagePerTickRatio / 100m;
            var unroundedDamage = BattleStatusDamageService.CalculateUnrounded(
                damageAmount,
                target,
                PachimonAttribute.Poison);
            var tick = toxin.AccumulateToxinTick(
                unroundedDamage,
                definition.DecayPerTick);
            if (tick.Decay > 0)
            {
                target.NotifyStatusValueChanged();
            }

            if (tick.Damage > 0)
            {
                BattleStatusDamageService.Apply(
                    _state,
                    target,
                    BattleStatusId.Toxin,
                    PachimonAttribute.Poison,
                    tick.Damage);
            }
        }

        public bool TryCompleteCharge(
            BattleUnitState target,
            BattleStatusInstance charging)
        {
            ValidateTarget(target);
            if (charging == null)
            {
                throw new ArgumentNullException(nameof(charging));
            }
            if (charging.StatusId != BattleStatusId.Charge
                || charging.RuntimeData is not ChargeStatusRuntimeState state
                || state.Phase != ChargePhase.Charging)
            {
                throw new ArgumentException(
                    "A Charge status in the Charging phase is required.",
                    nameof(charging));
            }
            if (!target.TryRemoveStatusInstance(charging))
            {
                return false;
            }

            ApplyStatus(target, BattleStatusFactory.CreateCharged(charging));
            return true;
        }

        internal void RefreshAllActionClockPauses()
        {
            foreach (var unit in GetAllUnits())
            {
                RefreshActionClockPause(unit);
            }
        }

        public void BeginSkillResolution()
        {
            _collectedEffects.Clear();
            _isCollecting = true;
            _reactionDepth = 0;
        }

        public IReadOnlyList<SkillEffectResult> EndSkillResolution()
        {
            var effects = _collectedEffects.ToArray();
            _collectedEffects.Clear();
            _isCollecting = false;
            _reactionDepth = 0;
            return effects;
        }

        public void CancelSkillResolution()
        {
            _collectedEffects.Clear();
            _isCollecting = false;
            _reactionDepth = 0;
        }

        public void HandleAttributeDamageApplied(
            AttributeDamageAppliedEvent damageEvent)
        {
            if (damageEvent == null)
            {
                throw new ArgumentNullException(nameof(damageEvent));
            }

            if (damageEvent.Attribute == PachimonAttribute.Fire)
            {
                ReduceFreezeFromFireDamage(damageEvent);
            }
            if (damageEvent.Attribute != PachimonAttribute.Electric)
            {
                return;
            }
            if (damageEvent.Source == null
                || !damageEvent.Calculation.Context.IsAttack)
            {
                return;
            }

            var leaks = damageEvent.Target.Statuses
                .Where(status =>
                    (status.Categories & BattleStatusCategory.Leak) != 0)
                .ToArray();
            var totalValue = leaks.Sum(leak =>
                checked(leak.Value * leak.StackCount));
            foreach (var leak in leaks)
            {
                damageEvent.Target.TryRemoveStatusInstance(leak);
            }
            if (totalValue <= 0)
            {
                return;
            }

            ResolveLeak(damageEvent, totalValue);
        }

        private static void AdvanceFrozenBreak(
            BattleUnitState target,
            int ticks)
        {
            if (!target.IsAlive || ticks <= 0)
            {
                return;
            }

            var status = target.GetStatus(BattleStatusId.FrozenBreakSelf);
            if (status?.RuntimeData is not FrozenBreakRuntimeState runtime
                || !status.RemainingTicks.HasValue)
            {
                return;
            }

            var activeTicks = Math.Min(ticks, status.RemainingTicks.Value);
            var healing = runtime.AccumulateHealing(activeTicks);
            if (healing > 0)
            {
                target.RestoreHp(healing);
            }
        }

        private void ReduceFreezeFromFireDamage(
            AttributeDamageAppliedEvent damageEvent)
        {
            var freeze = damageEvent.Target.GetStatus(BattleStatusId.Freeze);
            if (freeze?.Definition is not FreezeStatusAsset definition
                || damageEvent.AppliedDamage <= 0)
            {
                return;
            }

            var reduction = damageEvent.AppliedDamage
                / definition.FireDamagePerDecay;
            if (reduction <= 0)
            {
                return;
            }

            ReduceStatusValue(
                damageEvent.Target,
                BattleStatusId.Freeze,
                reduction);
        }

        private void ResolveLeak(
            AttributeDamageAppliedEvent trigger,
            int leakValue)
        {
            if (_reactionDepth >= MaxReactionDepth)
            {
                throw new InvalidOperationException(
                    "Status reaction depth exceeded the safety limit.");
            }

            var rawExtraDamage =
                trigger.AppliedDamage * leakValue / 100m;
            if (rawExtraDamage <= 0m)
            {
                return;
            }

            _state.AddLog($"{trigger.Target.DisplayName}は漏電している！");

            _reactionDepth++;
            try
            {
                var targetSide = trigger.Target.Side == BattleSide.Player
                    ? _state.Player
                    : _state.Enemy;
                foreach (var target in targetSide.GetAllLiving().ToArray())
                {
                    var result = BattleStatusDamageService.ApplyAttribute(
                        _state,
                        target,
                        BattleStatusId.Leak,
                        PachimonAttribute.Electric,
                        rawExtraDamage);
                    if (_isCollecting)
                    {
                        _collectedEffects.Add(new SkillEffectResult(
                            target,
                            result.AppliedDamage,
                            isTrueDamage: false));
                    }
                }
            }
            finally
            {
                _reactionDepth--;
            }
        }

        private void RefreshActionClockPause(BattleUnitState unit)
        {
            unit.SetActionClockPaused(
                unit.HasStatusCategory(BattleStatusCategory.Stun));
        }

        private IEnumerable<BattleUnitState> GetAllUnits()
        {
            return _state.Player.Units.Concat(_state.Enemy.Units);
        }

        private void ValidateTarget(BattleUnitState target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (!GetAllUnits().Contains(target))
            {
                throw new ArgumentException(
                    "The Status target does not belong to this Battle.",
                    nameof(target));
            }
        }
    }
}
