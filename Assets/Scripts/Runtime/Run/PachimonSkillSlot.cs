using System;

namespace Pachimon.Run
{
    public sealed class PachimonSkillSlot
    {
        public PachimonSkillSlot(int slotId, int skillId)
        {
            if (slotId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotId));
            }

            if (skillId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skillId));
            }

            SlotId = slotId;
            SkillId = skillId;
        }

        public int SlotId { get; }

        public int SkillId { get; }
    }
}
