using UnityEngine;

// Item chia 2 loai:
// - Equip: nhat vao la cong luon chi so vinh vien cho player (khong nam trong tui do)
// - Use  : nhat vao thi luu so luong trong tui, moi lan dung se -1 so luong va ap dung hieu ung
public enum ItemType
{
    Equip,
    Use
}

// Cac chi so cua PlayerController co the bi Equip item cong them
public enum StatType
{
    MaxHealth,
    MoveSpeed,
    JumpForce,
    AttackDamage,
    BulletDamage,
    DashDamage,
    MaxJumpCount
}

[System.Serializable]
public struct StatBonus
{
    public StatType statType;
    public float value;
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Thong tin chung")]
    public string itemId;
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemType itemType = ItemType.Use;

    [Header("Use - so luong toi da trong 1 stack (chi ap dung cho Use)")]
    public int maxStack = 99;

    [Header("Equip - cong chi so vinh vien khi nhat")]
    public StatBonus[] equipStatBonuses;

    [Header("Use - hieu ung khi su dung")]
    public int useHealAmount = 0;
    // mo rong them cac hieu ung use khac (buff tam thoi, mana, v.v.) tai day va trong Inventory.ApplyUseEffect
}