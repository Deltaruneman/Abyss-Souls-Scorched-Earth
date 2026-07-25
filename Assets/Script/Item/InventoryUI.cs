using System.Collections.Generic;
using UnityEngine;

// Gan script nay len 1 GameObject UI (vd: InventoryUI), keo cac tham chieu trong Inspector.
// Bam toggleKey (mac dinh Tab) de mo/dong panel tui do.
public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public Inventory inventory;
    public GameObject panelRoot;      // GameObject chua toan bo UI tui do, se duoc SetActive bat/tat
    public Transform slotContainer;   // parent co Layout Group (vd Grid Layout Group) chua cac slot
    public InventorySlotUI slotPrefab;

    [Header("Dieu khien")]
    public KeyCode toggleKey = KeyCode.Tab;
    public bool startOpen = false;

    private void Awake()
    {
        if (inventory != null)
        {
            inventory.onInventoryChanged.AddListener(RefreshUI);
        }
    }

    private void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(startOpen);
        RefreshUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey) && panelRoot != null)
        {
            bool willOpen = !panelRoot.activeSelf;
            panelRoot.SetActive(willOpen);
            if (willOpen) RefreshUI();
        }
    }

    // pool slot da tao, tai su dung thay vi Destroy/Instantiate lai moi lan refresh
    private readonly List<InventorySlotUI> slotPool = new List<InventorySlotUI>();

    // ve lai toan bo tui do theo du lieu hien tai, tai su dung slot cu neu co
    public void RefreshUI()
    {
        if (slotContainer == null || slotPrefab == null || inventory == null) return;

        int usedCount = 0;

        foreach (InventoryItem item in inventory.items)
        {
            if (item.data == null) continue;

            InventorySlotUI slot;
            if (usedCount < slotPool.Count)
            {
                slot = slotPool[usedCount];
            }
            else
            {
                slot = Instantiate(slotPrefab, slotContainer);
                slotPool.Add(slot);
            }

            slot.gameObject.SetActive(true);
            ItemData data = item.data; // tranh loi closure khi dung trong lambda
            slot.Setup(data, item.quantity, () => inventory.UseItem(data));

            usedCount++;
        }

        // an bot cac slot du (khong Destroy, giu lai de tai su dung lan sau)
        for (int i = usedCount; i < slotPool.Count; i++)
        {
            slotPool[i].gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.onInventoryChanged.RemoveListener(RefreshUI);
        }
    }
}