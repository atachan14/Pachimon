namespace Pachimon.Items
{
    public interface IItemLogic
    {
        ItemUseFailureReason CanUse(
            ItemAsset item,
            ItemInstance itemInstance,
            ItemUseContext context);

        int Apply(
            ItemAsset item,
            ItemInstance itemInstance,
            ItemUseContext context);
    }
}
