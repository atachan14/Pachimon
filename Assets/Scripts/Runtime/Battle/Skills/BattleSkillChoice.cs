using System;
using Pachimon.Skills;

namespace Pachimon.Battle
{
    public sealed class BattleSkillChoice
    {
        public BattleSkillChoice(
            int slotId,
            SkillAsset skill,
            bool isUsable,
            long cooldownReadyTick)
        {
            if (slotId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotId));
            }

            if (cooldownReadyTick < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cooldownReadyTick));
            }

            SlotId = slotId;
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            IsUsable = isUsable;
            CooldownReadyTick = cooldownReadyTick;
        }

        public int SlotId { get; }
        public SkillAsset Skill { get; }
        public bool IsUsable { get; }
        public long CooldownReadyTick { get; }
    }
}
