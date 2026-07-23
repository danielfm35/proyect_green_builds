using System.Collections.Generic;
using UnityEngine;

public class ChestStorageManager : MonoBehaviour
{
    [SerializeField] private List<ChestSlotUI> slots = new();

    private void Awake()
    {
        RefreshSlots();
    }

    public void RefreshSlots()
    {
        slots.Clear();
        ChestSlotUI[] discovered = GetComponentsInChildren<ChestSlotUI>(true);
        slots.AddRange(discovered);
    }

    private void AutoPopulateSlotsIfNeeded()
    {
        if (slots.Count == 0)
            RefreshSlots();
    }

    public bool TryStoreItem(DraggedItemVisual item)
    {
        if (item == null) return false;

        AutoPopulateSlotsIfNeeded();

        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].CanAcceptItem())
                continue;

            PlaceItemInSlot(item, slots[i]);
            return true;
        }

        return false;
    }

    public bool TryStoreItemInSlot(DraggedItemVisual item, ChestSlotUI slot)
    {
        if (item == null || slot == null) return false;
        if (!slot.CanAcceptItem()) return false;

        PlaceItemInSlot(item, slot);
        return true;
    }

    private void PlaceItemInSlot(DraggedItemVisual item, ChestSlotUI slot)
    {
        RectTransform itemRect = item.GetComponent<RectTransform>();
        RectTransform slotRect = slot.GetComponent<RectTransform>();

        item.transform.SetParent(slot.transform, false);
        item.transform.SetAsLastSibling();

        itemRect.anchorMin = new Vector2(0.5f, 0.5f);
        itemRect.anchorMax = new Vector2(0.5f, 0.5f);
        itemRect.pivot = new Vector2(0.5f, 0.5f);
        itemRect.anchoredPosition = Vector2.zero;
        itemRect.localScale = Vector3.one;
        itemRect.sizeDelta = slotRect.rect.size;

        slot.SetItem(item);
        item.SetCurrentChestSlot(slot);
    }
}
