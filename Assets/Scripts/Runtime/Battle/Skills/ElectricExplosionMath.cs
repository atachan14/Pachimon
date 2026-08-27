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
            decimal? electricScalingPercent = null)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));

            return SignedStatMath.ScaleFromBase(
                skill.BaseDamage,
                electric,
                electricScalingPercent ?? skill.ElectricScalingPercent);
        }

        public static decimal CalculateAttributePenetrationValue(
            ElectricExplosionSkillAsset skill,
            decimal fire,
            decimal fireScalingPercent = 100m)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return fire
                * fireScalingPercent / 100m
                * skill.FirePenetrationRatio / 100m;
        }
    }
}
