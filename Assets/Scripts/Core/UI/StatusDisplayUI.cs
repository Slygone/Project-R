using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Reusable status display for combat entities (player + enemies).
///
/// Compact view (always visible):
///   One green chip labeled "Buff(s)" if any buffs active.
///   One red chip labeled "Debuff(s)" if any debuffs active.
///   Hidden entirely when no statuses.
///
/// Expanded view (X key toggle):
///   Details container with "Buffs" / "Debuffs" sections.
///   Individual chips per status showing name + duration.
///   Hover a chip for tooltip with full description.
///   Details container renders in front of all other UI.
///
/// All data comes from ActiveStatusInfo.Category / .Description
/// populated by the caller — no DataCache lookups in this class.
/// </summary>
public class StatusDisplayUI
{
    // ═══════════════ COLORS ═══════════════
    private static readonly Color BuffBg     = new Color(0.15f, 0.35f, 0.15f, 0.92f);
    private static readonly Color BuffBorder = new Color(0.3f, 0.75f, 0.35f, 0.9f);
    private static readonly Color BuffText   = new Color(0.7f, 1f, 0.75f);

    private static readonly Color DebuffBg     = new Color(0.45f, 0.12f, 0.12f, 0.92f);
    private static readonly Color DebuffBorder = new Color(0.8f, 0.25f, 0.25f, 0.9f);
    private static readonly Color DebuffText   = new Color(1f, 0.75f, 0.75f);

    private static readonly Color PanelBg = new Color(0.08f, 0.08f, 0.12f, 0.95f);

    // ═══════════════ COMPACT (summary chips) ═══════════════
    private GameObject compactContainer;
    private GameObject buffSummaryChip;
    private TextMeshProUGUI buffSummaryLabel;
    private GameObject debuffSummaryChip;
    private TextMeshProUGUI debuffSummaryLabel;

    // ═══════════════ DETAILS (expanded panel) ═══════════════
    private GameObject detailsContainer;
    private GameObject detailBuffSection;
    private Transform detailBuffGrid;
    private GameObject detailDebuffSection;
    private Transform detailDebuffGrid;
    private List<GameObject> detailChipObjs = new List<GameObject>();

    // ═══════════════ STATE ═══════════════
    private bool isExpanded;
    private bool isWorldSpace;
    private System.Action<string> showTooltip;
    private System.Action hideTooltip;
    private List<ActiveStatusInfo> cachedBuffs = new List<ActiveStatusInfo>();
    private List<ActiveStatusInfo> cachedDebuffs = new List<ActiveStatusInfo>();

    // ═══════════════ SIZING ═══════════════
    private float summaryChipW, summaryChipH, summaryFontSize;
    private float detailChipW, detailChipH, detailFontSize, headerFontSize;
    private float detailPanelW;
    private float chipGap;
    private string lastDetailFingerprint = "";

    // ═══════════════ STATIC ═══════════════
    public static bool GlobalExpanded { get; set; }

    // ═══════════════ CONSTRUCTOR ═══════════════

    public StatusDisplayUI(Transform parent, bool worldSpace, Vector2 anchoredPos,
        System.Action<string> onShowTooltip, System.Action onHideTooltip)
    {
        isWorldSpace = worldSpace;
        showTooltip = onShowTooltip;
        hideTooltip = onHideTooltip;

        if (worldSpace)
        {
            // Compact chips (+20% from base 110x28)
            summaryChipW = 132f; summaryChipH = 34f; summaryFontSize = 22f;
            // Detail chips + panel (+30% from base 180x28, panel 300)
            detailChipW = 234f; detailChipH = 36f;
            detailFontSize = 21f; headerFontSize = 23f;
            detailPanelW = 390f; chipGap = 5f;
        }
        else
        {
            // Compact chips (+20% from base 72x20)
            summaryChipW = 86f; summaryChipH = 24f; summaryFontSize = 14f;
            // Detail chips + panel (+30% from base 130x20, panel 220)
            detailChipW = 169f; detailChipH = 26f;
            detailFontSize = 14f; headerFontSize = 16f;
            detailPanelW = 286f; chipGap = 4f;
        }

        BuildCompact(parent, anchoredPos);
        BuildDetails(parent, anchoredPos);
    }

    // ────────── Compact: two summary chips ──────────

    private void BuildCompact(Transform parent, Vector2 pos)
    {
        compactContainer = new GameObject("StatusChips");
        compactContainer.transform.SetParent(parent, false);

        var rect = compactContainer.AddComponent<RectTransform>();
        if (isWorldSpace)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
        else
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0f, 0f);
        }
        rect.anchoredPosition = pos;

        var hlg = compactContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = chipGap;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset(2, 2, 2, 2);

        var csf = compactContainer.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Pre-create the two summary chips (hidden by default)
        buffSummaryChip = CreateSummaryChip("Buff(s)", true, out buffSummaryLabel);
        debuffSummaryChip = CreateSummaryChip("Debuff(s)", false, out debuffSummaryLabel);
        buffSummaryChip.SetActive(false);
        debuffSummaryChip.SetActive(false);

        compactContainer.SetActive(false);
    }

    private GameObject CreateSummaryChip(string text, bool isBuff, out TextMeshProUGUI label)
    {
        var obj = new GameObject(isBuff ? "BuffSummary" : "DebuffSummary");
        obj.transform.SetParent(compactContainer.transform, false);

        var img = obj.AddComponent<Image>();
        img.color = isBuff ? BuffBg : DebuffBg;
        img.raycastTarget = false;

        var outline = obj.AddComponent<Outline>();
        outline.effectColor = isBuff ? BuffBorder : DebuffBorder;
        outline.effectDistance = new Vector2(1, 1);

        var le = obj.AddComponent<LayoutElement>();
        le.preferredWidth = summaryChipW;
        le.preferredHeight = summaryChipH;

        var textObj = new GameObject("Label");
        textObj.transform.SetParent(obj.transform, false);
        var tr = textObj.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(4, 1); tr.offsetMax = new Vector2(-4, -1);

        label = textObj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = summaryFontSize;
        label.color = isBuff ? BuffText : DebuffText;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;

        return obj;
    }

    // ────────── Details container (X toggle) ──────────

    private void BuildDetails(Transform parent, Vector2 chipPos)
    {
        detailsContainer = new GameObject("StatusDetailsPanel");
        detailsContainer.transform.SetParent(parent, false);

        var rect = detailsContainer.AddComponent<RectTransform>();
        if (isWorldSpace)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(chipPos.x, chipPos.y - summaryChipH - 4f);
        }
        else
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(chipPos.x, chipPos.y + summaryChipH + 6f);
        }
        rect.sizeDelta = new Vector2(detailPanelW, 0f);

        var bg = detailsContainer.AddComponent<Image>();
        bg.color = PanelBg;
        bg.raycastTarget = false;

        var outline = detailsContainer.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.4f, 0.5f, 0.9f);
        outline.effectDistance = new Vector2(2, 2);

        // Render in front of everything
        var canvas = detailsContainer.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 500;
        detailsContainer.AddComponent<GraphicRaycaster>();

        var vlg = detailsContainer.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.spacing = 4f;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = detailsContainer.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Buff section
        detailBuffSection = BuildDetailSection("Buffs", BuffText, out detailBuffGrid);
        // Debuff section
        detailDebuffSection = BuildDetailSection("Debuffs", DebuffText, out detailDebuffGrid);

        detailsContainer.SetActive(false);
    }

    private GameObject BuildDetailSection(string label, Color headerColor, out Transform chipGrid)
    {
        var section = new GameObject(label + "Section");
        section.transform.SetParent(detailsContainer.transform, false);

        var vlg = section.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 3f;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var fit = section.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Header
        var headerObj = new GameObject("Header");
        headerObj.transform.SetParent(section.transform, false);
        var headerTmp = headerObj.AddComponent<TextMeshProUGUI>();
        headerTmp.text = label;
        headerTmp.fontSize = headerFontSize;
        headerTmp.fontStyle = FontStyles.Bold;
        headerTmp.color = headerColor;
        headerTmp.alignment = TextAlignmentOptions.Left;
        headerTmp.raycastTarget = false;
        var hle = headerObj.AddComponent<LayoutElement>();
        hle.preferredHeight = headerFontSize + 4f;

        // Grid for individual chips
        var gridObj = new GameObject("ChipGrid");
        gridObj.transform.SetParent(section.transform, false);

        var grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(detailChipW, detailChipH);
        grid.spacing = new Vector2(chipGap, chipGap);
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, Mathf.FloorToInt((detailPanelW - 12f) / (detailChipW + chipGap)));

        var gFit = gridObj.AddComponent<ContentSizeFitter>();
        gFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        chipGrid = gridObj.transform;
        return section;
    }

    // ═══════════════ UPDATE (called every frame) ═══════════════

    public void UpdateStatuses(List<ActiveStatusInfo> statuses)
    {
        // Split by category
        cachedBuffs.Clear();
        cachedDebuffs.Clear();
        foreach (var s in statuses)
        {
            if (s.IsBuff) cachedBuffs.Add(s);
            else cachedDebuffs.Add(s);
        }

        bool hasBuffs = cachedBuffs.Count > 0;
        bool hasDebuffs = cachedDebuffs.Count > 0;
        bool hasAny = hasBuffs || hasDebuffs;

        // Show/hide compact container and summary chips with singular/plural labels
        compactContainer.SetActive(hasAny);
        buffSummaryChip.SetActive(hasBuffs);
        debuffSummaryChip.SetActive(hasDebuffs);
        if (hasBuffs)
            buffSummaryLabel.text = cachedBuffs.Count == 1 ? "Buff" : "Buffs";
        if (hasDebuffs)
            debuffSummaryLabel.text = cachedDebuffs.Count == 1 ? "Debuff" : "Debuffs";

        // Update detail panel if expanded — only rebuild when data changes
        if (isExpanded && hasAny)
        {
            string fp = BuildFingerprint();
            if (fp != lastDetailFingerprint)
            {
                hideTooltip?.Invoke();
                RebuildDetailChips();
                lastDetailFingerprint = fp;
            }
        }
        else if (isExpanded && !hasAny)
        {
            // Nothing to show — collapse
            hideTooltip?.Invoke();
            detailsContainer.SetActive(false);
            lastDetailFingerprint = "";
        }
    }

    // ═══════════════ EXPAND / COLLAPSE ═══════════════

    public void SetExpanded(bool expanded)
    {
        isExpanded = expanded;

        if (expanded)
        {
            bool hasAny = cachedBuffs.Count > 0 || cachedDebuffs.Count > 0;
            if (hasAny)
            {
                detailsContainer.SetActive(true);
                RebuildDetailChips();
                lastDetailFingerprint = BuildFingerprint();
            }
        }
        else
        {
            detailsContainer.SetActive(false);
            ClearDetailChips();
            hideTooltip?.Invoke();
            lastDetailFingerprint = "";
        }
    }

    // ────────── Detail chip rebuild ──────────

    private void RebuildDetailChips()
    {
        ClearDetailChips();

        bool hasBuffs = cachedBuffs.Count > 0;
        bool hasDebuffs = cachedDebuffs.Count > 0;

        detailBuffSection.SetActive(hasBuffs);
        detailDebuffSection.SetActive(hasDebuffs);

        if (hasBuffs)
        {
            foreach (var s in cachedBuffs)
                detailChipObjs.Add(CreateDetailChip(detailBuffGrid, s, true));
        }
        if (hasDebuffs)
        {
            foreach (var s in cachedDebuffs)
                detailChipObjs.Add(CreateDetailChip(detailDebuffGrid, s, false));
        }
    }

    private void ClearDetailChips()
    {
        foreach (var obj in detailChipObjs)
        {
            if (obj != null) Object.Destroy(obj);
        }
        detailChipObjs.Clear();
    }

    private GameObject CreateDetailChip(Transform parent, ActiveStatusInfo status, bool isBuff)
    {
        string name = !string.IsNullOrEmpty(status.DisplayName) ? status.DisplayName : status.StatusId;
        string label = status.Duration > 0 ? $"{name} ({status.Duration}t)" : name;
        string tooltip = !string.IsNullOrEmpty(status.Description) ? status.Description : name;

        var obj = new GameObject($"Detail_{name}");
        obj.transform.SetParent(parent, false);

        var bg = obj.AddComponent<Image>();
        bg.color = isBuff ? BuffBg : DebuffBg;
        bg.raycastTarget = true;

        var outline = obj.AddComponent<Outline>();
        outline.effectColor = isBuff ? BuffBorder : DebuffBorder;
        outline.effectDistance = new Vector2(1, 1);

        var textObj = new GameObject("Label");
        textObj.transform.SetParent(obj.transform, false);
        var tr = textObj.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(4, 1); tr.offsetMax = new Vector2(-4, -1);

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = detailFontSize;
        tmp.color = isBuff ? BuffText : DebuffText;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = detailFontSize * 0.6f;
        tmp.fontSizeMax = detailFontSize;
        tmp.raycastTarget = false;

        // Tooltip hover
        var trigger = obj.AddComponent<EventTrigger>();
        string tooltipCopy = tooltip;
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener((_) => showTooltip?.Invoke(tooltipCopy));
        trigger.triggers.Add(enter);
        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener((_) => hideTooltip?.Invoke());
        trigger.triggers.Add(exit);

        return obj;
    }

    // ═══════════════ FINGERPRINT ═══════════════

    private string BuildFingerprint()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var s in cachedBuffs)
            sb.Append(s.StatusId).Append(':').Append(s.Duration).Append(':').Append(s.Stacks).Append(',');
        sb.Append('|');
        foreach (var s in cachedDebuffs)
            sb.Append(s.StatusId).Append(':').Append(s.Duration).Append(':').Append(s.Stacks).Append(',');
        return sb.ToString();
    }

    // ═══════════════ CLEANUP ═══════════════

    public void Destroy()
    {
        ClearDetailChips();
        if (compactContainer != null) Object.Destroy(compactContainer);
        if (detailsContainer != null) Object.Destroy(detailsContainer);
        compactContainer = null;
        detailsContainer = null;
    }
}
