namespace Pachimon.Battle
{
    public sealed class BattleUnit
    {
        public BattleUnit(string id, string displayName, int slotIndex, int maxHp, int currentHp, int currentMn, bool isEnemy)
        {
            Id = id;
            DisplayName = displayName;
            SlotIndex = slotIndex;
            MaxHp = maxHp;
            CurrentHp = currentHp;
            CurrentMn = currentMn;
            IsEnemy = isEnemy;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int SlotIndex { get; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public int CurrentMn { get; private set; }
        public bool IsEnemy { get; }
        public bool IsAlive => CurrentHp > 0;

        public void ApplyDamage(int amount)
        {
            if (amount <= 0 || !IsAlive)
            {
                return;
            }

            CurrentHp = CurrentHp - amount;
            if (CurrentHp < 0)
            {
                CurrentHp = 0;
            }
        }

        public void ChangeMana(int delta)
        {
            CurrentMn += delta;
            if (CurrentMn < 0)
            {
                CurrentMn = 0;
            }
        }
    }
}
