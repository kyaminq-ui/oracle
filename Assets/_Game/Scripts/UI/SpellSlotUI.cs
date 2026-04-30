using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class SpellSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    // =========================================================
    // RÉFÉRENCES
    // =========================================================
    [Header("Icône")]
    public Image iconImage;
    public Image dimOverlay;        // Image noire semi-transparente (sort indisponible)

    [Header("Cooldown")]
    public Image cooldownFill;      // Image type Filled, remplie de 1 → 0 pendant le cooldown
    public TextMeshProUGUI cooldownText;

    [Header("Labels")]
    public TextMeshProUGUI paCostText;
    public TextMeshProUGUI hotkeyText;

    [Header("Sélection")]
    public Image selectionBorder;   // Bordure dorée quand le sort est sélectionné

    // =========================================================
    // CONFIGURATION
    // =========================================================
    [Header("Couleurs")]
    public Color availableColor  = Color.white;
    public Color unavailableColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    public Color selectedBorderColor = new Color(0.79f, 0.66f, 0.30f, 1f);

    // =========================================================
    // ÉTAT
    // =========================================================
    private SpellData spell;
    private TacticalCharacter owner;
    private DeckUI deckUI;
    private int slotIndex;
    private bool isSelected;

    public SpellData Spell => spell;
    public bool HasSpell   => spell != null;

    // =========================================================
    // INITIALISATION
    // =========================================================
    public void Setup(SpellData spellData, TacticalCharacter character, DeckUI deck, int index, string hotkey)
    {
        spell      = spellData;
        owner      = character;
        deckUI     = deck;
        slotIndex  = index;

        if (hotkeyText != null) hotkeyText.text = hotkey;

        if (spell != null)
        {
            if (iconImage != null)
            {
                iconImage.sprite  = spell.icon;
                iconImage.enabled = spell.icon != null;
            }
            if (paCostText != null) paCostText.text = spell.paCost.ToString();
        }
        else
        {
            if (iconImage != null)    iconImage.enabled = false;
            if (paCostText != null)   paCostText.text   = "";
            if (cooldownText != null) cooldownText.text  = "";
        }

        SetSelected(false);
        Refresh();
    }

    // =========================================================
    // REFRESH — état disponible / cooldown
    // =========================================================
    public void Refresh()
    {
        if (spell == null || owner == null) return;

        bool onCooldown  = owner.GetCooldown(spell) > 0;
        bool paEnough    = owner.CurrentPA >= spell.paCost;
        bool canCast     = owner.CanCastSpell(spell);

        // Overlay de grisé
        if (dimOverlay != null)
            dimOverlay.enabled = !canCast;

        // Couleur de l'icône
        if (iconImage != null)
            iconImage.color = canCast ? availableColor : unavailableColor;

        // Cooldown fill (wipe circulaire)
        int cd = owner.GetCooldown(spell);
        if (cooldownFill != null)
        {
            cooldownFill.enabled = onCooldown;
            if (onCooldown)
                cooldownFill.fillAmount = (float)cd / Mathf.Max(1, spell.cooldown);
        }

        if (cooldownText != null)
        {
            cooldownText.enabled = onCooldown;
            cooldownText.text    = onCooldown ? cd.ToString() : "";
        }
    }

    // =========================================================
    // SÉLECTION VISUELLE
    // =========================================================
    public void SetSelected(bool value)
    {
        isSelected = value;
        if (selectionBorder != null)
        {
            selectionBorder.enabled = value;
            selectionBorder.color   = value ? selectedBorderColor : Color.clear;
        }
    }

    // =========================================================
    // EVENTS POINTER
    // =========================================================
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (spell != null && deckUI != null)
            deckUI.ShowTooltip(spell, transform.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (deckUI != null)
            deckUI.HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (deckUI != null)
            deckUI.SelectSlot(slotIndex);
    }
}
