using UnityEngine;

/// <summary>
/// Caméra isométrique 2.5D pour pixel art
/// Angle 30°, zoom molette, déplacement ZQSD / clic milieu
/// </summary>
public class IsometricCamera : MonoBehaviour
{
    // =========================================================
    // CONFIGURATION INSPECTOR
    // =========================================================

    [Header("=== ANGLE ISOMÉTRIQUE ===")]
    [Tooltip("Angle vertical de la caméra (30° = isométrique classique)")]
    [Range(20f, 90f)]
    public float isometricAngle = 30f;

    [Header("=== ZOOM ===")]
    [Tooltip("Taille orthographique de départ")]
    public float defaultZoom = 5f;

    [Tooltip("Zoom minimum (plus petit = plus zoomé)")]
    public float minZoom = 2f;

    [Tooltip("Zoom maximum (plus grand = plus dézoomé)")]
    public float maxZoom = 10f;

    [Tooltip("Vitesse du zoom molette")]
    public float zoomSpeed = 1f;

    [Tooltip("Fluidité du zoom (plus grand = plus lent et smooth)")]
    [Range(1f, 20f)]
    public float zoomSmoothness = 8f;

    [Header("=== DÉPLACEMENT ===")]
    [Tooltip("Vitesse de déplacement clavier ZQSD")]
    public float moveSpeed = 5f;

    [Tooltip("Vitesse de déplacement clic milieu (pan)")]
    public float panSpeed = 1f;

    [Tooltip("Fluidité du déplacement")]
    [Range(1f, 20f)]
    public float moveSmoothness = 10f;

    [Header("=== LIMITES DE DÉPLACEMENT ===")]
    [Tooltip("Activer les limites de déplacement")]
    public bool useBounds = true;

    [Tooltip("Limite gauche")]
    public float boundLeft = -20f;

    [Tooltip("Limite droite")]
    public float boundRight = 20f;

    [Tooltip("Limite bas")]
    public float boundBottom = -20f;

    [Tooltip("Limite haut")]
    public float boundTop = 20f;

    [Header("=== SUIVI D'UNE CIBLE (optionnel) ===")]
    [Tooltip("Laisser vide si pas de suivi automatique")]
    public Transform target;

    [Tooltip("Vitesse de suivi de la cible")]
    [Range(1f, 20f)]
    public float followSpeed = 5f;

    [Tooltip("Décalage de la caméra par rapport à la cible")]
    public Vector2 followOffset = Vector2.zero;

    [Header("=== PIXEL ART ===")]
    [Tooltip("Arrondir la position pour éviter le pixel crawling")]
    public bool pixelPerfect = true;

    [Tooltip("Pixels Per Unit de tes sprites (doit correspondre aux sprites)")]
    public float pixelsPerUnit = 32f;

    // =========================================================
    // VARIABLES PRIVÉES (internes au script)
    // =========================================================

    private Camera cam;                    // Référence à la caméra
    private float targetZoom;             // Zoom cible (pour smooth)
    private Vector3 targetPosition;       // Position cible (pour smooth)
    private Vector3 panStartPosition;     // Position souris au début du pan
    private bool isPanning = false;       // Est-ce qu'on pan actuellement ?

    // =========================================================
    // INITIALISATION
    // =========================================================

    void Awake()
    {
        // Récupérer le composant Camera sur ce GameObject
        cam = GetComponent<Camera>();

        // Vérifications de sécurité
        if (cam == null)
        {
            Debug.LogError("❌ IsometricCamera : Aucun composant Camera trouvé ! " +
                          "Attache ce script à ta Main Camera.");
            enabled = false; // Désactiver le script pour éviter les erreurs
            return;
        }

        if (!cam.orthographic)
        {
            Debug.LogWarning("⚠️ IsometricCamera : La caméra n'est pas en mode Orthographic ! " +
                            "Passage automatique en Orthographic.");
            cam.orthographic = true;
        }

        // Initialiser les valeurs
        targetZoom = defaultZoom;
        cam.orthographicSize = defaultZoom;
        targetPosition = transform.position;

        // Appliquer l'angle isométrique au démarrage
        ApplyIsometricAngle();

        Debug.Log("✅ IsometricCamera initialisée avec succès !");
    }

    // =========================================================
    // CHAQUE FRAME
    // =========================================================

    void Update()
    {
        // Ordre important : les inputs d'abord, puis appliquer
        HandleZoomInput();
        HandleKeyboardMovement();
        HandleMiddleClickPan();
        HandleFollowTarget();

        // Appliquer le zoom en douceur
        ApplySmoothedZoom();

        // Appliquer le mouvement en douceur
        ApplySmoothedMovement();
    }

    // =========================================================
    // MÉTHODES PRIVÉES — ZOOM
    // =========================================================

    /// <summary>
    /// Lit la molette de la souris et ajuste le zoom cible
    /// </summary>
    void HandleZoomInput()
    {
        // Input.mouseScrollDelta.y = valeur entre -1 et 1 selon la molette
        float scrollDelta = Input.mouseScrollDelta.y;

        if (scrollDelta != 0f)
        {
            // Molette vers le haut = scroll positif = on zoome (size diminue)
            // Molette vers le bas  = scroll négatif = on dézoome (size augmente)
            targetZoom -= scrollDelta * zoomSpeed;

            // Clamp = forcer la valeur entre min et max
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
    }

    /// <summary>
    /// Applique le zoom progressivement (effet smooth)
    /// </summary>
    void ApplySmoothedZoom()
    {
        // Lerp = interpolation linéaire : va de A vers B progressivement
        // Time.deltaTime = temps écoulé depuis la dernière frame
        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize,   // valeur actuelle
            targetZoom,             // valeur cible
            Time.deltaTime * zoomSmoothness  // vitesse
        );
    }

    // =========================================================
    // MÉTHODES PRIVÉES — DÉPLACEMENT CLAVIER
    // =========================================================

    /// <summary>
    /// Déplacement avec ZQSD (ou WASD selon clavier)
    /// </summary>
    void HandleKeyboardMovement()
    {
        // Si on suit une cible, le clavier ne bouge pas la caméra
        if (target != null) return;

        // Lire les axes (Unity gère ZQSD et WASD automatiquement)
        // GetAxisRaw = valeur -1, 0 ou 1 (sans lissage)
        float horizontal = Input.GetAxisRaw("Horizontal"); // Q/D ou A/D
        float vertical = Input.GetAxisRaw("Vertical");     // Z/S ou W/S

        // Calculer le mouvement
        // On multiplie par moveSpeed et Time.deltaTime pour être frame-rate indépendant
        Vector3 movement = new Vector3(horizontal, vertical, 0f)
                           * moveSpeed
                           * Time.deltaTime;

        // Ajouter à la position cible
        targetPosition += movement;

        // Appliquer les limites
        ApplyBounds();
    }

    // =========================================================
    // MÉTHODES PRIVÉES — PAN CLIC MILIEU
    // =========================================================

    /// <summary>
    /// Déplacement en maintenant le clic milieu de la souris
    /// </summary>
    void HandleMiddleClickPan()
    {
        // Si on suit une cible, le pan est désactivé
        if (target != null) return;

        // Bouton 2 = clic milieu (0=gauche, 1=droit, 2=milieu)
        if (Input.GetMouseButtonDown(2))
        {
            // Début du pan : enregistrer la position de départ
            isPanning = true;
            panStartPosition = GetMouseWorldPosition();
        }

        if (Input.GetMouseButtonUp(2))
        {
            isPanning = false;
        }

        if (isPanning && Input.GetMouseButton(2))
        {
            // Calculer le delta (différence) entre position actuelle et départ
            Vector3 currentMousePosition = GetMouseWorldPosition();
            Vector3 delta = panStartPosition - currentMousePosition;

            // Déplacer la caméra de ce delta
            targetPosition += delta * panSpeed;

            // Appliquer les limites
            ApplyBounds();
        }
    }

    /// <summary>
    /// Convertit la position de la souris en coordonnées monde
    /// </summary>
    Vector3 GetMouseWorldPosition()
    {
        // Prendre la position de la souris en pixels
        Vector3 mousePos = Input.mousePosition;
        // Ajouter la profondeur de la caméra
        mousePos.z = -cam.transform.position.z;
        // Convertir pixels → coordonnées monde
        return cam.ScreenToWorldPoint(mousePos);
    }

    // =========================================================
    // MÉTHODES PRIVÉES — SUIVI DE CIBLE
    // =========================================================

    /// <summary>
    /// Suit une cible (ex: le joueur) si elle est définie
    /// </summary>
    void HandleFollowTarget()
    {
        if (target == null) return;

        // Position désirée = position cible + offset
        Vector3 desiredPosition = new Vector3(
            target.position.x + followOffset.x,
            target.position.y + followOffset.y,
            transform.position.z  // Garder la profondeur Z de la caméra
        );

        targetPosition = desiredPosition;

        // Appliquer les limites même en mode suivi
        ApplyBounds();
    }

    // =========================================================
    // MÉTHODES PRIVÉES — APPLICATION DU MOUVEMENT
    // =========================================================

    /// <summary>
    /// Applique le mouvement progressivement
    /// </summary>
    void ApplySmoothedMovement()
    {
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            new Vector3(targetPosition.x, targetPosition.y, transform.position.z),
            Time.deltaTime * moveSmoothness
        );

        // Pixel Perfect : arrondir pour éviter le flou inter-pixels
        if (pixelPerfect)
        {
            smoothedPosition = RoundToPixel(smoothedPosition);
        }

        transform.position = smoothedPosition;
    }

    /// <summary>
    /// Force les limites de déplacement
    /// </summary>
    void ApplyBounds()
    {
        if (!useBounds) return;

        targetPosition.x = Mathf.Clamp(targetPosition.x, boundLeft, boundRight);
        targetPosition.y = Mathf.Clamp(targetPosition.y, boundBottom, boundTop);
    }

    /// <summary>
    /// Arrondit la position au pixel le plus proche (anti pixel crawling)
    /// </summary>
    Vector3 RoundToPixel(Vector3 position)
    {
        float pixelSize = 1f / pixelsPerUnit;
        return new Vector3(
            Mathf.Round(position.x / pixelSize) * pixelSize,
            Mathf.Round(position.y / pixelSize) * pixelSize,
            position.z
        );
    }

    /// <summary>
    /// Applique l'angle isométrique à la rotation de la caméra
    /// </summary>
    void ApplyIsometricAngle()
    {
        // En 2D avec caméra orthographique, on simule l'isométrique
        // par la rotation X de la caméra
        transform.rotation = Quaternion.Euler(isometricAngle, 0f, 0f);
    }

    // =========================================================
    // MÉTHODES PUBLIQUES (utilisables par d'autres scripts)
    // =========================================================

    /// <summary>
    /// Téléporter la caméra à une position instantanément
    /// </summary>
    public void TeleportTo(Vector2 position)
    {
        targetPosition = new Vector3(position.x, position.y, transform.position.z);
        transform.position = targetPosition;
    }

    /// <summary>
    /// Centrer la caméra sur une position (smooth)
    /// </summary>
    public void MoveTo(Vector2 position)
    {
        targetPosition = new Vector3(position.x, position.y, transform.position.z);
        ApplyBounds();
    }

    /// <summary>
    /// Changer le zoom depuis un autre script
    /// </summary>
    public void SetZoom(float zoomValue)
    {
        targetZoom = Mathf.Clamp(zoomValue, minZoom, maxZoom);
    }

    /// <summary>
    /// Réinitialiser la caméra à sa position et zoom par défaut
    /// </summary>
    public void ResetCamera()
    {
        targetZoom = defaultZoom;
        targetPosition = Vector3.zero;
        targetPosition.z = transform.position.z;
    }

    // =========================================================
    // GIZMOS — Visualisation dans l'éditeur Unity
    // =========================================================

#if UNITY_EDITOR
    /// <summary>
    /// Dessine les limites de déplacement dans la Scene view
    /// </summary>
    void OnDrawGizmos()
    {
        if (!useBounds) return;

        Gizmos.color = Color.cyan;

        // Dessiner un rectangle représentant les limites
        Vector3 topLeft = new Vector3(boundLeft, boundTop, 0f);
        Vector3 topRight = new Vector3(boundRight, boundTop, 0f);
        Vector3 bottomLeft = new Vector3(boundLeft, boundBottom, 0f);
        Vector3 bottomRight = new Vector3(boundRight, boundBottom, 0f);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);

        // Croix au centre
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0));
        Gizmos.DrawLine(new Vector3(0, -0.5f, 0), new Vector3(0, 0.5f, 0));
    }
#endif
}
