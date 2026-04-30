using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Chef d'orchestre de la scène de combat.
///
/// PIPELINE COMPLET :
///   Phase 0 : L'ArenaGenerator génère la map (generateOnStart = true)
///   Phase 1 : Sélection des passifs — PassiveSelectionScreen pour chaque joueur
///   Phase 2 : Placement — le joueur clique sur une case de spawn pour placer son perso
///   Phase 3 : Combat — TurnManager.StartCombat(), DeckUI bind sur chaque début de tour
///   Phase 4 : Fin de combat — affichage résultat
///
/// SETUP INSPECTOR :
///   - Glisser les deux TacticalCharacter (ils doivent être DÉSACTIVÉS au départ)
///   - Glisser le DeckUI, le PassiveSelectionScreen, l'ArenaGenerator
///   - (Optionnel) Glisser le panneau résultat
/// </summary>
public class CombatInitializer : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================
    public static CombatInitializer Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // =========================================================
    // PHASE PUBLIQUE (lue par PlayerInputHandler)
    // =========================================================
    public enum CombatPhase { WaitingForArena, PassiveSelection, Placement, Combat, End }
    public CombatPhase CurrentPhase => phase;

    // =========================================================
    // RÉFÉRENCES INSPECTOR
    // =========================================================
    [Header("Personnages (désactivés au départ)")]
    public TacticalCharacter player;       // Équipe 1 — contrôlé localement
    public TacticalCharacter opponent;     // Équipe 2 — IA ou second joueur

    [Header("Composants scène")]
    public ArenaGenerator    arenaGenerator;
    public DeckUI            deckUI;
    public PassiveSelectionScreen passiveSelectionScreen;

    [Header("UI Résultat (optionnel)")]
    public GameObject victoryPanel;
    public GameObject defeatPanel;

    [Header("Options")]
    [Tooltip("Si true, l'adversaire est placé automatiquement sur une case de spawn aléatoire.")]
    public bool autoPlaceOpponent = true;
    [Tooltip("Délai (secondes) après la sélection de passif avant le placement.")]
    public float delayAfterPassiveSelection = 1.5f;

    // =========================================================
    // ÉTAT INTERNE
    // =========================================================
    private CombatPhase phase = CombatPhase.WaitingForArena;

    private List<Cell> spawnCellsTeam1;
    private List<Cell> spawnCellsTeam2;

    private bool playerPlaced = false;

    // =========================================================
    // DÉMARRAGE
    // =========================================================
    void Start()
    {
        // L'ArenaGenerator a generateOnStart = true, donc la map est prête en Start()
        // (ArenaGenerator s'exécute en ordre -5, avant CombatInitializer)
        StartCoroutine(InitSequence());
    }

    IEnumerator InitSequence()
    {
        // ── Sécurité : attendre un frame pour être sûr que la grille est initialisée ──
        yield return null;

        // Auto-find de toutes les références non assignées (GOs actifs ET inactifs)
        if (player   == null) player   = FindObjectOfType<TacticalCharacter>(true);
        if (opponent == null)
        {
            var all = FindObjectsOfType<TacticalCharacter>(true);
            foreach (var tc in all)
                if (tc != player) { opponent = tc; break; }
        }
        if (arenaGenerator        == null) arenaGenerator        = FindObjectOfType<ArenaGenerator>(true);
        if (deckUI                == null) deckUI                = FindObjectOfType<DeckUI>(true);
        if (passiveSelectionScreen == null) passiveSelectionScreen = FindObjectOfType<PassiveSelectionScreen>(true);
        if (victoryPanel          == null)
        {
            var go = GameObject.Find("VictoryPanel");
            if (go != null) victoryPanel = go;
        }
        if (defeatPanel == null)
        {
            var go = GameObject.Find("DefeatPanel");
            if (go != null) defeatPanel = go;
        }

        Validate();

        spawnCellsTeam1 = arenaGenerator != null ? arenaGenerator.GetSpawnCells(1) : new List<Cell>();
        spawnCellsTeam2 = arenaGenerator != null ? arenaGenerator.GetSpawnCells(2) : new List<Cell>();

        // ── Phase 1 : Sélection des passifs ─────────────────
        yield return StartCoroutine(RunPassiveSelection());

        // ── Phase 2 : Placement ──────────────────────────────
        yield return StartCoroutine(RunPlacement());

        // ── Phase 3 : Lancement du combat ───────────────────
        StartCombat();
    }

    // =========================================================
    // PHASE 1 — SÉLECTION DES PASSIFS
    // =========================================================
    IEnumerator RunPassiveSelection()
    {
        phase = CombatPhase.PassiveSelection;

        if (passiveSelectionScreen == null)
        {
            Debug.LogWarning("[CombatInitializer] Pas de PassiveSelectionScreen — passifs ignorés.");
            yield break;
        }

        bool selectionDone = false;
        PassiveData chosenPassive = null;

        passiveSelectionScreen.OnPassiveSelected += (passive) =>
        {
            chosenPassive  = passive;
            selectionDone  = true;
        };

        passiveSelectionScreen.Show();

        // Attendre que le joueur confirme (ou que le timer expire)
        yield return new WaitUntil(() => selectionDone);

        // Appliquer le passif au joueur
        if (chosenPassive != null)
        {
            var pm = player.GetComponent<PassiveManager>();
            if (pm != null)
            {
                pm.SetPassive(chosenPassive);
                Debug.Log($"[CombatInitializer] Passif joueur : {chosenPassive.passiveName}");
            }
        }

        passiveSelectionScreen.Hide();

        // Petit délai avant la phase placement
        yield return new WaitForSeconds(delayAfterPassiveSelection);
    }

    // =========================================================
    // PHASE 2 — PLACEMENT
    // =========================================================
    IEnumerator RunPlacement()
    {
        phase = CombatPhase.Placement;

        // Mettre en surbrillance les cases de spawn du joueur (bleu)
        GridManager.Instance.ClearAllHighlights();
        GridManager.Instance.HighlightCells(spawnCellsTeam1, HighlightType.Move);

        Debug.Log("[CombatInitializer] Phase placement — clique sur une case bleue pour placer ton personnage.");

        // ── Placement automatique de l'adversaire ───────────
        if (autoPlaceOpponent && spawnCellsTeam2.Count > 0)
        {
            Cell opponentCell = spawnCellsTeam2[Random.Range(0, spawnCellsTeam2.Count)];
            PlaceCharacter(opponent, opponentCell, teamId: 2);
            Debug.Log($"[CombatInitializer] Adversaire placé en {opponentCell.GridX},{opponentCell.GridY}");
        }

        // ── Attendre que le joueur clique sur une case de spawn ──
        yield return new WaitUntil(() => playerPlaced);

        // Retirer les highlights de placement
        GridManager.Instance.ClearAllHighlights();
    }

    // =========================================================
    // CLIC SUR CASE DE SPAWN (appelé par PlayerInputHandler ou équivalent)
    // =========================================================

    /// <summary>
    /// À appeler depuis ton script de gestion des clics quand le joueur
    /// clique sur une cellule pendant la phase de placement.
    /// </summary>
    public void OnCellClickedDuringPlacement(Cell clickedCell)
    {
        if (phase != CombatPhase.Placement) return;
        if (playerPlaced) return;
        if (!spawnCellsTeam1.Contains(clickedCell)) return;
        if (clickedCell.IsOccupied) return;

        PlaceCharacter(player, clickedCell, teamId: 1);
        playerPlaced = true;

        Debug.Log($"[CombatInitializer] Joueur placé en {clickedCell.GridX},{clickedCell.GridY}");
    }

    // =========================================================
    // PHASE 3 — DÉMARRAGE DU COMBAT
    // =========================================================
    void StartCombat()
    {
        phase = CombatPhase.Combat;

        // Enregistrer les personnages dans le TurnManager
        TurnManager.Instance.RegisterCharacter(player,   teamId: 1);
        TurnManager.Instance.RegisterCharacter(opponent, teamId: 2);

        // Lier le DeckUI au personnage dont c'est le tour
        TurnManager.Instance.OnTurnStart += OnTurnStarted;

        // Écouter la fin du combat
        TurnManager.Instance.OnCombatEnd += OnCombatEnd;

        // Lancer !
        TurnManager.Instance.StartCombat();

        Debug.Log("[CombatInitializer] Combat démarré !");
    }

    // =========================================================
    // CALLBACKS DE COMBAT
    // =========================================================
    void OnTurnStarted(TacticalCharacter character)
    {
        // Le DeckUI se met à jour pour refléter les sorts du personnage actif
        if (deckUI != null)
            deckUI.BindCharacter(character);

        Debug.Log($"[CombatInitializer] Tour de : {character.name}");
    }

    void OnCombatEnd(int winnerTeamId)
    {
        phase = CombatPhase.End;

        TurnManager.Instance.OnTurnStart -= OnTurnStarted;
        TurnManager.Instance.OnCombatEnd -= OnCombatEnd;

        if (deckUI != null) deckUI.UnbindCharacter();

        bool playerWon = (winnerTeamId == 1);
        Debug.Log($"[CombatInitializer] Combat terminé — {(winnerTeamId == -1 ? "Égalité" : $"Équipe {winnerTeamId} gagne")}");

        if (victoryPanel != null) victoryPanel.SetActive(playerWon);
        if (defeatPanel  != null) defeatPanel.SetActive(!playerWon && winnerTeamId != -1);
    }

    // =========================================================
    // UTILITAIRES
    // =========================================================
    void PlaceCharacter(TacticalCharacter character, Cell cell, int teamId)
    {
        character.gameObject.SetActive(true);
        character.Initialize(cell);

        // Appliquer le passif du deck si aucun passif manuel n'a été sélectionné
        var pm = character.GetComponent<PassiveManager>();
        if (pm != null && pm.activePassive == null && character.deck?.Passive != null)
        {
            pm.SetPassive(character.deck.Passive);
        }
    }

    void Validate()
    {
        if (player             == null) Debug.LogError("[CombatInitializer] Aucun TacticalCharacter (player) trouvé dans la scène !");
        if (opponent           == null) Debug.LogWarning("[CombatInitializer] Aucun adversaire trouvé — combat en solo uniquement.");
        if (arenaGenerator     == null) Debug.LogWarning("[CombatInitializer] ArenaGenerator absent — zones de spawn vides.");
        if (GridManager.Instance  == null) Debug.LogError("[CombatInitializer] GridManager absent de la scène !");
        if (TurnManager.Instance  == null) Debug.LogError("[CombatInitializer] TurnManager absent de la scène !");
    }
}
