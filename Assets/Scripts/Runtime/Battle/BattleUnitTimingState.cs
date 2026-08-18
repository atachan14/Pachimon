using System;

namespace Pachimon.Battle
{
    public enum BattleActionPhase
    {
        InitialDelay = 0,
        Ready = 1,
        Startup = 2,
        Recovery = 3,
        Defeated = 4,
    }

    /// <summary>
    /// Owns the remaining time for one Unit's action clock.
    /// Stun and similar effects pause this clock instead of rewriting absolute ticks.
    /// </summary>
    public sealed class BattleUnitTimingState
    {
        public BattleActionPhase Phase { get; private set; }
        public decimal TotalWork { get; private set; }
        public decimal RemainingWork { get; private set; }
        public int TotalTicks => (int)Math.Ceiling(TotalWork);
        public int RemainingTicks => (int)Math.Ceiling(RemainingWork);
        public bool IsPaused { get; private set; }
        public bool IsComplete => RemainingWork <= 0m;
        public string CurrentActionName { get; private set; } = string.Empty;

        public float Progress =>
            TotalWork <= 0m
                ? Phase == BattleActionPhase.Ready ? 1f : 0f
                : 1f - (float)(RemainingWork / TotalWork);

        internal void BeginInitialDelay(decimal work)
        {
            BeginTimedPhase(BattleActionPhase.InitialDelay, work);
        }

        internal void BeginStartup(decimal work, string actionName = null)
        {
            if (work <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(work));
            }

            CurrentActionName = actionName ?? string.Empty;
            BeginTimedPhase(BattleActionPhase.Startup, work);
        }

        internal void BeginRecovery(decimal work, string actionName = null)
        {
            if (actionName != null)
            {
                CurrentActionName = actionName;
            }
            BeginTimedPhase(BattleActionPhase.Recovery, work);
        }

        internal void MarkReady()
        {
            Phase = BattleActionPhase.Ready;
            TotalWork = 0m;
            RemainingWork = 0m;
            IsPaused = false;
            CurrentActionName = string.Empty;
        }

        internal void MarkDefeated()
        {
            Phase = BattleActionPhase.Defeated;
            TotalWork = 0m;
            RemainingWork = 0m;
            IsPaused = false;
            CurrentActionName = string.Empty;
        }

        internal void SetPaused(bool isPaused)
        {
            if (Phase == BattleActionPhase.Defeated)
            {
                IsPaused = false;
                return;
            }

            IsPaused = isPaused;
        }

        internal void Advance(decimal progress)
        {
            if (progress < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(progress));
            }

            if (progress == 0m
                || IsPaused
                || IsComplete
                || Phase == BattleActionPhase.Defeated)
            {
                return;
            }

            RemainingWork = Math.Max(0m, RemainingWork - progress);
        }

        internal BattleUnitTimingState CreateSimulationClone()
        {
            return new BattleUnitTimingState
            {
                Phase = Phase,
                TotalWork = TotalWork,
                RemainingWork = RemainingWork,
                IsPaused = IsPaused,
                CurrentActionName = CurrentActionName,
            };
        }

        internal void CopyFrom(BattleUnitTimingState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            Phase = source.Phase;
            TotalWork = source.TotalWork;
            RemainingWork = source.RemainingWork;
            IsPaused = source.IsPaused;
            CurrentActionName = source.CurrentActionName;
        }

        private void BeginTimedPhase(
            BattleActionPhase phase,
            decimal work)
        {
            if (work < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(work));
            }

            Phase = phase;
            TotalWork = work;
            RemainingWork = work;
            IsPaused = false;
            if (work == 0m)
            {
                MarkReady();
            }
        }
    }
}
