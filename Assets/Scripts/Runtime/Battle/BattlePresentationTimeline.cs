using System;
using System.Collections.Generic;
using System.Linq;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public enum BattlePresentationStepKind
    {
        PassiveTriggered = 0,
        DamageApplied = 1,
        ResourceChanged = 2,
    }

    public enum BattlePresentationBlockStyle
    {
        RepeatedSkill,
        Continuous,
    }

    public sealed class BattleResourceTransition
    {
        public BattleResourceTransition(
            BattleUnitState unit,
            int hpBefore,
            int hpAfter,
            int mnBefore,
            int mnAfter)
        {
            Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            HpBefore = hpBefore;
            HpAfter = hpAfter;
            MnBefore = mnBefore;
            MnAfter = mnAfter;
        }

        public BattleUnitState Unit { get; }
        public int HpBefore { get; }
        public int HpAfter { get; }
        public int MnBefore { get; }
        public int MnAfter { get; }
    }

    public sealed class BattlePresentationStep
    {
        public BattlePresentationStep(
            BattlePresentationStepKind kind,
            string text,
            BattleUnitState focusUnit,
            IEnumerable<BattleResourceTransition> transitions,
            int blockIndex,
            int damage = 0,
            bool isTrueDamage = false)
        {
            Kind = kind;
            Text = text ?? string.Empty;
            FocusUnit = focusUnit;
            Transitions = transitions?.ToArray()
                ?? Array.Empty<BattleResourceTransition>();
            BlockIndex = blockIndex;
            Damage = damage;
            IsTrueDamage = isTrueDamage;
        }

        public BattlePresentationStepKind Kind { get; }
        public string Text { get; }
        public BattleUnitState FocusUnit { get; }
        public IReadOnlyList<BattleResourceTransition> Transitions { get; }
        public int BlockIndex { get; }
        public int Damage { get; }
        public bool IsTrueDamage { get; }
    }

    public sealed class BattlePresentationTimeline
    {
        public static readonly BattlePresentationTimeline Empty = new(
            null,
            Array.Empty<BattlePresentationStep>(),
            BattlePresentationBlockStyle.RepeatedSkill);

        public BattlePresentationTimeline(
            BattleResourceTransition initialManaTransition,
            IEnumerable<BattlePresentationStep> steps,
            BattlePresentationBlockStyle blockStyle)
        {
            InitialManaTransition = initialManaTransition;
            Steps = steps?.ToArray() ?? Array.Empty<BattlePresentationStep>();
            BlockStyle = blockStyle;
        }

        public BattleResourceTransition InitialManaTransition { get; }
        public IReadOnlyList<BattlePresentationStep> Steps { get; }
        public BattlePresentationBlockStyle BlockStyle { get; }
    }

    public sealed class BattlePresentationRecorder
    {
        private readonly BattleState _state;
        private readonly List<BattlePresentationStep> _steps = new();
        private readonly Dictionary<BattleUnitState, ResourceSnapshot> _snapshots = new();
        private readonly Dictionary<BattleUnitState, PendingResourceChange> _pendingResources = new();
        private BattleResourceTransition _initialManaTransition;
        private int _currentBlockIndex;
        private BattlePresentationBlockStyle _blockStyle;

        public BattlePresentationRecorder(BattleState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public bool IsRecording { get; private set; }

        public void Begin(BattleUnitState user, SkillAsset skill)
        {
            if (IsRecording)
            {
                throw new InvalidOperationException(
                    "A Battle presentation is already being recorded.");
            }

            _ = user ?? throw new ArgumentNullException(nameof(user));
            _ = skill ?? throw new ArgumentNullException(nameof(skill));
            _steps.Clear();
            _snapshots.Clear();
            _pendingResources.Clear();
            _initialManaTransition = null;
            _currentBlockIndex = 0;
            _blockStyle = BattlePresentationBlockStyle.RepeatedSkill;
            foreach (var unit in _state.Player.Units.Concat(_state.Enemy.Units))
            {
                _snapshots[unit] = new ResourceSnapshot(
                    unit.CurrentHp,
                    unit.CurrentMn);
            }

            IsRecording = true;
        }

        public void BeginNextBlock()
        {
            if (!IsRecording)
            {
                return;
            }

            _currentBlockIndex++;
        }

        public void UseContinuousBlocks()
        {
            if (IsRecording)
            {
                _blockStyle = BattlePresentationBlockStyle.Continuous;
            }
        }

        public void RecordInitialManaSpent(
            BattleUnitState unit,
            int mnBefore,
            int mnAfter)
        {
            if (!IsRecording || unit == null || mnBefore == mnAfter)
            {
                return;
            }

            var snapshot = GetSnapshot(unit);
            _initialManaTransition = new BattleResourceTransition(
                unit,
                snapshot.Hp,
                snapshot.Hp,
                mnBefore,
                mnAfter);
            _snapshots[unit] = new ResourceSnapshot(snapshot.Hp, mnAfter);
        }

        public void RecordAdditionalManaSpent(
            BattleUnitState unit,
            int mnBefore,
            int mnAfter)
        {
            if (!IsRecording || unit == null || mnBefore == mnAfter)
            {
                return;
            }

            var snapshot = GetSnapshot(unit);
            var updated = new ResourceSnapshot(snapshot.Hp, mnAfter);
            _snapshots[unit] = updated;
            _pendingResources[unit] = _pendingResources.TryGetValue(
                unit,
                out var pending)
                ? new PendingResourceChange(pending.Before, updated)
                : new PendingResourceChange(
                    new ResourceSnapshot(snapshot.Hp, mnBefore),
                    updated);
        }

        public void RecordDamage(
            BattleUnitState target,
            int hpBefore,
            int hpAfter,
            int appliedDamage,
            bool isTrueDamage,
            int shieldAbsorbedDamage = 0)
        {
            if (!IsRecording || target == null)
            {
                return;
            }

            var targetBefore = GetSnapshot(target);
            var targetAfter = new ResourceSnapshot(hpAfter, targetBefore.Mn);
            _snapshots[target] = targetAfter;
            var changes = new Dictionary<BattleUnitState, PendingResourceChange>(
                _pendingResources);
            if (changes.TryGetValue(target, out var targetPending))
            {
                changes[target] = new PendingResourceChange(
                    targetPending.Before,
                    targetAfter);
            }
            else
            {
                changes[target] = new PendingResourceChange(
                    new ResourceSnapshot(hpBefore, targetBefore.Mn),
                    targetAfter);
            }

            var transitions = changes.Select(pair =>
                new BattleResourceTransition(
                    pair.Key,
                    pair.Value.Before.Hp,
                    pair.Value.After.Hp,
                    pair.Value.Before.Mn,
                    pair.Value.After.Mn)).ToArray();
            _pendingResources.Clear();

            var damageKind = isTrueDamage ? "確定ダメージ" : "ダメージ";
            var text = shieldAbsorbedDamage > 0
                ? appliedDamage > 0
                    ? $"{target.DisplayName}に{appliedDamage}の{damageKind}！"
                      + $"（Shieldが{shieldAbsorbedDamage}吸収）"
                    : $"{target.DisplayName}のShieldが"
                      + $"{shieldAbsorbedDamage}ダメージを吸収した！"
                : $"{target.DisplayName}に{appliedDamage}の{damageKind}！";
            _steps.Add(new BattlePresentationStep(
                BattlePresentationStepKind.DamageApplied,
                text,
                target,
                transitions,
                _currentBlockIndex,
                appliedDamage,
                isTrueDamage));
        }

        public void RecordLog(string message)
        {
            if (!IsRecording || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            _steps.Add(new BattlePresentationStep(
                BattlePresentationStepKind.PassiveTriggered,
                message,
                null,
                Array.Empty<BattleResourceTransition>(),
                _currentBlockIndex));
        }

        public BattlePresentationTimeline Complete()
        {
            if (!IsRecording)
            {
                return BattlePresentationTimeline.Empty;
            }

            FlushPendingResources();
            var result = new BattlePresentationTimeline(
                _initialManaTransition,
                _steps,
                _blockStyle);
            Reset();
            return result;
        }

        public void Cancel()
        {
            Reset();
        }

        private void FlushPendingResources()
        {
            if (_pendingResources.Count == 0)
            {
                return;
            }

            var transitions = _pendingResources.Select(pair =>
                new BattleResourceTransition(
                    pair.Key,
                    pair.Value.Before.Hp,
                    pair.Value.After.Hp,
                    pair.Value.Before.Mn,
                    pair.Value.After.Mn)).ToArray();
            _steps.Add(new BattlePresentationStep(
                BattlePresentationStepKind.ResourceChanged,
                string.Empty,
                null,
                transitions,
                _currentBlockIndex));
            _pendingResources.Clear();
        }

        private ResourceSnapshot GetSnapshot(BattleUnitState unit)
        {
            return _snapshots.TryGetValue(unit, out var snapshot)
                ? snapshot
                : new ResourceSnapshot(unit.CurrentHp, unit.CurrentMn);
        }

        private void Reset()
        {
            IsRecording = false;
            _steps.Clear();
            _snapshots.Clear();
            _pendingResources.Clear();
            _initialManaTransition = null;
            _currentBlockIndex = 0;
            _blockStyle = BattlePresentationBlockStyle.RepeatedSkill;
        }

        private readonly struct ResourceSnapshot
        {
            public ResourceSnapshot(int hp, int mn)
            {
                Hp = hp;
                Mn = mn;
            }

            public int Hp { get; }
            public int Mn { get; }
        }

        private readonly struct PendingResourceChange
        {
            public PendingResourceChange(
                ResourceSnapshot before,
                ResourceSnapshot after)
            {
                Before = before;
                After = after;
            }

            public ResourceSnapshot Before { get; }
            public ResourceSnapshot After { get; }
        }
    }

    public sealed class ToxinPresentationRecorder
    {
        private readonly Dictionary<BattleUnitState, BattleResourceTransition>
            _pending = new();

        public void RecordDamage(
            BattleUnitState target,
            int hpBefore,
            int hpAfter)
        {
            if (target == null || hpBefore == hpAfter)
            {
                return;
            }

            if (_pending.TryGetValue(target, out var existing))
            {
                _pending[target] = new BattleResourceTransition(
                    target,
                    existing.HpBefore,
                    hpAfter,
                    existing.MnBefore,
                    target.CurrentMn);
                return;
            }

            _pending.Add(target, new BattleResourceTransition(
                target,
                hpBefore,
                hpAfter,
                target.CurrentMn,
                target.CurrentMn));
        }

        public IReadOnlyList<BattleResourceTransition> Drain()
        {
            if (_pending.Count == 0)
            {
                return Array.Empty<BattleResourceTransition>();
            }

            var transitions = _pending.Values.ToArray();
            _pending.Clear();
            return transitions;
        }
    }
}
