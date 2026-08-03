using System;
using Pachimon.Run;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public static class CombustionMath
    {
        public static decimal CalculateBaseDamage(
            CombustionSkillAsset skill,
            decimal fire)
        {
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            return SignedStatMath.ScaleFromBase(
                skill.BasePower,
                fire,
                skill.FireScalingPercent);
        }
    }
}
