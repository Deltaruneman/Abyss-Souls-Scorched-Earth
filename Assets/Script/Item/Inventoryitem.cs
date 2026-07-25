// Dai dien 1 stack item Use trong tui do (item Equip khong luu o day,
// vi Equip cong thang chi so vinh vien roi bien mat khoi tui).
[System.Serializable]
public class InventoryItem
{
    public ItemData data;
    public int quantity;

    public InventoryItem(ItemData data, int quantity)
    {
        this.data = data;
        this.quantity = quantity;
    }
}