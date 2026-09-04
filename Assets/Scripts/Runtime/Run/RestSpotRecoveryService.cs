using System;
using System.Linq;

namespace Pachimon.Run
{
    public readonly struct RestSpotRecoveryResult
    {
        public RestSpotRecoveryResult(
            int recoveredPachimonCount,
            int revivedPachimonCount,
            int totalRestoredHp,
            int totalRestoredMn)
        {
            RecoveredPachimonCount = recoveredPachimonCount;
            RevivedPachimonCount = revivedPachimonCount;
            TotalRestoredHp = totalRestoredHp;
            TotalRestoredMn = totalRestoredMn;
        }

        public int RecoveredPachimonCount { get; }

        public int RevivedPachimonCount { get; }

        public int TotalRestoredHp { get; }

        public int TotalRestoredMn { get; }
    }

    public static class RestSpotRecoveryService
    {
        public static RestSpotRecoveryResult RecoverPlayerParty(
            RunState runState,
            RunPachimonPool pachimonPool,
            PassiveStatModifierRegistry passiveStatModifierRegistry,
            int healPercent)
        {
            if (runState == null)
            {
                throw new ArgumentNullException(nameof(runState));
            }

            if (pachimonPool == null)
            {
                throw new ArgumentNullException(nameof(pachimonPool));
            }

            if (passiveStatModifierRegistry == null)
            {
                throw new ArgumentNullException(nameof(passiveStatModifierRegistry));
            }

            if (healPercent <= 0 || healPercent > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(healPercent));
            }

            var party = runState.PlayerPachimonIds
                .Select(pachimonPool.Get)
                .ToArray();
            if (party.Length < 1
                || party.Length > RunState.MaxPartySize
                || party.Any(instance => instance == null))
            {
                throw new InvalidOperationException(
                    $"RestSpot requires between 1 and {RunState.MaxPartySize} Player Pachimon.");
            }

            var recoveredCount = 0;
            var revivedCount = 0;
            var totalRestoredHp = 0;
            var totalRestoredMn = 0;
            foreach (var instance in party)
            {
                var effectiveStats = PachimonStatService.Calculate(
                    instance,
                    runState.PlayerModifiers,
                    passiveStatModifierRegistry);
                var effectiveMaxHp = effectiveStats.MaxHp;
                var healAmount = CalculateHealAmount(effectiveMaxHp, healPercent);
                var previousHp = Math.Min(instance.CurrentHp, effectiveMaxHp);
                var wasDefeated = previousHp <= 0;
                var currentHp = instance.RestoreHp(healAmount, effectiveMaxHp);
                var restoredHp = currentHp - previousHp;
                var effectiveMaxMn = effectiveStats.MaxMn;
                var mnAmount = CalculateHealAmount(effectiveMaxMn, healPercent);
                var previousMn = Math.Min(instance.CurrentMn, effectiveMaxMn);
                var currentMn = instance.RestoreMn(mnAmount, effectiveMaxMn);
                var restoredMn = currentMn - previousMn;
                if (restoredHp <= 0 && restoredMn <= 0)
                {
                    continue;
                }

                recoveredCount++;
                totalRestoredHp = checked(totalRestoredHp + restoredHp);
                totalRestoredMn = checked(totalRestoredMn + restoredMn);
                if (wasDefeated)
                {
                    revivedCount++;
                }
            }

            return new RestSpotRecoveryResult(
                recoveredCount,
                revivedCount,
                totalRestoredHp,
                totalRestoredMn);
        }

        public static int CalculateHealAmount(int effectiveMaxHp, int healPercent)
        {
            if (effectiveMaxHp < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(effectiveMaxHp));
            }

            if (healPercent <= 0 || healPercent > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(healPercent));
            }

            var numerator = checked((long)effectiveMaxHp * healPercent);
            if (numerator <= 0L)
            {
                return 0;
            }

            return checked((int)Math.Max(1L, numerator / 100L));
        }
    }
}
