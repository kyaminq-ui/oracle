using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimerUI : MonoBehaviour
{
    // =========================================================
    // RÉFÉRENCES
    // =========================================================
    [Header("Références")]
    public Image fillImage;
    public TextMeshProUGUI timeText;

    [Header("Feedback sonore (5 dernières secondes)")]
    public AudioSource audioSource;
    public AudioClip tickClip;

    // =========================================================
    // COULEURS  (spec : vert >15s, orange >8s, rouge <8s)
    // =========================================================
    [Header("Couleurs")]
    public Color colorGreen  = new Color(0.20f, 0.80f, 0.20f);
    public Color colorOrange = new Color(1.00f, 0.50f, 0.00f);
    public Color colorRed    = new Color(0.80f, 0.10f, 0.10f);

    // =========================================================
    // SEUILS (secondes restantes)
    // =========================================================
    [Header("Seuils (secondes restantes)")]
    public float thresholdGreenAbove = 15f;
    public float thresholdOrangeAbove = 8f;

    [Header("Urgence")]
    [Tooltip("Pulsation + ticks — spec : < 5 s")]
    public float pulseUnderSeconds = 5f;

    private float maxDuration;
    private int lastTickSecond = -1;

    // =========================================================
    // INITIALISATION
    // =========================================================
    void Start()
    {
        if (TurnManager.Instance == null) return;
        maxDuration = TurnManager.Instance.turnDuration;
        TurnManager.Instance.OnTurnStart += _ => OnNewTurn();
    }

    private void OnNewTurn()
    {
        maxDuration = TurnManager.Instance.turnDuration;
        lastTickSecond = -1;
        transform.localScale = Vector3.one;
    }

    // =========================================================
    // MISE À JOUR
    // =========================================================
    void Update()
    {
        if (TurnManager.Instance == null || !TurnManager.Instance.IsCombatActive) return;

        float remaining = TurnManager.Instance.TimeRemaining;
        float ratio = (maxDuration > 0f) ? remaining / maxDuration : 0f;

        if (fillImage != null)
            fillImage.fillAmount = ratio;

        if (timeText != null)
            timeText.text = Mathf.CeilToInt(remaining).ToString();

        Color targetColor;
        if (remaining > thresholdGreenAbove)          targetColor = colorGreen;
        else if (remaining > thresholdOrangeAbove)    targetColor = colorOrange;
        else                                          targetColor = colorRed;

        if (fillImage != null) fillImage.color = targetColor;
        if (timeText != null)  timeText.color  = targetColor;

        // Pulsation d'urgence (< 5 s)
        if (remaining <= pulseUnderSeconds && remaining > 0f)
        {
            float pulse = 1f + 0.10f * Mathf.Sin(Time.time * 12f);
            transform.localScale = Vector3.one * pulse;

            int sec = Mathf.CeilToInt(remaining);
            if (sec != lastTickSecond && sec > 0)
            {
                lastTickSecond = sec;
                PlayTick();
            }
        }
        else
        {
            transform.localScale = Vector3.one;
            lastTickSecond = -1;
        }
    }

    void PlayTick()
    {
        if (tickClip == null) return;
        if (audioSource != null)
            audioSource.PlayOneShot(tickClip);
        else
            AudioSource.PlayClipAtPoint(tickClip, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
    }
}
