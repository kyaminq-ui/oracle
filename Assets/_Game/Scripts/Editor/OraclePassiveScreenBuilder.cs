#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;

/// <summary>
/// Construit l'intégralité du PassiveSelectionScreen en une seule passe.
/// Menu : Oracle > Build Passive Selection Screen
/// </summary>
public static class OraclePassiveScreenBuilder
{
    // =========================================================
    // CONSTANTES DE LAYOUT
    // =========================================================
    private const int   CARD_COUNT       = 5;
    private const float CARD_WIDTH       = 160f;
    private const float CARD_HEIGHT      = 210f;
    private const float CARD_SPACING     = 16f;
    private const float ICON_SIZE        = 64f;
    private const string PASSIVE_POOL_PATH = "Assets/_Game/ScriptableObjects/Spells/Passifs";
    private const string POOL_ASSET_PATH   = "Assets/_Game/ScriptableObjects/AllPassivesPool.asset";

    // =========================================================
    // ENTRÉE
    // =========================================================
    [MenuItem("Oracle/Build Passive Selection Screen")]
    public static void Build()
    {
        Canvas canvas = EnsureCanvas();

        // Détruire l'ancien PassiveSelectionScreen si incomplet
        var existing = FindChild(canvas.transform, "PassiveSelectionScreen");
        if (existing != null)
        {
            bool rebuild = EditorUtility.DisplayDialog(
                "Oracle — Passive Screen",
                "Un PassiveSelectionScreen existe déjà.\nVeut-on le reconstruire entièrement ?",
                "Oui, reconstruire", "Annuler");
            if (!rebuild) return;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        // ── Racine — stretch plein écran (anchorMin/Max 0–1, pas deux fois 0,0) ──
        GameObject root = MakePanel(canvas.transform, "PassiveSelectionScreen",
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        StretchFull(root.GetComponent<RectTransform>());
        root.SetActive(false);
        var pss = root.AddComponent<PassiveSelectionScreen>();
        Undo.RegisterCreatedObjectUndo(root, "Build PassiveSelectionScreen");

        // ── Background ──────────────────────────────────────
        var bg = MakePanel(root.transform, "Background",
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
        StretchFull(bg.GetComponent<RectTransform>());

        // ── Titre ────────────────────────────────────────────
        var title = MakeTMP(root.transform, "Title",
            new Vector2(0f, -70f), new Vector2(700f, 55f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        title.text      = "Choisis ton passif";
        title.fontSize  = 28f;
        title.fontStyle = FontStyles.Bold;
        title.color     = new Color(0.79f, 0.66f, 0.30f);
        title.alignment = TextAlignmentOptions.Center;

        // ── Cards Container ──────────────────────────────────
        float totalW = CARD_COUNT * CARD_WIDTH + (CARD_COUNT - 1) * CARD_SPACING;
        // Centré à l'écran (l'ancien -totalW/2 en X décalait tout hors vue sur certaines résolutions)
        var container = MakePanel(root.transform, "CardsContainer",
            new Vector2(0f, -20f),
            new Vector2(totalW, CARD_HEIGHT),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        container.AddComponent<Image>().color = Color.clear;
        var hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing             = CARD_SPACING;
        hlg.childAlignment      = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth   = false;
        hlg.childControlHeight  = false;

        // ── 5 cartes ─────────────────────────────────────────
        var cardList = new System.Collections.Generic.List<PassiveCardUI>();
        for (int i = 0; i < CARD_COUNT; i++)
        {
            var card = BuildCard(container.transform, i);
            cardList.Add(card);
        }
        pss.cards = cardList;

        // ── Timer Container ───────────────────────────────────
        var timerRoot = MakePanel(root.transform, "TimerContainer",
            new Vector2(-60f, -60f), new Vector2(80f, 80f),
            new Vector2(1f, 1f), new Vector2(1f, 1f));
        timerRoot.AddComponent<Image>().color = Color.clear;

        var timerFillGO = MakePanel(timerRoot.transform, "TimerFill",
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        StretchFull(timerFillGO.GetComponent<RectTransform>());
        var timerFillImg        = timerFillGO.AddComponent<Image>();
        timerFillImg.type       = Image.Type.Filled;
        timerFillImg.fillMethod = Image.FillMethod.Radial360;
        timerFillImg.fillOrigin = (int)Image.Origin360.Top;
        timerFillImg.fillClockwise = true;
        timerFillImg.fillAmount = 1f;
        timerFillImg.color      = new Color(0.20f, 0.80f, 0.20f);
        pss.timerFill = timerFillImg;

        var timerTextGO = MakePanel(timerRoot.transform, "TimerText",
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        StretchFull(timerTextGO.GetComponent<RectTransform>());
        var timerTMP        = timerTextGO.AddComponent<TextMeshProUGUI>();
        timerTMP.text       = "30";
        timerTMP.fontSize   = 22f;
        timerTMP.fontStyle  = FontStyles.Bold;
        timerTMP.color      = Color.white;
        timerTMP.alignment  = TextAlignmentOptions.Center;
        pss.timerText = timerTMP;

        // ── Confirm Button ────────────────────────────────────
        var btnGO = MakePanel(root.transform, "ConfirmButton",
            new Vector2(-100f, 70f), new Vector2(200f, 50f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.79f, 0.66f, 0.30f);
        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var btnBlock  = btn.colors;
        btnBlock.normalColor      = new Color(0.79f, 0.66f, 0.30f);
        btnBlock.highlightedColor = new Color(0.95f, 0.80f, 0.40f);
        btnBlock.pressedColor     = new Color(0.60f, 0.48f, 0.18f);
        btnBlock.disabledColor    = new Color(0.35f, 0.35f, 0.35f);
        btn.colors = btnBlock;
        btn.interactable = false;

        var btnLabelGO = MakePanel(btnGO.transform, "Label",
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        StretchFull(btnLabelGO.GetComponent<RectTransform>());
        var btnTMP       = btnLabelGO.AddComponent<TextMeshProUGUI>();
        btnTMP.text      = "Confirmer";
        btnTMP.fontSize  = 18f;
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.color     = new Color(0.10f, 0.07f, 0.02f);
        btnTMP.alignment = TextAlignmentOptions.Center;
        pss.confirmButton = btn;

        // ── Recap Panel ───────────────────────────────────────
        var recap = MakePanel(root.transform, "RecapPanel",
            new Vector2(-200f, -60f), new Vector2(400f, 120f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var recapImg = recap.AddComponent<Image>();
        recapImg.color = new Color(0.10f, 0.10f, 0.18f, 0.93f);
        recap.SetActive(false);
        pss.recapPanel = recap;

        var recapTextGO = MakePanel(recap.transform, "RecapText",
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        StretchFull(recapTextGO.GetComponent<RectTransform>());
        var recapTMP       = recapTextGO.AddComponent<TextMeshProUGUI>();
        recapTMP.text      = "Passif choisi : —";
        recapTMP.fontSize  = 18f;
        recapTMP.color     = Color.white;
        recapTMP.alignment = TextAlignmentOptions.Center;
        pss.recapText = recapTMP;

        // ── PassivePool SO ────────────────────────────────────
        TryAssignPassivePool(pss);

        // ── Marquer dirty et sauvegarder ─────────────────────
        EditorUtility.SetDirty(root);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = root;
        Debug.Log("[OraclePassiveScreenBuilder] PassiveSelectionScreen construit avec succès !");

        EditorUtility.DisplayDialog(
            "Oracle — Passive Screen",
            "PassiveSelectionScreen construit !\n\n" +
            (pss.passivePool != null
                ? $"PassivePool assigné : {pss.passivePool.allPassives.Count} passifs trouvés."
                : "PassivePool non trouvé — crée Assets/_Game/ScriptableObjects/AllPassivesPool.asset\n" +
                  "et glisse les 10 passifs dedans."),
            "OK");
    }

    // =========================================================
    // CONSTRUCTION D'UNE CARTE
    // =========================================================
    static PassiveCardUI BuildCard(Transform parent, int index)
    {
        var cardGO = MakePanel(parent, $"Card_{index}",
            Vector2.zero, new Vector2(CARD_WIDTH, CARD_HEIGHT),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var cardImg = cardGO.AddComponent<Image>();
        cardImg.color = new Color(0.12f, 0.12f, 0.18f);

        var btn = cardGO.AddComponent<Button>();
        btn.targetGraphic = cardImg;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.20f, 0.20f, 0.30f);
        btn.colors = colors;

        var pcu = cardGO.AddComponent<PassiveCardUI>();
        pcu.cardBackground = cardImg;

        // Layout interne
        var vlg = cardGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding          = new RectOffset(8, 8, 12, 12);
        vlg.spacing          = 6f;
        vlg.childAlignment   = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = false;

        // Icône
        var iconGO  = new GameObject("Icon");
        iconGO.transform.SetParent(cardGO.transform, false);
        var iconRT  = iconGO.AddComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(ICON_SIZE, ICON_SIZE);
        iconGO.AddComponent<LayoutElement>().preferredHeight = ICON_SIZE;
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.color   = Color.white;
        iconImg.enabled = false;
        pcu.iconImage   = iconImg;

        // Nom
        var nameGO  = new GameObject("Name");
        nameGO.transform.SetParent(cardGO.transform, false);
        nameGO.AddComponent<RectTransform>().sizeDelta = new Vector2(CARD_WIDTH - 16f, 28f);
        nameGO.AddComponent<LayoutElement>().preferredHeight = 28f;
        var nameTMP     = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text    = $"Passif {index + 1}";
        nameTMP.fontSize = 13f;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color   = new Color(0.79f, 0.66f, 0.30f);
        nameTMP.alignment = TextAlignmentOptions.Center;
        pcu.nameText    = nameTMP;

        // Description
        var descGO  = new GameObject("Description");
        descGO.transform.SetParent(cardGO.transform, false);
        descGO.AddComponent<RectTransform>().sizeDelta = new Vector2(CARD_WIDTH - 16f, 80f);
        var descLE  = descGO.AddComponent<LayoutElement>();
        descLE.preferredHeight = 80f;
        descLE.flexibleHeight  = 1f;
        var descTMP      = descGO.AddComponent<TextMeshProUGUI>();
        descTMP.text     = "—";
        descTMP.fontSize = 10f;
        descTMP.color    = new Color(0.80f, 0.80f, 0.80f);
        descTMP.alignment = TextAlignmentOptions.Center;
        descTMP.enableWordWrapping = true;
        pcu.descriptionText = descTMP;

        return pcu;
    }

    // =========================================================
    // PASSIVE POOL AUTO-ASSIGN
    // =========================================================
    static void TryAssignPassivePool(PassiveSelectionScreen pss)
    {
        // Chercher un pool déjà existant
        var pool = AssetDatabase.LoadAssetAtPath<PassivePool>(POOL_ASSET_PATH);

        if (pool == null)
        {
            // Essayer d'en créer un et d'y mettre tous les passifs trouvés
            if (!AssetDatabase.IsValidFolder("Assets/_Game/ScriptableObjects"))
                return;

            pool = ScriptableObject.CreateInstance<PassivePool>();
            AssetDatabase.CreateAsset(pool, POOL_ASSET_PATH);
        }

        // Charger tous les PassiveData du dossier Passifs
        if (pool.allPassives.Count == 0 &&
            AssetDatabase.IsValidFolder(PASSIVE_POOL_PATH))
        {
            string[] guids = AssetDatabase.FindAssets("t:PassiveData", new[] { PASSIVE_POOL_PATH });
            foreach (var guid in guids)
            {
                var path    = AssetDatabase.GUIDToAssetPath(guid);
                var passive = AssetDatabase.LoadAssetAtPath<PassiveData>(path);
                if (passive != null && !pool.allPassives.Contains(passive))
                    pool.allPassives.Add(passive);
            }
            EditorUtility.SetDirty(pool);
        }

        pss.passivePool = pool;
        Debug.Log($"[OraclePassiveScreenBuilder] PassivePool : {pool.allPassives.Count} passifs assignés.");
    }

    // =========================================================
    // UTILITAIRES UI
    // =========================================================
    static Canvas EnsureCanvas()
    {
        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas != null) return canvas;

        var go = new GameObject("Canvas");
        Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static GameObject MakePanel(Transform parent, string name,
        Vector2 anchoredPos, Vector2 sizeDelta,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt             = go.AddComponent<RectTransform>();
        rt.anchorMin       = anchorMin;
        rt.anchorMax       = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta       = sizeDelta;
        return go;
    }

    static TextMeshProUGUI MakeTMP(Transform parent, string name,
        Vector2 anchoredPos, Vector2 sizeDelta,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go  = MakePanel(parent, name, anchoredPos, sizeDelta, anchorMin, anchorMax);
        return go.AddComponent<TextMeshProUGUI>();
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
    }

    static Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
            if (child.name == name) return child;
        return null;
    }
}
#endif
