using UnityEngine;

/// <summary>
/// Paramètres de génération d'une arène 1v1.
/// Créer via : Clic droit → Create → Arena → Arena Configuration
/// Un seul asset suffit pour le MVP ; duplique-le pour tester des variantes.
/// </summary>
[CreateAssetMenu(fileName = "ArenaConfig_1v1", menuName = "Arena/Arena Configuration")]
public class ArenaConfig : ScriptableObject
{
    // =========================================================
    // DIMENSIONS
    // =========================================================

    [Header("=== DIMENSIONS ===")]
    [Range(9, 25)]
    [Tooltip("Nombre de colonnes (axe X). " +
             "Doit être impair pour avoir une colonne centrale. " +
             "Recommandé : 11, 13, 15.")]
    public int arenaWidth = 13;

    [Range(7, 20)]
    [Tooltip("Nombre de lignes (axe Y). " +
             "Doit être impair pour avoir une ligne centrale. " +
             "Recommandé : 9, 11.")]
    public int arenaHeight = 11;

    // =========================================================
    // ZONES DE SPAWN
    // =========================================================

    [Header("=== ZONES DE SPAWN ===")]
    [Range(1, 4)]
    [Tooltip("Profondeur (en colonnes) de la zone de spawn de chaque équipe. " +
             "Équipe 1 = colonnes 0 à depth-1. " +
             "Équipe 2 = colonnes (width-depth) à (width-1).")]
    public int spawnZoneDepth = 2;

    // =========================================================
    // OBSTACLES
    // =========================================================

    [Header("=== OBSTACLES ===")]
    [Range(0f, 0.35f)]
    [Tooltip("Densité d'obstacles dans la zone de combat centrale. " +
             "0 = aucun obstacle. 0.35 = 35% des cases sont bloquées. " +
             "Recommandé entre 0.10 et 0.20 pour un bon équilibre.")]
    public float obstacleDensity = 0.15f;

    [Tooltip("Activer la symétrie miroir X des obstacles. " +
             "FORTEMENT recommandé pour un jeu 1v1 équilibré.")]
    public bool mirrorSymmetry = true;

    [Range(0, 3)]
    [Tooltip("Distance minimale (en cases) entre un obstacle et le bord d'une zone de spawn. " +
             "Évite de bloquer l'accès à la zone de combat dès la sortie du spawn.")]
    public int minClearanceFromSpawn = 1;

    [Range(1, 3)]
    [Tooltip("Aucune case d'obstacle à moins de N cases du bord extérieur de l'arène complète " +
             "(x=0, y=0, derniers X/Y). Valeur recommandée : 1 = pas d'obstacles au pourtour de la carte.")]
    public int obstacleBorderMargin = 1;

    // =========================================================
    // VARIANTES DE TERRAIN
    // =========================================================

    [Header("=== VARIANTES DE TERRAIN ===")]
    [Range(0f, 0.20f)]
    [Tooltip("Probabilité qu'une case de sol devienne une variante sang (GROUNDBLOOD).")]
    public float bloodTileChance = 0.06f;

    [Range(0f, 0.20f)]
    [Tooltip("Probabilité qu'une case de sol devienne une variante herbe (GROUNDGRASS).")]
    public float grassTileChance = 0.08f;

    // =========================================================
    // GÉNÉRATION PROCÉDURALE
    // =========================================================

    [Header("=== GÉNÉRATION ===")]
    [Tooltip("Graine de génération aléatoire. " +
             "-1 = nouvelle seed aléatoire à chaque génération. " +
             "Toute valeur ≥ 0 = résultat identique et reproductible.")]
    public int seed = -1;

    [Range(50, 500)]
    [Tooltip("Nombre maximum de tentatives pour placer les obstacles. " +
             "Augmente si la densité est élevée mais que peu d'obstacles sont placés.")]
    public int maxObstaclePlacementAttempts = 300;

    // =========================================================
    // RÉFÉRENCES
    // =========================================================

    [Header("=== RÉFÉRENCES ===")]
    [Tooltip("Registre des sprites de tiles (GROUND, OBSTACLE, etc.). " +
             "Créer un TileSpriteRegistry et l'assigner ici.")]
    public TileSpriteRegistry tileRegistry;

    [Header("=== BORDURES PÉRIMÈTRIQUES ===")]
    [Tooltip("Affiche les EDGE1–EDGE12 sur le contour de la grille.")]
    public bool renderPerimeterEdges = true;

    // =========================================================
    // VALIDATION
    // =========================================================

    /// <summary>Retourne true si la configuration est utilisable pour générer une arène.</summary>
    public bool IsValid(out string errorMessage)
    {
        if (tileRegistry == null)
        {
            errorMessage = "TileSpriteRegistry non assigné dans ArenaConfig !";
            return false;
        }

        if (arenaWidth <= spawnZoneDepth * 2 + 2)
        {
            errorMessage = $"arenaWidth ({arenaWidth}) trop petit pour 2 zones de spawn " +
                           $"de profondeur {spawnZoneDepth} avec une zone de combat.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
