namespace Pachimon.Items
{
    public static class ItemIds
    {
        public const int Potion = 1;
        public const int Stone = 2;
        public const int SkillMachineItemBase = 10000;

        public static int GetSkillMachineItemId(int skillId)
        {
            return checked(SkillMachineItemBase + skillId);
        }
    }
}
