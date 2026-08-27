using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public static class FireArrowMath
    {
        public static decimal CalculateBaseDamage(
            FireArrowSkillAsset skill,
            decimal fire,
            decimal? fireScalingPercent = null)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return SignedStatMath.ScaleFromBase(
                skill.BaseDamage,
                fire,
                fireScalingPercent ?? skill.FireScalingPercent);
        }
    }
}
