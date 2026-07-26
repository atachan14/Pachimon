namespace Pachimon.Items
{
    public interface IItemLogic
    {
        ItemUseFailureReason CanUse(ItemAsset item, ItemUseContext context);

        int Apply(ItemAsset item, ItemUseContext context);
    }
}
