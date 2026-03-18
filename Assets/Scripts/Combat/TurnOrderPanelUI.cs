using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TurnOrderPanelUI : MonoBehaviour
{
    [SerializeField] List<Image> turnOrderSlots = new List<Image>(8);
    [SerializeField] Color defaultSlotColor = Color.white;
    [SerializeField] Color highlightedSlotColor = Color.yellow;
    [SerializeField] Vector3 defaultSlotScale = Vector3.one;
    [SerializeField] Vector3 highlightedSlotScale = new Vector3(1.15f, 1.15f, 1f);

    void OnEnable()
    {
        if (TurnManager.I != null)
        {
            TurnManager.I.TurnOrderChanged += HandleTurnOrderChanged;
            Refresh(TurnManager.I.GetTurnOrder(), TurnManager.I.GetCurrentTurnIndex());
        }
    }

    void OnDisable()
    {
        if (TurnManager.I != null)
            TurnManager.I.TurnOrderChanged -= HandleTurnOrderChanged;
    }

    void HandleTurnOrderChanged(IReadOnlyList<CombatUnit> orderedUnits, int currentTurnIndex)
    {
        Refresh(orderedUnits, currentTurnIndex);
    }

    void Refresh(IReadOnlyList<CombatUnit> orderedUnits, int currentTurnIndex)
    {
        for (int i = 0; i < turnOrderSlots.Count; i++)
        {
            Image slotImage = turnOrderSlots[i];
            if (slotImage == null)
                continue;

            if (orderedUnits != null && i < orderedUnits.Count && orderedUnits[i] != null)
            {
                slotImage.enabled = true;
                slotImage.sprite = orderedUnits[i].GetTurnOrderSprite();
                bool isCurrentTurnUnit = i == currentTurnIndex;
                slotImage.color = isCurrentTurnUnit ? highlightedSlotColor : defaultSlotColor;
                slotImage.transform.localScale = isCurrentTurnUnit ? highlightedSlotScale : defaultSlotScale;
            }
            else
            {
                slotImage.sprite = null;
                slotImage.enabled = false;
                slotImage.color = defaultSlotColor;
                slotImage.transform.localScale = defaultSlotScale;
            }
        }
    }
}
