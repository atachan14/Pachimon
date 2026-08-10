using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public static class CombustionMath
    {
        public static decimal CalculateBaseDamage(
            CombustionSkillAsset skill,
            decimal fire,
            decimal? fireScalingPercent = null)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return SignedStatMath.ScaleFromBase(
                skill.BasePower,
                fire,
                fireScalingPercent ?? skill.FireScalingPercent);
        }
    }
}
