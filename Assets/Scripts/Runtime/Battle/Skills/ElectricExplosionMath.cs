using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public static class ElectricExplosionMath
    {
        public static decimal CalculateBaseDamage(
            ElectricExplosionSkillAsset skill,
            decimal electric,
            decimal fire,
            decimal? electricScalingPercent = null,
            decimal? fireScalingPercent = null)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));

            return SignedStatMath.ScaleFromBase(
                    skill.BasePower,
                    electric,
                    electricScalingPercent ?? skill.ElectricScalingPercent)
                * SignedStatMath.AmplificationMultiplier(
                    fire
                    * (fireScalingPercent ?? skill.FireScalingPercent)
                    / 100m);
        }

        public static decimal CalculatePenetrationPercent(
            ElectricExplosionSkillAsset skill,
            decimal fire,
            decimal fireScalingPercent = 100m)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return fire
                * fireScalingPercent / 100m
                * skill.PenetrationPercentAtFire100 / 100m;
        }
    }
}
