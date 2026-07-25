using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// Gan script nay len prefab 1 slot (icon + so luong + nut Dung).
// Cau truc prefab goi y: Slot (Image nen) > Icon (Image), QuantityText (TMP), UseButton (Button > NameText TMP)
public class InventorySlotUI : MonoBehaviour
{
    [Header("References")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text quantityText;
    public Button useButton;

    public void Setup(ItemData data, int quantity, UnityAction onUseClicked)
    {
        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = data.icon != null;
        }

        if (nameText != null) nameText.text = data.itemName;
        if (quantityText != null) quantityText.text = "x" + quantity;

        if (useButton != null)
        {
            useButton.onClick.RemoveAllListeners();
            // item Equip khong nam trong tui nen slot luon la item Use -> luon cho dung
            useButton.onClick.AddListener(onUseClicked);
        }
    }
}