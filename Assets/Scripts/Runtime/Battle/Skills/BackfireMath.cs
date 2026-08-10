using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public static class BackfireMath
    {
        public static decimal CalculateBaseDamage(
            BackfireSkillAsset skill,
            decimal fire,
            decimal? fireScalingPercent = null)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return SignedStatMath.ScaleFromBase(
                skill.BasePower,
                fire,
                fireScalingPercent ?? skill.FireScalingPercent);
        }

        public static decimal CalculatePenetrationPercent(
            BackfireSkillAsset skill,
            decimal poison)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return SignedStatMath.ScaleFromBase(
                skill.BasePenetrationPercent,
                poison,
                skill.PoisonScalingPercent);
        }
    }
}
