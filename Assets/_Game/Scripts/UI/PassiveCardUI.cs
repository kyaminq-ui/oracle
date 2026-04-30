using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PassiveCardUI : MonoBehaviour
{
    [Header("Références")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Image cardBackground;

    [Header("Couleurs")]
    public Color normalColor    = new Color(0.12f, 0.12f, 0.18f);
    public Color selectedColor  = new Color(0.55f, 0.38f, 0.07f);
    public Color hoverColor     = new Color(0.20f, 0.20f, 0.30f);

    private PassiveData data;
    private bool selected = false;

    public PassiveData Data => data;
    public bool IsSelected => selected;

    public void Setup(PassiveData passive)
    {
        data = passive;
        selected = false;
        SetBackground(normalColor);

        if (iconImage != null)
        {
            iconImage.sprite = passive.icon;
            iconImage.enabled = passive.icon != null;
        }
        if (nameText != null)        nameText.text = passive.passiveName;
        if (descriptionText != null) descriptionText.text = passive.description;
    }

    public void SetSelected(bool value)
    {
        selected = value;
        SetBackground(selected ? selectedColor : normalColor);
    }

    public void OnPointerEnter()
    {
        if (!selected) SetBackground(hoverColor);
    }

    public void OnPointerExit()
    {
        if (!selected) SetBackground(normalColor);
    }

    private void SetBackground(Color c)
    {
        if (cardBackground != null) cardBackground.color = c;
    }
}
