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
            long cooldownReadyTick,
            bool isCooldownReady,
            bool hasEnoughMn)
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
            IsCooldownReady = isCooldownReady;
            HasEnoughMn = hasEnoughMn;
        }

        public int SlotId { get; }
        public SkillAsset Skill { get; }
        public bool IsUsable { get; }
        public long CooldownReadyTick { get; }
        public bool IsCooldownReady { get; }
        public bool HasEnoughMn { get; }
    }
}
