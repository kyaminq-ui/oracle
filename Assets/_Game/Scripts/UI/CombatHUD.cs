using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD de combat — roadmap 4.2.1
/// Barre haute : HP équipe A | Timer | HP équipe B
/// Barre basse : passif actif | PA / PM | zone Deck (DeckUI) | Fin de tour
/// </summary>
public class CombatHUD : MonoBehaviour
{
    // =========================================================
    // PERSONNAGES (auto si vides)
    // =========================================================
    [Header("Combatants")]
    public TacticalCharacter teamACharacter;
    public TacticalCharacter teamBCharacter;
    [Tooltip("Celui qui appuie sur Fin de tour (souvent = team A en local).")]
    public TacticalCharacter localPlayerCharacter;

    // =========================================================
    // HAUT — HP
    // =========================================================
    [Header("Équipe A (gauche)")]
    public TextMeshProUGUI teamALabel;
    public Image            teamAHpFill;
    public TextMeshProUGUI  teamAHpValue;

    [Header("Équipe B (droite)")]
    public TextMeshProUGUI teamBLabel;
    public Image            teamBHpFill;
    public TextMeshProUGUI  teamBHpValue;

    // =========================================================
    // BAS — Passif / ressources / fin de tour
    // =========================================================
    [Header("Passif (tour actif)")]
    public Image            passiveIcon;
    public TextMeshProUGUI  passiveNameText;

    [Header("PA / PM (tour actif)")]
    public TextMeshProUGUI  paText;
    public TextMeshProUGUI  pmText;

    [Header("Actions")]
    public Button endTurnButton;

    // =========================================================
    // COULEURS
    // =========================================================
    public Color accentColor = new Color(0.788f, 0.659f, 0.298f, 1f);

    TacticalCharacter _subPA, _subPM, _subHP_A, _subHP_B;

    void Awake()
    {
        AutoFindCharacters();
        if (localPlayerCharacter == null) localPlayerCharacter = teamACharacter;
    }

    void Start()
    {
        WireHpStatic();
        WireEndTurnButton();
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStart += OnTurnStart;
        OnTurnStart(TurnManager.Instance != null ? TurnManager.Instance.CurrentCharacter : null);
    }

    void OnDestroy()
    {
        UnsubscribeAll();
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStart -= OnTurnStart;
    }

    void AutoFindCharacters()
    {
        if (teamACharacter != null && teamBCharacter != null) return;
        var all = FindObjectsOfType<TacticalCharacter>(true);
        if (all.Length >= 1 && teamACharacter == null) teamACharacter = all[0];
        if (all.Length >= 2 && teamBCharacter == null)
        {
            foreach (var c in all)
                if (c != teamACharacter) { teamBCharacter = c; break; }
        }
    }

    void WireHpStatic()
    {
        if (teamACharacter != null)
        {
            teamACharacter.OnHPChanged += OnHpA;
            _subHP_A = teamACharacter;
            if (teamALabel != null) teamALabel.text = teamACharacter.name;
            RefreshHpBar(teamACharacter, teamAHpFill, teamAHpValue);
        }
        if (teamBCharacter != null)
        {
            teamBCharacter.OnHPChanged += OnHpB;
            _subHP_B = teamBCharacter;
            if (teamBLabel != null) teamBLabel.text = teamBCharacter.name;
            RefreshHpBar(teamBCharacter, teamBHpFill, teamBHpValue);
        }
    }

    void OnHpA(int cur, int max) => RefreshHpBar(teamACharacter, teamAHpFill, teamAHpValue);
    void OnHpB(int cur, int max) => RefreshHpBar(teamBCharacter, teamBHpFill, teamBHpValue);

    static void RefreshHpBar(TacticalCharacter ch, Image fill, TextMeshProUGUI valueText)
    {
        if (ch?.stats == null) return;
        int max = ch.stats.maxHP;
        int cur = ch.CurrentHP;
        if (fill != null)
        {
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            float t     = max > 0 ? Mathf.Clamp01((float)cur / max) : 0f;
            fill.fillAmount = t;
        }
        if (valueText != null) valueText.text = $"{cur} / {max}";
    }

    void WireEndTurnButton()
    {
        if (endTurnButton == null) return;
        endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
        endTurnButton.onClick.AddListener(OnEndTurnClicked);
    }

    void OnEndTurnClicked()
    {
        if (TurnManager.Instance == null) return;
        var cur = TurnManager.Instance.CurrentCharacter;
        if (cur == null) return;
        if (localPlayerCharacter != null && cur != localPlayerCharacter) return;
        TurnManager.Instance.EndTurn();
    }

    void OnTurnStart(TacticalCharacter active)
    {
        UnsubscribeResources();

        if (active == null) return;

        active.OnPAChanged += OnPAChanged;
        active.OnPMChanged += OnPMChanged;
        _subPA = active;
        _subPM = active;

        OnPAChanged(active.CurrentPA, active.stats != null ? active.stats.maxPA : 8);
        OnPMChanged(active.CurrentPM, active.stats != null ? active.stats.maxPM : 3);

        RefreshPassiveDisplay(active);

        if (endTurnButton != null)
        {
            bool myTurn = localPlayerCharacter == null || active == localPlayerCharacter;
            endTurnButton.interactable = myTurn && TurnManager.Instance != null && TurnManager.Instance.IsCombatActive;
        }
    }

    void UnsubscribeResources()
    {
        if (_subPA != null) _subPA.OnPAChanged -= OnPAChanged;
        if (_subPM != null) _subPM.OnPMChanged -= OnPMChanged;
        _subPA = _subPM = null;
    }

    void UnsubscribeAll()
    {
        UnsubscribeResources();
        if (_subHP_A != null) _subHP_A.OnHPChanged -= OnHpA;
        if (_subHP_B != null) _subHP_B.OnHPChanged -= OnHpB;
        _subHP_A = _subHP_B = null;
    }

    void OnPAChanged(int cur, int max)
    {
        if (paText == null) return;
        paText.text = $"PA: {cur}/{max}";
    }

    void OnPMChanged(int cur, int max)
    {
        if (pmText == null) return;
        pmText.text = $"PM: {cur}/{max}";
    }

    void RefreshPassiveDisplay(TacticalCharacter ch)
    {
        var pm = ch != null ? ch.GetComponent<PassiveManager>() : null;
        var p  = pm != null ? pm.activePassive : null;

        if (passiveNameText != null)
            passiveNameText.text = p != null ? p.passiveName : "—";

        if (passiveIcon != null)
        {
            passiveIcon.sprite  = p != null ? p.icon : null;
            passiveIcon.enabled = p != null && p.icon != null;
        }
    }
}
