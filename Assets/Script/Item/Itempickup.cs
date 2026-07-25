using UnityEngine;

// Gan script nay len object item trong scene. Object phai o Layer "Item"
// (layer duoc chon trong Inventory.itemLayer) va co Collider2D (isTrigger tuy y,
// vi Inventory dung OverlapCircleNonAlloc de do khoang cach chu khong bat buoc trigger).
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour
{
    public ItemData itemData;
    public int quantity = 1;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    // Inventory goi ham nay khi player nhat item thanh cong
    public void OnPickedUp()
    {
        Destroy(gameObject);
    }
}