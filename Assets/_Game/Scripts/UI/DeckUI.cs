using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Hotbar de sorts en bas d'écran.
/// Raccourcis : Q W E R A S D F (indices 0-7, spec Phase 2).
/// Réagit aux événements PA/PM du TacticalCharacter actif.
/// </summary>
public class DeckUI : MonoBehaviour
{
    // =========================================================
    // CONFIGURATION
    // =========================================================
    [Header("Slots (jusqu'à 8 — Q W E R A S D F)")]
    public List<SpellSlotUI> slots = new List<SpellSlotUI>();

    [Header("Tooltip")]
    public SpellTooltip tooltip;

    // =========================================================
    // ÉTAT
    // =========================================================
    private TacticalCharacter activeCharacter;
    private SpellCaster       activeCaster;
    private int               selectedSlotIndex = -1;

    private static readonly string[] Hotkeys = { "Q", "W", "E", "R", "A", "S", "D", "F" };
    private static readonly KeyCode[] HotkeyCodes =
    {
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R,
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F
    };

    // =========================================================
    // LIAISON AVEC UN PERSONNAGE
    // =========================================================
    public void BindCharacter(TacticalCharacter character)
    {
        if (activeCharacter != null)
        {
            activeCharacter.OnPAChanged -= OnResourceChanged;
            activeCharacter.OnPMChanged -= OnResourceChanged;
        }

        activeCharacter = character;
        activeCaster    = character != null ? character.GetComponent<SpellCaster>() : null;

        if (activeCharacter != null)
        {
            activeCharacter.OnPAChanged += OnResourceChanged;
            activeCharacter.OnPMChanged += OnResourceChanged;
        }

        RebuildSlots();
    }

    public void UnbindCharacter()
    {
        BindCharacter(null);
        ClearSelection();
    }

    // =========================================================
    // CONSTRUCTION DE LA HOTBAR
    // =========================================================
    private void RebuildSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null) continue;

            SpellData spell = null;
            if (activeCharacter != null && activeCharacter.deck != null)
            {
                var spells = activeCharacter.deck.Spells;
                spell = i < spells.Count ? spells[i] : null;
            }

            string hotkey = i < Hotkeys.Length ? Hotkeys[i] : $"(Slot {i + 1})";
            slots[i].Setup(spell, activeCharacter, this, i, hotkey);
            slots[i].gameObject.SetActive(activeCharacter != null);
        }

        ClearSelection();
    }

    // =========================================================
    // REFRESH — mis à jour à chaque changement de PA/PM
    // =========================================================
    private void OnResourceChanged(int current, int max) => RefreshAll();

    public void RefreshAll()
    {
        for (int i = 0; i < slots.Count; i++)
            if (slots[i] != null) slots[i].Refresh();
    }

    // =========================================================
    // SÉLECTION
    // =========================================================
    public void SelectSlot(int index)
    {
        if (activeCaster == null || activeCharacter == null) return;
        if (index < 0 || index >= slots.Count) return;

        SpellSlotUI slot = slots[index];
        if (slot == null || !slot.HasSpell) return;

        if (selectedSlotIndex == index)
        {
            ClearSelection();
            activeCaster.CancelSpell();
            return;
        }

        bool ok = activeCaster.SelectSpell(slot.Spell);
        if (!ok) return;

        if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
            slots[selectedSlotIndex].SetSelected(false);

        selectedSlotIndex = index;
        slot.SetSelected(true);
    }

    public void ClearSelection()
    {
        if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
            slots[selectedSlotIndex]?.SetSelected(false);
        selectedSlotIndex = -1;
    }

    // =========================================================
    // RACCOURCIS CLAVIER
    // =========================================================
    void Update()
    {
        if (activeCharacter == null) return;

        int keyCount = Mathf.Min(HotkeyCodes.Length, slots.Count);
        for (int i = 0; i < keyCount; i++)
        {
            if (Input.GetKeyDown(HotkeyCodes[i]))
            {
                SelectSlot(i);
                break;
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape) && selectedSlotIndex >= 0)
        {
            ClearSelection();
            activeCaster?.CancelSpell();
        }
    }

    // =========================================================
    // TOOLTIP
    // =========================================================
    public void ShowTooltip(SpellData spell, Vector3 anchorWorldPos)
    {
        if (tooltip != null) tooltip.Show(spell, anchorWorldPos);
    }

    public void HideTooltip()
    {
        if (tooltip != null) tooltip.Hide();
    }
}
