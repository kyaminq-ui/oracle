using UnityEngine;

/// <summary>
/// Gestion complète des clics souris sur la grille isométrique.
///
/// COMPORTEMENT PAR PHASE :
///   Placement → clic gauche sur case bleue = placement du personnage
///   Combat    → clic gauche sur case = déplacement OU lancer de sort selon contexte
///               clic droit            = annuler le sort sélectionné
///               survol                = preview de la zone AoE
///
/// SETUP INSPECTOR :
///   - Glisser le TacticalCharacter du joueur local
///   - Glisser la caméra principale (ou laisser vide = Camera.main)
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    // =========================================================
    // RÉFÉRENCES INSPECTOR
    // =========================================================
    [Header("Personnage local")]
    public TacticalCharacter character;

    [Header("Caméra (laisse vide = Camera.main)")]
    public Camera cam;

    // =========================================================
    // ÉTAT INTERNE
    // =========================================================
    private SpellCaster spellCaster;
    private Cell        lastHoveredCell;

    // =========================================================
    // INITIALISATION
    // =========================================================
    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        if (character != null)
            spellCaster = character.GetComponent<SpellCaster>();
    }

    // =========================================================
    // UPDATE — LECTURE DES INPUTS
    // =========================================================
    void Update()
    {
        if (cam == null || GridManager.Instance == null) return;

        Cell hoveredCell = GetCellUnderMouse();

        HandleHover(hoveredCell);
        HandleLeftClick(hoveredCell);
        HandleRightClick();

        lastHoveredCell = hoveredCell;
    }

    // =========================================================
    // SURVOL — preview AoE
    // =========================================================
    void HandleHover(Cell cell)
    {
        if (cell == lastHoveredCell) return;

        if (spellCaster != null && spellCaster.HasSpellSelected)
        {
            if (cell != null)
                spellCaster.PreviewAoE(cell);
            else
                spellCaster.ClearPreview();
        }

        // Hover visuel sur la grille
        if (cell != null)
            GridManager.Instance.SetHoveredCell(cell.GridX, cell.GridY);
    }

    // =========================================================
    // CLIC GAUCHE
    // =========================================================
    void HandleLeftClick(Cell cell)
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (cell == null) return;

        var combatPhase = CombatInitializer.Instance != null
            ? CombatInitializer.Instance.CurrentPhase
            : CombatInitializer.CombatPhase.Combat;

        switch (combatPhase)
        {
            // ── Phase placement ──────────────────────────────
            case CombatInitializer.CombatPhase.Placement:
                CombatInitializer.Instance.OnCellClickedDuringPlacement(cell);
                break;

            // ── Phase combat ─────────────────────────────────
            case CombatInitializer.CombatPhase.Combat:
                HandleCombatClick(cell);
                break;
        }
    }

    void HandleCombatClick(Cell cell)
    {
        if (character == null) return;

        // Pas le tour du joueur → ignorer
        if (TurnManager.Instance != null &&
            TurnManager.Instance.CurrentCharacter != character)
            return;

        // Un sort est sélectionné → tenter de le lancer
        if (spellCaster != null && spellCaster.HasSpellSelected)
        {
            spellCaster.TryCast(cell);
            return;
        }

        // Pas de sort → tenter de se déplacer
        if (character.CanMoveTo(cell))
        {
            character.MoveToCell(cell);
        }
    }

    // =========================================================
    // CLIC DROIT — annuler le sort sélectionné
    // =========================================================
    void HandleRightClick()
    {
        if (!Input.GetMouseButtonDown(1)) return;

        if (spellCaster != null && spellCaster.HasSpellSelected)
        {
            spellCaster.CancelSpell();

            // Remettre les highlights de déplacement
            if (character != null && TurnManager.Instance?.CurrentCharacter == character)
                HighlightReachableCells();
        }
    }

    // =========================================================
    // HIGHLIGHTS DE DÉPLACEMENT
    // =========================================================

    /// <summary>
    /// Affiche les cases accessibles au personnage actif.
    /// Appelé automatiquement au début du tour si ce personnage joue.
    /// </summary>
    public void HighlightReachableCells()
    {
        if (character == null) return;

        GridManager.Instance.ClearAllHighlights();

        if (!character.IsAlive || character.CurrentCell == null) return;

        var reachable = new Pathfinding().GetReachableCells(
            character.CurrentCell,
            character.CurrentPM
        );

        GridManager.Instance.HighlightCells(reachable, HighlightType.Move);
    }

    // =========================================================
    // UTILITAIRE — CELLULE SOUS LA SOURIS
    // =========================================================
    Cell GetCellUnderMouse()
    {
        Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0f;
        return GridManager.Instance.GetCellFromWorldPosition(worldPos);
    }

    // =========================================================
    // ABONNEMENT AUX ÉVÉNEMENTS DE TOUR
    // =========================================================
    void OnEnable()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStart += OnTurnStart;
    }

    void OnDisable()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnStart -= OnTurnStart;
    }

    void OnTurnStart(TacticalCharacter who)
    {
        // Mettre à jour la référence SpellCaster si le personnage a changé
        if (character != null)
            spellCaster = character.GetComponent<SpellCaster>();

        // Afficher les cases accessibles seulement si c'est notre tour
        if (who == character)
            HighlightReachableCells();
        else
            GridManager.Instance.ClearAllHighlights();
    }
}
