namespace Pachimon.Items
{
    public enum ItemUseFailureReason
    {
        None = 0,
        ItemNotOwned = 1,
        ItemDefinitionMissing = 2,
        ItemLogicMissing = 3,
        InvalidTarget = 4,
        NoEffect = 5,
        SkillSlotsFull = 6,
        SkillAlreadyKnown = 7,
    }

    public readonly struct ItemUseResult
    {
        private ItemUseResult(
            bool succeeded,
            ItemUseFailureReason failureReason,
            string itemInstanceId,
            string targetInstanceId,
            int effectAmount)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            ItemInstanceId = itemInstanceId;
            TargetInstanceId = targetInstanceId;
            EffectAmount = effectAmount;
        }

        public bool Succeeded { get; }
        public ItemUseFailureReason FailureReason { get; }
        public string ItemInstanceId { get; }
        public string TargetInstanceId { get; }
        public int EffectAmount { get; }

        public static ItemUseResult Success(
            string itemInstanceId,
            string targetInstanceId,
            int effectAmount)
        {
            return new ItemUseResult(
                true,
                ItemUseFailureReason.None,
                itemInstanceId,
                targetInstanceId,
                effectAmount);
        }

        public static ItemUseResult Failure(
            ItemUseFailureReason reason,
            string itemInstanceId = null,
            string targetInstanceId = null)
        {
            return new ItemUseResult(
                false,
                reason,
                itemInstanceId,
                targetInstanceId,
                0);
        }
    }
}
