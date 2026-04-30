#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Roadmap 4.2.1 — Layout écran de combat (un clic).
/// Menu : Oracle > Build Combat HUD (4.2.1)
/// Réorganise DeckUI + TimerUI existants dans la structure officielle.
/// </summary>
public static class OracleCombatHUDBuilder
{
    private static readonly Color Accent = new Color(0.788f, 0.659f, 0.298f, 1f);
    private static readonly Color DarkBg  = new Color(0.08f, 0.08f, 0.12f, 0.92f);
    private static readonly Color BarBg   = new Color(0.18f, 0.18f, 0.22f, 1f);
    private static readonly Color HpOk    = new Color(0.25f, 0.75f, 0.35f, 1f);

    const string MENU = "Oracle/Build Combat HUD (4.2.1)";

    [MenuItem(MENU)]
    public static void Build()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Oracle — HUD", "Aucun Canvas dans la scène. Lance d'abord Setup UI Combat.", "OK");
            return;
        }

        var old = canvas.transform.Find("CombatHUD");
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("OracleCombatHUD",
                    "CombatHUD existe déjà. Reconstruire ? (DeckUI/TimerUI seront re-parentés)",
                    "Oui", "Non"))
                return;
            Undo.DestroyObjectImmediate(old.gameObject);
        }

        var rootGO = new GameObject("CombatHUD");
        Undo.RegisterCreatedObjectUndo(rootGO, "CombatHUD");
        rootGO.transform.SetParent(canvas.transform, false);
        var rootRT = rootGO.AddComponent<RectTransform>();
        StretchFull(rootRT);
        var hud = rootGO.AddComponent<CombatHUD>();
        rootGO.transform.SetAsFirstSibling();

        // ── TOP BAR (~88 px) ────────────────────────────────
        var top = Panel(rootGO.transform, "TopBar",
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(0.5f, 1f),
            anchoredPos: new Vector2(0f, 0f),
            sizeDelta: new Vector2(0f, 88f));
        var topImg = top.gameObject.AddComponent<Image>();
        topImg.color = DarkBg;
        var topH = top.gameObject.AddComponent<HorizontalLayoutGroup>();
        topH.padding        = new RectOffset(16, 16, 10, 10);
        topH.spacing        = 12f;
        topH.childAlignment   = TextAnchor.MiddleCenter;
        topH.childControlHeight = true;
        topH.childControlWidth  = true;
        topH.childForceExpandHeight = true;

        var teamABuilt = TeamHpBlock(top.transform, "TeamA_Block", "Équipe A");
        var timerHost = Panel(top, "TimerHost",
            anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: Vector2.zero,
            sizeDelta: new Vector2(100f, 72f));
        var leTimer = timerHost.gameObject.AddComponent<LayoutElement>();
        leTimer.preferredWidth  = 100f;
        leTimer.preferredHeight = 72f;
        leTimer.flexibleWidth   = 0f;

        var teamBBuilt = TeamHpBlock(top.transform, "TeamB_Block", "Équipe B");
        var leA = teamABuilt.root.gameObject.AddComponent<LayoutElement>();
        leA.flexibleWidth = 1f;
        leA.minWidth      = 180f;
        var leB = teamBBuilt.root.gameObject.AddComponent<LayoutElement>();
        leB.flexibleWidth = 1f;
        leB.minWidth      = 180f;

        // ── BOTTOM BAR (~132 px) ─────────────────────────────
        var bottom = Panel(rootGO.transform, "BottomBar",
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, 0f),
            pivot: new Vector2(0.5f, 0f),
            anchoredPos: new Vector2(0f, 0f),
            sizeDelta: new Vector2(0f, 132f));
        bottom.gameObject.AddComponent<Image>().color = DarkBg;
        var botH = bottom.gameObject.AddComponent<HorizontalLayoutGroup>();
        botH.padding  = new RectOffset(12, 12, 10, 10);
        botH.spacing  = 10f;
        botH.childAlignment = TextAnchor.MiddleCenter;

        var passiveBlock = PassiveBlock(bottom);
        var resBlock     = ResourcesBlock(bottom);
        var deckHost     = Panel(bottom, "DeckHost",
            anchorMin: Vector2.zero, anchorMax: Vector2.one,
            pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: Vector2.zero,
            sizeDelta: new Vector2(200f, 100f));
        var leDeck = deckHost.gameObject.AddComponent<LayoutElement>();
        leDeck.flexibleWidth = 3f;
        leDeck.minHeight = 100f;

        var endBtn = EndTurnButton(bottom);

        // ── Câblage CombatHUD ───────────────────────────────
        hud.teamALabel   = teamABuilt.label;
        hud.teamAHpFill  = teamABuilt.fill;
        hud.teamAHpValue = teamABuilt.value;

        hud.teamBLabel   = teamBBuilt.label;
        hud.teamBHpFill  = teamBBuilt.fill;
        hud.teamBHpValue = teamBBuilt.value;

        hud.passiveIcon     = passiveBlock.icon;
        hud.passiveNameText = passiveBlock.nameTmp;

        hud.paText = resBlock.pa;
        hud.pmText = resBlock.pm;

        hud.endTurnButton = endBtn;

        // ── Re-parent TimerUI + DeckUI ─────────────────────
        var timer = Object.FindObjectOfType<TimerUI>(true);
        if (timer != null)
        {
            Undo.SetTransformParent(timer.transform, timerHost, "Move TimerUI");
            StretchFull(timer.GetComponent<RectTransform>());
        }
        else
            Debug.LogWarning("[OracleCombatHUDBuilder] Aucun TimerUI trouvé — barre haute vide au centre.");

        var deck = Object.FindObjectOfType<DeckUI>(true);
        if (deck != null)
        {
            Undo.SetTransformParent(deck.transform, deckHost, "Move DeckUI");
            var drt = deck.GetComponent<RectTransform>();
            StretchFull(drt);
            var dle = deck.GetComponent<LayoutElement>();
            if (dle == null) dle = Undo.AddComponent<LayoutElement>(deck.gameObject);
            dle.flexibleWidth = 1f;
            dle.minHeight     = 80f;
        }
        else
            Debug.LogWarning("[OracleCombatHUDBuilder] Aucun DeckUI trouvé.");

        EditorUtility.SetDirty(hud);
        Selection.activeGameObject = rootGO;
        Debug.Log("[OracleCombatHUDBuilder] HUD 4.2.1 généré. CombatHUD en premier sous le Canvas (ne masque pas les popups avec tri plus tard).");

        EditorUtility.DisplayDialog("Oracle — HUD 4.2.1",
            "CombatHUD créé.\n\n" +
            "- TimerUI et DeckUI ont été déplacés dans les zones prévues.\n" +
            "- Passe les personnages dans l'Inspector si l'auto-detect ne suffit pas.", "OK");
    }

    struct HpBuilt
    {
        public RectTransform root;
        public TextMeshProUGUI label, value;
        public Image fill;
    }

    static HpBuilt TeamHpBlock(Transform parent, string name, string defaultTitle)
    {
        var block = Panel(parent, name,
            anchorMin: Vector2.zero, anchorMax: Vector2.one,
            pivot: new Vector2(0.5f, 0.5f),
            anchoredPos: Vector2.zero,
            sizeDelta: new Vector2(220f, 70f));
        block.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 0.85f);
        var v = block.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding      = new RectOffset(10, 10, 6, 6);
        v.spacing      = 4f;
        v.childAlignment = TextAnchor.UpperCenter;

        var labelGO = TextBlock(block, "Label", defaultTitle, 15f, FontStyles.Bold, Accent);
        var label   = labelGO.GetComponent<TextMeshProUGUI>();

        var barRow = Panel(block, "HpBarRow",
            Vector2.zero, new Vector2(0f, 22f), Vector2.zero, Vector2.one);
        var barLE  = barRow.gameObject.AddComponent<LayoutElement>();
        barLE.preferredHeight = 18f;
        barLE.minHeight       = 18f;
        var barBg = barRow.gameObject.AddComponent<Image>();
        barBg.color = BarBg;
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(barRow, false);
        var frt = fillGO.AddComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(2f, 2f);
        frt.offsetMax = new Vector2(-2f, -2f);
        var fill = fillGO.AddComponent<Image>();
        fill.color = HpOk;
        fill.type  = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;

        var valGO = TextBlock(block, "HpValue", "— / —", 13f, FontStyles.Normal, Color.white);
        var val   = valGO.GetComponent<TextMeshProUGUI>();

        return new HpBuilt { root = block, label = label, fill = fill, value = val };
    }

    struct PassiveW { public Image icon; public TextMeshProUGUI nameTmp; }

    static PassiveW PassiveBlock(RectTransform parent)
    {
        var block = Panel(parent, "PassiveBlock",
            Vector2.zero, new Vector2(200f, 100f), Vector2.zero, Vector2.one);
        block.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 0.9f);
        block.gameObject.AddComponent<LayoutElement>().preferredWidth = 200f;
        var h = block.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(8, 8, 8, 8);
        h.spacing = 8;

        var iconGO = new GameObject("PassiveIcon");
        iconGO.transform.SetParent(block, false);
        var irt = iconGO.AddComponent<RectTransform>();
        irt.sizeDelta = new Vector2(48f, 48f);
        iconGO.AddComponent<LayoutElement>().preferredWidth = 48f;
        var icon = iconGO.AddComponent<Image>();
        icon.color = new Color(0.35f, 0.35f, 0.4f);

        var nameGO = TextBlock(block, "PassiveName", "Passif", 14f, FontStyles.Bold, Accent);
        var nameTmp = nameGO.GetComponent<TextMeshProUGUI>();
        nameTmp.alignment = TextAlignmentOptions.MidlineLeft;

        return new PassiveW { icon = icon, nameTmp = nameTmp };
    }

    struct ResW { public TextMeshProUGUI pa, pm; }

    static ResW ResourcesBlock(RectTransform parent)
    {
        var block = Panel(parent, "ResourcesBlock",
            Vector2.zero, new Vector2(100f, 80f), Vector2.zero, Vector2.one);
        block.gameObject.AddComponent<LayoutElement>().preferredWidth = 100f;
        var v = block.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 4f;
        v.childAlignment = TextAnchor.MiddleCenter;

        var paGO = TextBlock(block, "PA", "PA: —", 16f, FontStyles.Bold, Color.white);
        var pmGO = TextBlock(block, "PM", "PM: —", 16f, FontStyles.Bold, Color.white);
        return new ResW
        {
            pa = paGO.GetComponent<TextMeshProUGUI>(),
            pm = pmGO.GetComponent<TextMeshProUGUI>(),
        };
    }

    static Button EndTurnButton(RectTransform parent)
    {
        var wrap = Panel(parent, "EndTurnWrap",
            Vector2.zero, new Vector2(130f, 56f), Vector2.zero, Vector2.one);
        wrap.gameObject.AddComponent<LayoutElement>().preferredWidth = 130f;

        var btnGO = new GameObject("EndTurnButton");
        btnGO.transform.SetParent(wrap, false);
        var brt = btnGO.AddComponent<RectTransform>();
        StretchFull(brt);
        var img = btnGO.AddComponent<Image>();
        img.color = Accent;
        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = img;

        var txtGO = TextBlock(brt, "Label", "Fin de tour", 16f, FontStyles.Bold, new Color(0.1f, 0.07f, 0.02f));
        StretchFull(txtGO.GetComponent<RectTransform>());
        txtGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        return btn;
    }

    static RectTransform Panel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
        return rt;
    }

    static RectTransform Panel(RectTransform parent, string name,
        Vector2 anchoredPos, Vector2 sizeDelta,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
        return rt;
    }

    static GameObject TextBlock(RectTransform parent, string name, string text,
        float size, FontStyles style, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(100f, 24f);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text       = text;
        tmp.fontSize   = size;
        tmp.fontStyle  = style;
        tmp.color      = col;
        tmp.alignment  = TextAlignmentOptions.Center;
        return go;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale       = Vector3.one;
    }
}
#endif
