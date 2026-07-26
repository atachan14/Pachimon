namespace Pachimon.Data
{
    public enum AllocationType
    {
        Unassigned = 0,
        Fire = 1,
        Aqua = 2,
        Leaf = 3,
        Electric = 4,
        Poison = 5,
        Ice = 6,
        Wind = 7,
        Dragon = 8,
    }

    public static class AttributePlaceholderName
    {
        public const int AttributeCount = 8;

        public static string Format(AllocationType allocationType, int number)
        {
            var prefix = allocationType switch
            {
                AllocationType.Fire => "炎",
                AllocationType.Aqua => "水",
                AllocationType.Leaf => "草",
                AllocationType.Electric => "電",
                AllocationType.Poison => "毒",
                AllocationType.Ice => "氷",
                AllocationType.Wind => "風",
                AllocationType.Dragon => "竜",
                _ => "無",
            };
            return $"{prefix}{number:D3}";
        }

        public static string FromCyclicId(int id)
        {
            if (id <= 0)
            {
                return Format(AllocationType.Unassigned, 0);
            }

            var allocationType = (AllocationType)(((id - 1) % AttributeCount) + 1);
            var number = ((id - 1) / AttributeCount) + 1;
            return Format(allocationType, number);
        }
    }
}
