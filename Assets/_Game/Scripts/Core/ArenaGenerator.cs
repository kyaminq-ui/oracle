using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Génère procéduralement une arène de combat 1v1 isométrique.
///
/// PIPELINE DE GÉNÉRATION :
///   Awake  → synchronise les dimensions ArenaConfig → GridConfig (avant GridManager)
///   Start  → Generate() si generateOnStart est activé
///
/// ALGORITHME :
///   1. Remplit toutes les cases avec des tiles de sol (variantes aléatoires)
///   2. Définit les zones de spawn (gauche = T1, droite = T2)
///   3. Place des obstacles symétriques dans la zone de combat centrale
///      → Vérifie la connectivité BFS après chaque obstacle (garantit un chemin T1↔T2)
///   4. Applique IsWalkable sur chaque Cell via GridManager
///   5. Crée les GameObjects visuels pour chaque tile
///   6. Colorie les zones de spawn via le système de highlight existant
///
/// SETUP UNITY :
///   - Ce MonoBehaviour doit être dans la scène avec GridManager
///   - Assigner ArenaConfig et GridConfig dans l'Inspector
///   - Créer les assets ScriptableObject : ArenaConfig + TileSpriteRegistry
/// </summary>
[DefaultExecutionOrder(-5)]  // S'exécute avant GridManager (order 0) pour ajuster les dimensions
public class ArenaGenerator : MonoBehaviour
{
    // =========================================================
    // INSPECTOR
    // =========================================================

    [Header("=== CONFIGURATION ===")]
    [Tooltip("Asset ArenaConfig contenant tous les paramètres de génération.")]
    public ArenaConfig arenaConfig;

    [Tooltip("Asset GridConfig partagé avec GridManager. " +
             "ArenaGenerator y écrit les dimensions avant que GridManager n'initialise la grille.")]
    public GridConfig gridConfig;

    [Header("=== OPTIONS ===")]
    [Tooltip("Lancer la génération automatiquement au démarrage de la scène.")]
    public bool generateOnStart = true;

    [Tooltip("Afficher des gizmos de debug procéduraux (spawn, obstacles) dans la Scene view.")]
    public bool showDebugGizmos = true;

    // =========================================================
    // DONNÉES INTERNES
    // =========================================================

    private CellTileType[,] arenaData;
    private List<Cell>       spawnCellsTeam1 = new List<Cell>();
    private List<Cell>       spawnCellsTeam2 = new List<Cell>();
    private Transform        tileContainer;
    private System.Random    rng;
    private int              effectiveSeed;

    // Directions cardinales pour le BFS de connectivité
    private static readonly int[] DX = {  0,  0,  1, -1 };
    private static readonly int[] DY = {  1, -1,  0,  0 };

    // =========================================================
    // CYCLE DE VIE
    // =========================================================

    void Awake()
    {
        // Même fichier GridConfig que GridManager : dimensions écrites ici avant son InitializeGrid().
        SyncArenaDimensionsIntoGridConfig();
    }

    /// <summary>Copie ArenaConfig.width/height vers gridConfig pour que GridManager construise le bon tableau de Cell.</summary>
    void SyncArenaDimensionsIntoGridConfig()
    {
        if (arenaConfig == null || gridConfig == null) return;
        gridConfig.width  = arenaConfig.arenaWidth;
        gridConfig.height = arenaConfig.arenaHeight;
    }

    void Start()
    {
        if (generateOnStart)
            Generate();
    }

    // =========================================================
    // POINT D'ENTRÉE PUBLIC
    // =========================================================

    /// <summary>
    /// Génère (ou régénère) l'arène.
    /// Peut être appelé plusieurs fois en cours de partie pour créer une nouvelle carte.
    /// Pour repartir à zéro après changement de taille sans Play, préférez <see cref="RegenerateArena"/>.
    /// </summary>
    public void Generate()
    {
        if (!ValidateSetup()) return;

        effectiveSeed = arenaConfig.seed < 0
            ? Random.Range(0, int.MaxValue)
            : arenaConfig.seed;

        rng = new System.Random(effectiveSeed);

        Debug.Log($"[ArenaGenerator] Génération {arenaConfig.arenaWidth}x{arenaConfig.arenaHeight}" +
                  $" | Seed : {effectiveSeed}" +
                  $" | Densité obstacles : {arenaConfig.obstacleDensity:P0}");

        ClearPreviousArena();

        // Pipeline
        Step1_InitializeArenaData();
        Step2_SetupSpawnZones();
        Step3_GenerateObstacles();
        Step4_ApplyToGrid();
        Step5_RenderTiles();
        Step6_HighlightSpawnZones();

        Debug.Log($"[ArenaGenerator] Arène prête ! " +
                  $"Spawn T1 : {spawnCellsTeam1.Count} cases | " +
                  $"Spawn T2 : {spawnCellsTeam2.Count} cases");
    }

    /// <summary>
    /// Synchronise les dimensions depuis <see cref="ArenaConfig"/>, appelle <see cref="GridManager.RegenerateGrid"/>,
    /// puis régénère l'arène complète. À utiliser après un changement de taille dans l'Inspector ou via le menu contextuel du composant.
    /// </summary>
    [ContextMenu("Regenerate Arena")]
    public void RegenerateArena()
    {
        if (!ValidateSetup()) return;

        SyncArenaDimensionsIntoGridConfig();
#if UNITY_EDITOR
        if (gridConfig != null)
            EditorUtility.SetDirty(gridConfig);
#endif

        GridManager.Instance.RegenerateGrid();
        Generate();
    }

    // =========================================================
    // ÉTAPE 1 — Données de sol
    // =========================================================

    void Step1_InitializeArenaData()
    {
        int w = arenaConfig.arenaWidth;
        int h = arenaConfig.arenaHeight;
        arenaData = new CellTileType[w, h];

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                float roll = (float)rng.NextDouble();

                if (roll < arenaConfig.bloodTileChance)
                    arenaData[x, y] = CellTileType.GroundBlood;
                else if (roll < arenaConfig.bloodTileChance + arenaConfig.grassTileChance)
                    arenaData[x, y] = CellTileType.GroundGrass;
                else
                    arenaData[x, y] = CellTileType.Ground;
            }
        }
    }

    // =========================================================
    // ÉTAPE 2 — Zones de spawn
    // =========================================================

    void Step2_SetupSpawnZones()
    {
        int w     = arenaConfig.arenaWidth;
        int h     = arenaConfig.arenaHeight;
        int depth = arenaConfig.spawnZoneDepth;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < depth; x++)
                arenaData[x, y] = CellTileType.SpawnTeam1;

            for (int x = w - depth; x < w; x++)
                arenaData[x, y] = CellTileType.SpawnTeam2;
        }
    }

    // =========================================================
    // ÉTAPE 3 — Obstacles symétriques avec vérification de connectivité
    // =========================================================

    void Step3_GenerateObstacles()
    {
        int w         = arenaConfig.arenaWidth;
        int h         = arenaConfig.arenaHeight;
        int depth     = arenaConfig.spawnZoneDepth;
        int clearance = arenaConfig.minClearanceFromSpawn;
        int borderM   = Mathf.Clamp(arenaConfig.obstacleBorderMargin, 0, 4);

        // Zone de combat accessible aux obstacles
        int combatXMin = Mathf.Max(depth + clearance, borderM);
        int combatXMax = Mathf.Min(w - depth - clearance - 1, w - 1 - borderM);

        if (combatXMin > combatXMax)
        {
            Debug.LogWarning("[ArenaGenerator] Zone de combat trop étroite pour placer des obstacles " +
                             "(augmente arenaWidth ou réduis spawnZoneDepth / minClearanceFromSpawn).");
            return;
        }

        // On ne travaille que sur la moitié gauche, puis on miroire sur la droite
        int halfXMax = (combatXMin + combatXMax) / 2;

        // Construire et mélanger la liste des candidats
        int yMin = borderM;
        int yMax = h - 1 - borderM;
        if (yMin > yMax)
        {
            Debug.LogWarning("[ArenaGenerator] obstacleBorderMargin supprime toute place verticale pour les obstacles.");
            return;
        }

        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = combatXMin; x <= halfXMax; x++)
            for (int y = yMin; y <= yMax; y++)
                candidates.Add(new Vector2Int(x, y));

        ShuffleList(candidates);

        int totalCombatCells = (combatXMax - combatXMin + 1) * (yMax - yMin + 1);
        int targetCount      = Mathf.RoundToInt(totalCombatCells * arenaConfig.obstacleDensity);
        // Moitié des obstacles, car chacun est mirrored (sauf la colonne centrale)
        int targetHalf = Mathf.CeilToInt(targetCount / 2f);

        int placed   = 0;
        int attempts = 0;

        foreach (Vector2Int pos in candidates)
        {
            if (placed >= targetHalf) break;
            if (attempts >= arenaConfig.maxObstaclePlacementAttempts) break;
            attempts++;

            int mirrorX = (w - 1) - pos.x;
            bool isCenterColumn = (pos.x == mirrorX);

            // Sauvegarder pour rollback
            CellTileType savedLeft   = arenaData[pos.x, pos.y];
            CellTileType savedRight  = isCenterColumn ? CellTileType.Ground : arenaData[mirrorX, pos.y];

            // Placer
            arenaData[pos.x, pos.y] = CellTileType.Obstacle;
            if (arenaConfig.mirrorSymmetry && !isCenterColumn)
                arenaData[mirrorX, pos.y] = CellTileType.Obstacle;

            // Vérifier connectivité T1 → T2
            if (!CheckConnectivity())
            {
                // Rollback
                arenaData[pos.x, pos.y] = savedLeft;
                if (arenaConfig.mirrorSymmetry && !isCenterColumn)
                    arenaData[mirrorX, pos.y] = savedRight;
                continue;
            }

            placed++;
        }

        Debug.Log($"[ArenaGenerator] Obstacles placés : {placed * (arenaConfig.mirrorSymmetry ? 2 : 1)}" +
                  $" / cible {targetCount} | Tentatives : {attempts}");
    }

    // =========================================================
    // CONNECTIVITÉ — BFS sur arenaData (sans GridManager)
    // =========================================================

    bool CheckConnectivity()
    {
        int w = arenaConfig.arenaWidth;
        int h = arenaConfig.arenaHeight;

        // Point de départ : première case marchable de la colonne 0 (spawn T1)
        Vector2Int start = new Vector2Int(-1, -1);
        for (int y = 0; y < h; y++)
        {
            if (IsWalkableInData(0, y))
            {
                start = new Vector2Int(0, y);
                break;
            }
        }
        if (start.x < 0) return false;

        // BFS
        bool[,]             visited = new bool[w, h];
        Queue<Vector2Int>   queue   = new Queue<Vector2Int>();

        visited[start.x, start.y] = true;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int cur = queue.Dequeue();

            for (int d = 0; d < 4; d++)
            {
                int nx = cur.x + DX[d];
                int ny = cur.y + DY[d];

                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                if (visited[nx, ny]) continue;
                if (!IsWalkableInData(nx, ny)) continue;

                visited[nx, ny] = true;
                queue.Enqueue(new Vector2Int(nx, ny));
            }
        }

        // Vérifier qu'au moins une case marchable dans la colonne w-1 (spawn T2) est atteinte
        for (int y = 0; y < h; y++)
            if (IsWalkableInData(w - 1, y) && visited[w - 1, y])
                return true;

        return false;
    }

    bool IsWalkableInData(int x, int y) =>
        arenaData[x, y] != CellTileType.Obstacle;

    // =========================================================
    // ÉTAPE 4 — Application sur les Cell de GridManager
    // =========================================================

    void Step4_ApplyToGrid()
    {
        int w = arenaConfig.arenaWidth;
        int h = arenaConfig.arenaHeight;

        spawnCellsTeam1.Clear();
        spawnCellsTeam2.Clear();

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Cell cell = GridManager.Instance.GetCell(x, y);
                if (cell == null) continue;

                CellTileType type = arenaData[x, y];
                cell.TileType  = type;
                cell.IsWalkable = type != CellTileType.Obstacle;

                if (type == CellTileType.SpawnTeam1) spawnCellsTeam1.Add(cell);
                else if (type == CellTileType.SpawnTeam2) spawnCellsTeam2.Add(cell);
            }
        }
    }

    // =========================================================
    // ÉTAPE 5 — Rendu des sprites de tiles
    // =========================================================

    /// <remarks>
    /// Les tuiles utilisent plusieurs <see cref="SpriteRenderer"/> par case :
    /// le rendu n'est pas fait via Tilemap Unity — pas de <c>SetTiles</c> batch ici ;
    /// le coût est linéaire en w×h, acceptable pour des arènes &lt;~25×25.
    /// </remarks>
    void Step5_RenderTiles()
    {
        TileSpriteRegistry registry = arenaConfig.tileRegistry;
        if (registry == null)
        {
            Debug.LogWarning("[ArenaGenerator] TileSpriteRegistry non assigné dans ArenaConfig — " +
                             "aucun tile visuel ne sera créé.");
            return;
        }

        var gridMgr = GridManager.Instance;
        Transform gridRoot = gridMgr.GridVisualRoot != null
            ? gridMgr.GridVisualRoot
            : gridMgr.transform;

        tileContainer = new GameObject("=== ARENA TILES ===").transform;
        tileContainer.SetParent(gridRoot);
        tileContainer.localPosition = Vector3.zero;
        tileContainer.localRotation = Quaternion.identity;
        tileContainer.localScale    = Vector3.one;

        int w = arenaConfig.arenaWidth;
        int h = arenaConfig.arenaHeight;
        Vector3 spriteOffset =
            gridConfig != null ? gridConfig.arenaTileSpriteWorldOffset : Vector3.zero;
        int orderBias =
            gridConfig != null ? gridConfig.arenaTileSortingOrderBias : 0;

        System.Random renderRng = new System.Random(effectiveSeed + 7919);

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Cell cell = gridMgr.GetCell(x, y);
                if (cell == null) continue;

                CellTileType type   = arenaData[x, y];
                Sprite       sprite = PickSprite(type, registry, renderRng);
                if (sprite == null) continue;

                GameObject tileGO = new GameObject($"Tile_{x}_{y}");
                tileGO.transform.SetParent(tileContainer, worldPositionStays: false);
                tileGO.transform.position = cell.WorldPosition + spriteOffset;

                SpriteRenderer sr = tileGO.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                ApplyArenaTileSorting(sr);

                // Tri pseudo-isométrique : plus la case est « bas / droite », plus elle est dessinée devant.
                bool isObstacle = type == CellTileType.Obstacle;
                int  baseOrder  = -(y * w + x);
                sr.sortingOrder = orderBias + (isObstacle ? baseOrder + w * h : baseOrder - w * h);

                if (!isObstacle)
                {
                    Sprite deco = registry.MaybeGetDecorationOverlay(renderRng);
                    if (deco != null)
                    {
                        var decoGo = new GameObject("Decoration");
                        decoGo.transform.SetParent(tileGO.transform, false);
                        decoGo.transform.localPosition = Vector3.zero;
                        var decoSr = decoGo.AddComponent<SpriteRenderer>();
                        decoSr.sprite = deco;
                        ApplyArenaTileSorting(decoSr);
                        decoSr.sortingOrder = sr.sortingOrder + 1;
                    }
                }

                if (arenaConfig.renderPerimeterEdges)
                {
                    int peri = TileSpriteRegistry.GetPerimeterStep(x, y, w, h);
                    if (peri >= 0)
                    {
                        Sprite edge = registry.GetEdgeOverlaySprite(peri);
                        if (edge != null)
                        {
                            var edgeGo = new GameObject("Edge");
                            edgeGo.transform.SetParent(tileGO.transform, false);
                            edgeGo.transform.localPosition = Vector3.zero;
                            var edgeSr = edgeGo.AddComponent<SpriteRenderer>();
                            edgeSr.sprite = edge;
                            ApplyArenaTileSorting(edgeSr);
                            edgeSr.sortingOrder = sr.sortingOrder + registry.edgeSortingOrderBoost;
                        }
                    }
                }
            }
        }
    }

    /// <summary>Sorting layer commun pour les sprites d'arène (optionnel, défini dans GridConfig).</summary>
    void ApplyArenaTileSorting(SpriteRenderer sr)
    {
        if (gridConfig == null || string.IsNullOrEmpty(gridConfig.arenaTileSortingLayerName)) return;
        sr.sortingLayerName = gridConfig.arenaTileSortingLayerName;
    }

    Sprite PickSprite(CellTileType type, TileSpriteRegistry registry, System.Random renderRng)
    {
        switch (type)
        {
            case CellTileType.Obstacle:
                return registry.GetRandomObstacleTile(renderRng);

            case CellTileType.GroundBlood:
            case CellTileType.GroundGrass:
                return registry.GetGroundSpriteForType(type, renderRng);

            default:
                // Ground, SpawnTeam1, SpawnTeam2 → sol aléatoire (tableau GROUND*)
                return registry.GetGroundSpriteForType(CellTileType.Ground, renderRng);
        }
    }

    // =========================================================
    // ÉTAPE 6 — Highlight des zones de spawn
    // =========================================================

    void Step6_HighlightSpawnZones()
    {
        // Réutilise le système de highlight existant de GridManager
        // Move (bleu) = T1 | Attack (rouge) = T2
        // Ces highlights seront effacés par CombatManager au début du combat
        GridManager.Instance.HighlightCells(spawnCellsTeam1, HighlightType.Move);
        GridManager.Instance.HighlightCells(spawnCellsTeam2, HighlightType.Attack);
    }

    // =========================================================
    // NETTOYAGE
    // =========================================================

    void ClearPreviousArena()
    {
        if (tileContainer != null)
            Destroy(tileContainer.gameObject);

        if (GridManager.Instance != null)
            GridManager.Instance.ClearAllHighlights();

        spawnCellsTeam1.Clear();
        spawnCellsTeam2.Clear();
        arenaData = null;
    }

    // =========================================================
    // ACCÈS PUBLIC
    // =========================================================

    /// <summary>Retourne les cases de spawn de l'équipe donnée (1 ou 2).</summary>
    public List<Cell> GetSpawnCells(int team)
    {
        return team == 1 ? spawnCellsTeam1
             : team == 2 ? spawnCellsTeam2
             : new List<Cell>();
    }

    /// <summary>Retourne la seed effectivement utilisée pour la dernière génération.</summary>
    public int GetEffectiveSeed() => effectiveSeed;

    /// <summary>Retourne true si la case (x,y) est dans la zone de spawn de l'équipe donnée.</summary>
    public bool IsSpawnCell(int x, int y, int team)
    {
        if (arenaData == null) return false;
        if (x < 0 || x >= arenaConfig.arenaWidth || y < 0 || y >= arenaConfig.arenaHeight) return false;
        CellTileType type = arenaData[x, y];
        return team == 1 ? type == CellTileType.SpawnTeam1
             : team == 2 ? type == CellTileType.SpawnTeam2
             : false;
    }

    // =========================================================
    // UTILITAIRES INTERNES
    // =========================================================

    bool ValidateSetup()
    {
        if (arenaConfig == null)
        {
            Debug.LogError("[ArenaGenerator] ArenaConfig non assigné dans l'Inspector !");
            return false;
        }
        if (gridConfig == null)
        {
            Debug.LogError("[ArenaGenerator] GridConfig non assigné dans l'Inspector !");
            return false;
        }
        if (GridManager.Instance == null)
        {
            Debug.LogError("[ArenaGenerator] GridManager.Instance est null ! " +
                           "Assure-toi que GridManager est présent dans la scène.");
            return false;
        }

        if (!arenaConfig.IsValid(out string err))
        {
            Debug.LogError($"[ArenaGenerator] ArenaConfig invalide : {err}");
            return false;
        }

        return true;
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j    = rng.Next(i + 1);
            T   tmp  = list[i];
            list[i]  = list[j];
            list[j]  = tmp;
        }
    }

    // =========================================================
    // GIZMOS EDITOR
    // =========================================================

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || arenaData == null || GridManager.Instance == null) return;

        int w = arenaConfig.arenaWidth;
        int h = arenaConfig.arenaHeight;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Vector3 pos = GridManager.Instance.GridToWorld(x, y);

                switch (arenaData[x, y])
                {
                    case CellTileType.Obstacle:
                        Gizmos.color = new Color(0.9f, 0.15f, 0.15f, 0.55f);
                        Gizmos.DrawCube(pos, new Vector3(0.35f, 0.20f, 0.01f));
                        break;

                    case CellTileType.SpawnTeam1:
                        Gizmos.color = new Color(0.25f, 0.45f, 1f, 0.30f);
                        Gizmos.DrawCube(pos, new Vector3(0.30f, 0.12f, 0.01f));
                        break;

                    case CellTileType.SpawnTeam2:
                        Gizmos.color = new Color(1f, 0.30f, 0.20f, 0.30f);
                        Gizmos.DrawCube(pos, new Vector3(0.30f, 0.12f, 0.01f));
                        break;
                }
            }
        }

        // Étiquette seed dans la Scene view
        UnityEditor.Handles.Label(
            GridManager.Instance.GridToWorld(0, h + 1),
            $"Arena {w}x{h} | Seed {effectiveSeed}"
        );
    }
#endif
}
