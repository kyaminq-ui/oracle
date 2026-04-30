using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PassiveSelectionScreen : MonoBehaviour
{
    // =========================================================
    // ÉVÉNEMENT
    // =========================================================
    public event System.Action<PassiveData> OnPassiveSelected;

    // =========================================================
    // RÉFÉRENCES
    // =========================================================
    [Header("Données")]
    public PassivePool passivePool;

    [Header("Cartes (5 slots)")]
    public List<PassiveCardUI> cards = new List<PassiveCardUI>();

    [Header("Timer")]
    public Image timerFill;
    public TextMeshProUGUI timerText;
    public float selectionDuration = 30f;

    [Tooltip("Nombre de passifs proposés (max = nombre de PassiveCardUI configurés). Spec : 9.")]
    [Range(1, 18)]
    public int offeredPassiveCount = 9;

    [Header("Bouton confirmer")]
    public Button confirmButton;

    [Header("Récap")]
    public GameObject recapPanel;
    public TextMeshProUGUI recapText;

    // =========================================================
    // ÉTAT
    // =========================================================
    private List<PassiveData> displayedPassives = new List<PassiveData>();
    private PassiveCardUI selectedCard = null;
    private float timeRemaining;
    private bool selectionDone = false;

    // =========================================================
    // OUVERTURE DE L'ÉCRAN
    // =========================================================
    public void Show()
    {
        gameObject.SetActive(true);
        FixStretchLayoutIfBroken();

        // Toujours au-dessus des autres éléments du même Canvas (DeckUI, etc.)
        transform.SetAsLastSibling();
        EnsureFrontCanvas();

        selectionDone = false;
        selectedCard  = null;
        timeRemaining = selectionDuration;

        if (passivePool == null)
        {
            Debug.LogError("[PassiveSelectionScreen] PassivePool non assigné !");
            return;
        }

        int offer = Mathf.Min(cards.Count, Mathf.Max(1, offeredPassiveCount));
        displayedPassives = passivePool.GetRandom(offer);
        if (displayedPassives.Count == 0)
        {
            Debug.LogError("[PassiveSelectionScreen] PassivePool vide — ajoute des PassiveData dans All Passives.");
            return;
        }
        for (int i = 0; i < cards.Count; i++)
        {
            bool hasData = i < displayedPassives.Count;
            cards[i].gameObject.SetActive(hasData);
            if (hasData)
            {
                cards[i].Setup(displayedPassives[i]);
                int idx = i;
                // Configurer le bouton de la carte
                var btn = cards[i].GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => SelectCard(idx));
                }
            }
        }

        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(Confirm);
        }

        if (recapPanel != null) recapPanel.SetActive(false);
    }

    // =========================================================
    // UPDATE — TIMER
    // =========================================================
    void Update()
    {
        if (!gameObject.activeSelf || selectionDone) return;

        timeRemaining -= Time.deltaTime;

        float ratio = Mathf.Clamp01(timeRemaining / selectionDuration);
        if (timerFill != null)  timerFill.fillAmount = ratio;
        if (timerText != null)  timerText.text = Mathf.CeilToInt(timeRemaining).ToString();

        if (timeRemaining <= 0f)
            AutoSelect();
    }

    // =========================================================
    // SÉLECTION
    // =========================================================
    private void SelectCard(int index)
    {
        if (selectionDone || index >= cards.Count) return;

        if (selectedCard != null) selectedCard.SetSelected(false);
        selectedCard = cards[index];
        selectedCard.SetSelected(true);

        if (confirmButton != null) confirmButton.interactable = true;
    }

    private void Confirm()
    {
        if (selectedCard == null) { AutoSelect(); return; }
        FinalizeSelection(selectedCard.Data);
    }

    private void AutoSelect()
    {
        if (selectionDone) return;
        int idx = Random.Range(0, displayedPassives.Count);
        FinalizeSelection(displayedPassives[idx]);
    }

    private void FinalizeSelection(PassiveData passive)
    {
        selectionDone = true;
        ShowRecap(passive);
        OnPassiveSelected?.Invoke(passive);
    }

    // =========================================================
    // RÉCAP
    // =========================================================
    private void ShowRecap(PassiveData passive)
    {
        if (recapPanel != null)
        {
            recapPanel.SetActive(true);
            if (recapText != null)
                recapText.text = $"Passif choisi : {passive.passiveName}";
        }
    }

    public void Hide() => gameObject.SetActive(false);

    /// <summary>
    /// Anciennes versions du builder utilisaient anchor (0,0)-(0,0) + size 1×1 → invisible.
    /// </summary>
    void FixStretchLayoutIfBroken()
    {
        var rt = GetComponent<RectTransform>();
        if (rt.anchorMax.x <= 0.01f && rt.anchorMax.y <= 0.01f &&
            rt.sizeDelta.x <= 4f && rt.sizeDelta.y <= 4f)
        {
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.offsetMin        = Vector2.zero;
            rt.offsetMax        = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        var bgTr = transform.Find("Background") as RectTransform;
        if (bgTr != null &&
            bgTr.anchorMax.x <= 0.01f && bgTr.anchorMax.y <= 0.01f &&
            bgTr.sizeDelta.x <= 4f && bgTr.sizeDelta.y <= 4f)
        {
            StretchRect(bgTr);
        }
    }

    static void StretchRect(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// Canvas imbriqué avec tri prioritaire : sans ça, un DeckUI plein écran
    /// ou un voisin créé après peut recouvrir tout l'écran des passifs.
    /// </summary>
    void EnsureFrontCanvas()
    {
        var c = GetComponent<Canvas>();
        if (c == null) c = gameObject.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 5000;

        if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    }
}
