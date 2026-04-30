using UnityEngine;

/// <summary>
/// Registre centralisé de tous les sprites de tiles.
/// Assigne ici tes sprites depuis Assets/_Game/Sprites/Tiles ou Sprites/NewTile.
/// Créer via : Clic droit → Create → Arena → Tile Sprite Registry
///
/// IMPORTANT — Import des GIF dans Unity :
/// Sélectionne chaque sprite dans le Project, puis dans l'Inspector :
///   Texture Type      → Sprite (2D and UI)
///   Sprite Mode       → Single
///   Filter Mode       → Point (no filter)   ← pixel art
///   Compression       → None
///   Pixels Per Unit   → correspond à ton GridConfig (32 ou 64)
/// </summary>
[CreateAssetMenu(fileName = "TileSpriteRegistry", menuName = "Arena/Tile Sprite Registry")]
public class TileSpriteRegistry : ScriptableObject
{
    /// <summary>EDGE1 à EDGE12 (indices 0..11). Remplace l'ancienne triple bordure haute.</summary>
    public const int EdgeTileCount = 12;

    // =========================================================
    // SOLS
    // =========================================================

    [Header("=== TILES DE SOL (GROUND1 → GROUND12) ===")]
    [Tooltip("Glisse GROUND1 à GROUND12 dans ce tableau. " +
             "L'arène les pioche aléatoirement pour la diversité visuelle.")]
    public Sprite[] groundTiles;

    [Header("=== VARIANTES SANG — une image ou pool aléatoire ===")]
    [Tooltip("Une seule texture sang (fallback).")]
    public Sprite groundBloodTile;

    [Tooltip("Si non vide : remplace groundBloodTile par un tirage aléatoire (ex. GROUND_BLOOD_RUST_VARIANT*).")]
    public Sprite[] groundBloodVariants;

    [Header("=== VARIANTES « HERBE / MAUDIT » — une image ou pool ===")]
    [Tooltip("Une seule texture herbe (fallback).")]
    public Sprite groundGrassTile;

    [Tooltip("Si non vide : variante curse/glow au lieu du tile herbe fixe.")]
    public Sprite[] groundGrassOrCursedVariants;

    [Header("=== OVERLAYS DÉCOR OPTIONNEL (case sol) ===")]
    [Tooltip("Si non vide, une petite proportion de cases sol reçoit un décor superposé (rendu ArenaGenerator).")]
    [Range(0f, 0.35f)] public float groundDecorationChance = 0f;

    public Sprite[] groundDecorationTiles;

    // =========================================================
    // OBSTACLES
    // =========================================================

    [Header("=== OBSTACLES (GROUND_OBSTACLE* …) ===")]
    [Tooltip("Glisse tes sprites obstacle (ex. jusqu'à OBSTACLE12 depuis NewTile).")]
    public Sprite[] obstacleTiles;

    // =========================================================
    // BORDURES — EDGE 1 à 12 sur le périmètre visible
    // =========================================================

    [Header("=== BORDURES (EDGE1 → EDGE12) ===")]
    [Tooltip("12 segments décoratifs le long du pourtour de l'arène " +
             "(index 0 = EDGE 1, … index 11 = EDGE 12). Laisser un slot vide désactive ce segment.")]
    public Sprite[] edgeTiles = new Sprite[EdgeTileCount];

    [Tooltip("Trie des sprites EDGE au dessus du sol (sprite secondaire sur périmètre).")]
    public int edgeSortingOrderBoost = 2;

    // =========================================================
    // COULEURS DE SPAWN (overlay sur le sol)
    // =========================================================

    [Header("=== COULEURS DE SPAWN ===")]
    [Tooltip("Teinte de la zone de spawn Équipe 1 (gauche). " +
             "Appliquée en overlay sur le CellHighlight existant.")]
    public Color spawnTeam1Color = new Color(0.25f, 0.45f, 1f, 0.40f);

    [Tooltip("Teinte de la zone de spawn Équipe 2 (droite).")]
    public Color spawnTeam2Color = new Color(1f, 0.30f, 0.20f, 0.40f);

    // =========================================================
    // HELPERS — Accès aléatoire
    // =========================================================

    /// <summary>Retourne un tile de sol aléatoire depuis le tableau groundTiles.</summary>
    public Sprite GetRandomGroundTile(System.Random rng)
    {
        if (groundTiles == null || groundTiles.Length == 0) return null;
        return groundTiles[rng.Next(groundTiles.Length)];
    }

    /// <summary>Retourne un tile d'obstacle aléatoire depuis le tableau obstacleTiles.</summary>
    public Sprite GetRandomObstacleTile(System.Random rng)
    {
        if (obstacleTiles == null || obstacleTiles.Length == 0) return null;
        return obstacleTiles[rng.Next(obstacleTiles.Length)];
    }

    /// <summary>Bordure pour la case (x,y) : index périmètre → EDGE 1..12 cyclique.</summary>
    public Sprite GetEdgeOverlaySprite(int perimeterStepIndex)
    {
        if (edgeTiles == null || edgeTiles.Length == 0) return null;
        int i = perimeterStepIndex % EdgeTileCount;
        if (i < 0) i += EdgeTileCount;
        return i < edgeTiles.Length ? edgeTiles[i] : null;
    }

    /// <summary>
    /// Indice le long du contour (0 … P-1) ou -1 si la case n'est pas au bord de l'arène.
    /// Sens : bas gauche → droite, puis haut droit, puis haut droite → gauche, puis gauche bas.
    /// </summary>
    public static int GetPerimeterStep(int x, int y, int w, int h)
    {
        if (w <= 0 || h <= 0) return -1;
        bool onBottom = y == 0;
        bool onTop    = y == h - 1;
        bool onLeft   = x == 0;
        bool onRight  = x == w - 1;

        if (!onBottom && !onTop && !onLeft && !onRight) return -1;

        int step = 0;
        for (int xi = 0; xi < w; xi++, step++)
        {
            if (xi == x && y == 0) return step;
        }
        for (int yi = 1; yi < h; yi++, step++)
        {
            if (x == w - 1 && yi == y) return step;
        }
        if (h > 1)
        {
            for (int xi = w - 2; xi >= 0; xi--, step++)
            {
                if (xi == x && y == h - 1) return step;
            }
        }
        if (w > 1 && h > 2)
        {
            for (int yi = h - 2; yi >= 1; yi--, step++)
            {
                if (x == 0 && yi == y) return step;
            }
        }
        return -1;
    }

    /// <summary>Retourne le sprite de sol correspondant au CellTileType donné.</summary>
    public Sprite GetGroundSpriteForType(CellTileType type, System.Random rng)
    {
        switch (type)
        {
            case CellTileType.GroundBlood:
                if (groundBloodVariants != null && groundBloodVariants.Length > 0)
                    return groundBloodVariants[rng.Next(groundBloodVariants.Length)];
                return groundBloodTile != null ? groundBloodTile : GetRandomGroundTile(rng);

            case CellTileType.GroundGrass:
                if (groundGrassOrCursedVariants != null && groundGrassOrCursedVariants.Length > 0)
                    return groundGrassOrCursedVariants[rng.Next(groundGrassOrCursedVariants.Length)];
                return groundGrassTile != null ? groundGrassTile : GetRandomGroundTile(rng);

            default:
                return GetRandomGroundTile(rng);
        }
    }

    public Sprite MaybeGetDecorationOverlay(System.Random rng)
    {
        if (groundDecorationTiles == null || groundDecorationTiles.Length == 0) return null;
        if (rng.NextDouble() >= groundDecorationChance) return null;
        return groundDecorationTiles[rng.Next(groundDecorationTiles.Length)];
    }
}
