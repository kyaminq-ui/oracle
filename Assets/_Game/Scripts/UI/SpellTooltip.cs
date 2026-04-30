using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpellTooltip : MonoBehaviour
{
    // =========================================================
    // RÉFÉRENCES
    // =========================================================
    [Header("Textes")]
    public TextMeshProUGUI spellNameText;
    public TextMeshProUGUI paCostText;
    public TextMeshProUGUI rangeText;
    public TextMeshProUGUI cooldownText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI synergyText;

    [Header("Icône")]
    public Image iconImage;

    [Header("Panel")]
    public RectTransform tooltipPanel;
    public Canvas rootCanvas;

    // =========================================================
    // AFFICHAGE
    // =========================================================
    public void Show(SpellData spell, Vector3 anchorWorldPos)
    {
        gameObject.SetActive(true);

        if (spellNameText != null)  spellNameText.text  = spell.spellName;
        if (paCostText != null)     paCostText.text     = $"Coût : {spell.paCost} PA";
        if (cooldownText != null)   cooldownText.text   = spell.cooldown > 0 ? $"Recharge : {spell.cooldown} tour(s)" : "Pas de recharge";
        if (descriptionText != null) descriptionText.text = spell.description;
        if (synergyText != null)
        {
            bool hasSynergy = !string.IsNullOrEmpty(spell.synergyDescription);
            synergyText.gameObject.SetActive(hasSynergy);
            synergyText.text = hasSynergy ? $"Synergie : {spell.synergyDescription}" : "";
        }

        if (rangeText != null)
        {
            if (spell.isMeleeOnly)
                rangeText.text = "Portée : Corps à corps";
            else
                rangeText.text = $"Portée : {spell.rangeMin}-{spell.rangeMax}";
        }

        if (iconImage != null)
        {
            iconImage.sprite  = spell.icon;
            iconImage.enabled = spell.icon != null;
        }

        PositionNearAnchor(anchorWorldPos);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // =========================================================
    // POSITIONNEMENT (au-dessus du slot, reste dans l'écran)
    // =========================================================
    private void PositionNearAnchor(Vector3 worldPos)
    {
        if (tooltipPanel == null || rootCanvas == null) return;

        Vector2 screenPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(),
            RectTransformUtility.WorldToScreenPoint(null, worldPos),
            rootCanvas.worldCamera,
            out screenPos
        );

        // Décaler vers le haut
        screenPos.y += tooltipPanel.rect.height + 20f;

        // Garder dans les limites du canvas
        RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
        float halfW = tooltipPanel.rect.width  * 0.5f;
        float halfH = tooltipPanel.rect.height * 0.5f;
        screenPos.x = Mathf.Clamp(screenPos.x, -canvasRect.rect.width  * 0.5f + halfW, canvasRect.rect.width  * 0.5f - halfW);
        screenPos.y = Mathf.Clamp(screenPos.y, -canvasRect.rect.height * 0.5f + halfH, canvasRect.rect.height * 0.5f - halfH);

        tooltipPanel.anchoredPosition = screenPos;
    }
}
