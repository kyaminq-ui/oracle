using UnityEngine;

/// <summary>
/// Configuration de la grille - modifiable dans l'Inspector sans toucher au code
/// Créer via : Clic droit → Create → Grid → Grid Configuration
/// </summary>
[CreateAssetMenu(fileName = "GridConfig", menuName = "Grid/Grid Configuration")]
public class GridConfig : ScriptableObject
{
    [Header("=== DIMENSIONS DE LA GRILLE ===")]
    [Tooltip("Nombre de colonnes (axe X)")]
    [Range(2, 30)]
    public int width = 10;

    [Tooltip("Nombre de lignes (axe Y)")]
    [Range(2, 30)]
    public int height = 10;

    [Header("=== TAILLE DES TUILES ===")]
    [Tooltip("Largeur d'une tuile en unités Unity")]
    public float tileWidth = 1f;

    [Tooltip("Hauteur d'une tuile en unités Unity (généralement tileWidth/2)")]
    public float tileHeight = 0.5f;

    [Header("=== ORIGINE DE LA GRILLE ===")]
    [Tooltip("Position monde du point (0,0) de la grille")]
    public Vector3 gridOrigin = Vector3.zero;

    [Header("=== VISUELS ===")]
    [Tooltip("Sprite utilisé pour afficher une cellule")]
    public Sprite cellSprite;

    [Tooltip("Couleur de base des cellules")]
    public Color defaultCellColor = new Color(1f, 1f, 1f, 0.1f);

    [Header("=== COULEURS DE HIGHLIGHT ===")]
    [Tooltip("Déplacement possible")]
    public Color moveColor = new Color(0.2f, 0.5f, 1f, 0.6f);      // Bleu

    [Tooltip("Zone d'attaque")]
    public Color attackColor = new Color(1f, 0.2f, 0.2f, 0.6f);    // Rouge

    [Tooltip("Zone AoE (Area of Effect)")]
    public Color aoeColor = new Color(1f, 0.6f, 0.1f, 0.6f);       // Orange

    [Tooltip("Cellule sélectionnée")]
    public Color selectedColor = new Color(1f, 1f, 0.2f, 0.8f);    // Jaune

    [Tooltip("Cellule survolée (hover)")]
    public Color hoverColor = new Color(0.8f, 0.8f, 0.8f, 0.4f);   // Gris clair

    [Header("=== COMPORTEMENT ===")]
    [Tooltip("Afficher la grille au démarrage")]
    public bool showGridOnStart = true;

    [Tooltip("Activer le debug (logs dans la console)")]
    public bool debugMode = false;
}
