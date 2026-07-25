using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Gan script nay chung GameObject voi PlayerController.
// Bam phim F (pickupKey) de nhat item nam trong pickupRadius va thuoc itemLayer.
[RequireComponent(typeof(PlayerController))]
public class Inventory : MonoBehaviour
{
    [Header("Nhat item")]
    public KeyCode pickupKey = KeyCode.F;
    public float pickupRadius = 1f;
    public LayerMask itemLayer;
    public Transform pickupCheckPoint; // rong thi dung transform cua player

    [Header("Du lieu tui do (chi luu item loai Use)")]
    public List<InventoryItem> items = new List<InventoryItem>();

    [Header("Events")]
    public UnityEvent onInventoryChanged;
    public UnityEvent<ItemData> onItemPickedUp;
    public UnityEvent<ItemData> onItemUsed;

    private PlayerController playerController;
    private readonly Collider2D[] pickupResults = new Collider2D[8];

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(pickupKey))
        {
            TryPickupNearbyItems();
        }
    }

    // ===================== NHAT ITEM =====================
    private void TryPickupNearbyItems()
    {
        Vector2 checkPos = pickupCheckPoint != null ? (Vector2)pickupCheckPoint.position : (Vector2)transform.position;
        int count = Physics2D.OverlapCircleNonAlloc(checkPos, pickupRadius, pickupResults, itemLayer);

        for (int i = 0; i < count; i++)
        {
            Collider2D hitCol = pickupResults[i];
            if (hitCol == null) continue;

            ItemPickup pickup = hitCol.GetComponentInParent<ItemPickup>();
            if (pickup == null || pickup.itemData == null) continue;

            AddItem(pickup.itemData, pickup.quantity);
            pickup.OnPickedUp();
        }
    }

    // ===================== THEM ITEM VAO TUI =====================
    public void AddItem(ItemData data, int quantity = 1)
    {
        if (data == null || quantity <= 0) return;

        // item Equip: cong thang chi so vinh vien, khong luu vao tui
        if (data.itemType == ItemType.Equip)
        {
            ApplyEquipBonus(data);
            onItemPickedUp?.Invoke(data);
            onInventoryChanged?.Invoke();
            return;
        }

        // item Use: gop stack neu da co, tao moi neu chua co
        InventoryItem existing = items.Find(it => it.data == data);
        if (existing != null)
        {
            existing.quantity = Mathf.Min(existing.quantity + quantity, data.maxStack);
        }
        else
        {
            items.Add(new InventoryItem(data, Mathf.Min(quantity, data.maxStack)));
        }

        onItemPickedUp?.Invoke(data);
        onInventoryChanged?.Invoke();
    }

    // ===================== SU DUNG ITEM (Use) =====================
    // dung item theo data, tru 1 so luong, xoa khoi tui neu ve 0
    public void UseItem(ItemData data)
    {
        if (data == null || data.itemType != ItemType.Use) return;

        InventoryItem existing = items.Find(it => it.data == data);
        if (existing == null || existing.quantity <= 0) return;

        ApplyUseEffect(data);

        existing.quantity -= 1;
        if (existing.quantity <= 0) items.Remove(existing);

        onItemUsed?.Invoke(data);
        onInventoryChanged?.Invoke();
    }

    // dung item theo vi tri trong list (tien cho UI inventory)
    public void UseItem(int index)
    {
        if (index < 0 || index >= items.Count) return;
        UseItem(items[index].data);
    }

    // ===================== AP DUNG HIEU UNG =====================
    private void ApplyEquipBonus(ItemData data)
    {
        if (data.equipStatBonuses == null) return;

        foreach (StatBonus bonus in data.equipStatBonuses)
        {
            playerController.ApplyStatBonus(bonus.statType, bonus.value);
        }
    }

    private void ApplyUseEffect(ItemData data)
    {
        if (data.useHealAmount > 0)
        {
            playerController.Heal(data.useHealAmount);
        }
        // mo rong them cac hieu ung use khac (buff tam thoi, mana, v.v.) o day
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 pos = pickupCheckPoint != null ? pickupCheckPoint.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, pickupRadius);
    }
}