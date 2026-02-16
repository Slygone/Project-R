using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Loot/Reward UI — shown after combat victory.
/// Handles gold collection, sigil enchantment, and relic rewards.
/// Combat display is handled entirely by CombatArena (in-world combat).
/// </summary>
public class CombatUI : MonoBehaviour
{
    // Loot panel
    private GameObject lootPanel;
    private TextMeshProUGUI lootTitleText;
    private TextMeshProUGUI lootGoldText;
    private TextMeshProUGUI lootXPText;
    private Button collectLootButton;
    private Button goldRewardButton;
    private GameObject sigilRewardButton;
    private GameObject relicRewardButton;
    private TextMeshProUGUI sigilInstructionText;
    private GameObject rewardContainer;
    private GameObject enchantOverlay;

    // Loot state
    private CombatManager combatManager;
    private int pendingGold;
    private int pendingXP;
    private Player pendingPlayer;
    private NodeBase pendingNode;
    private Element pendingSigil = Element.None;
    private RelicData pendingRelic = null;
    private bool isEnchanting = false;
    private bool goldCollected = false;
    private bool relicCollected = false;

    void Awake()
    {
        combatManager = FindFirstObjectByType<CombatManager>();
        SetupUI();
    }

    private void SetupUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            GameLog.Error(GameLogCategory.System, "[CombatUI]", "SetupFail | reason=CanvasNotFound");
            return;
        }

        lootPanel = CreateLootPanel(canvas.transform);
        lootPanel.SetActive(false);
    }

    #region Loot Panel

    private GameObject CreateLootPanel(Transform parent)
    {
        var pn = new GameObject("LootPanel");
        pn.transform.SetParent(parent, false);
        var rc = pn.AddComponent<RectTransform>();
        rc.anchorMin = new Vector2(0.25f, 0.2f);
        rc.anchorMax = new Vector2(0.75f, 0.8f);
        rc.offsetMin = Vector2.zero;
        rc.offsetMax = Vector2.zero;
        var bg = pn.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // Title
        var to = new GameObject("Title");
        to.transform.SetParent(pn.transform, false);
        var tr = to.AddComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 0.85f);
        tr.anchorMax = new Vector2(1, 0.98f);
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        lootTitleText = to.AddComponent<TextMeshProUGUI>();
        lootTitleText.text = "Reward";
        lootTitleText.alignment = TextAlignmentOptions.Center;
        lootTitleText.fontSize = 36;
        lootTitleText.fontStyle = FontStyles.Bold;
        lootTitleText.color = new Color(1f, 0.85f, 0.2f);

        // Reward container
        rewardContainer = new GameObject("RewardContainer");
        rewardContainer.transform.SetParent(pn.transform, false);
        var cr = rewardContainer.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0.05f, 0.25f);
        cr.anchorMax = new Vector2(0.95f, 0.82f);
        cr.offsetMin = Vector2.zero;
        cr.offsetMax = Vector2.zero;

        // Gold reward button
        var go = new GameObject("GoldReward");
        go.transform.SetParent(rewardContainer.transform, false);
        var gr = go.AddComponent<RectTransform>();
        gr.anchorMin = new Vector2(0.05f, 0.7f);
        gr.anchorMax = new Vector2(0.95f, 0.95f);
        gr.offsetMin = Vector2.zero;
        gr.offsetMax = Vector2.zero;
        var gi = go.AddComponent<Image>();
        gi.color = new Color(0.2f, 0.18f, 0.1f, 0.9f);
        goldRewardButton = go.AddComponent<Button>();
        goldRewardButton.targetGraphic = gi;
        goldRewardButton.onClick.AddListener(OnGoldRewardClicked);

        var gto = new GameObject("Text");
        gto.transform.SetParent(go.transform, false);
        var gtr = gto.AddComponent<RectTransform>();
        gtr.anchorMin = Vector2.zero;
        gtr.anchorMax = Vector2.one;
        gtr.offsetMin = new Vector2(10, 0);
        gtr.offsetMax = new Vector2(-10, 0);
        lootGoldText = gto.AddComponent<TextMeshProUGUI>();
        lootGoldText.alignment = TextAlignmentOptions.MidlineLeft;
        lootGoldText.fontSize = 22;
        lootGoldText.color = new Color(1f, 0.85f, 0.2f);

        // XP display
        var xo = new GameObject("XPReward");
        xo.transform.SetParent(rewardContainer.transform, false);
        var xr = xo.AddComponent<RectTransform>();
        xr.anchorMin = new Vector2(0.05f, 0.45f);
        xr.anchorMax = new Vector2(0.95f, 0.68f);
        xr.offsetMin = Vector2.zero;
        xr.offsetMax = Vector2.zero;
        var xi = xo.AddComponent<Image>();
        xi.color = new Color(0.1f, 0.15f, 0.2f, 0.9f);

        var xto = new GameObject("Text");
        xto.transform.SetParent(xo.transform, false);
        var xtr = xto.AddComponent<RectTransform>();
        xtr.anchorMin = Vector2.zero;
        xtr.anchorMax = Vector2.one;
        xtr.offsetMin = new Vector2(10, 0);
        xtr.offsetMax = new Vector2(-10, 0);
        lootXPText = xto.AddComponent<TextMeshProUGUI>();
        lootXPText.alignment = TextAlignmentOptions.MidlineLeft;
        lootXPText.fontSize = 22;
        lootXPText.color = new Color(0.4f, 0.8f, 1f);

        // Sigil reward
        sigilRewardButton = new GameObject("SigilReward");
        sigilRewardButton.transform.SetParent(rewardContainer.transform, false);
        var sr = sigilRewardButton.AddComponent<RectTransform>();
        sr.anchorMin = new Vector2(0.05f, 0.22f);
        sr.anchorMax = new Vector2(0.95f, 0.43f);
        sr.offsetMin = Vector2.zero;
        sr.offsetMax = Vector2.zero;
        var si = sigilRewardButton.AddComponent<Image>();
        si.color = new Color(0.15f, 0.12f, 0.2f, 0.9f);
        var sb = sigilRewardButton.AddComponent<Button>();
        sb.targetGraphic = si;
        sb.onClick.AddListener(OnSigilRewardClicked);

        var sto = new GameObject("Text");
        sto.transform.SetParent(sigilRewardButton.transform, false);
        var str2 = sto.AddComponent<RectTransform>();
        str2.anchorMin = Vector2.zero;
        str2.anchorMax = Vector2.one;
        str2.offsetMin = new Vector2(10, 0);
        str2.offsetMax = new Vector2(-10, 0);
        var stx = sto.AddComponent<TextMeshProUGUI>();
        stx.alignment = TextAlignmentOptions.MidlineLeft;
        stx.fontSize = 22;
        stx.color = Color.white;

        sigilRewardButton.SetActive(false);

        // Relic reward
        relicRewardButton = new GameObject("RelicReward");
        relicRewardButton.transform.SetParent(rewardContainer.transform, false);
        var rr = relicRewardButton.AddComponent<RectTransform>();
        rr.anchorMin = new Vector2(0.05f, 0.0f);
        rr.anchorMax = new Vector2(0.95f, 0.20f);
        rr.offsetMin = Vector2.zero;
        rr.offsetMax = Vector2.zero;
        var ri = relicRewardButton.AddComponent<Image>();
        ri.color = new Color(0.15f, 0.1f, 0.2f, 0.9f);
        var rb = relicRewardButton.AddComponent<Button>();
        rb.targetGraphic = ri;
        rb.onClick.AddListener(OnRelicRewardClicked);

        var rto = new GameObject("Text");
        rto.transform.SetParent(relicRewardButton.transform, false);
        var rtr = rto.AddComponent<RectTransform>();
        rtr.anchorMin = Vector2.zero;
        rtr.anchorMax = Vector2.one;
        rtr.offsetMin = new Vector2(10, 0);
        rtr.offsetMax = new Vector2(-10, 0);
        var rtx = rto.AddComponent<TextMeshProUGUI>();
        rtx.alignment = TextAlignmentOptions.MidlineLeft;
        rtx.fontSize = 22;
        rtx.color = Color.white;

        relicRewardButton.SetActive(false);

        // Sigil instruction text
        var sio = new GameObject("SigilInstruction");
        sio.transform.SetParent(rewardContainer.transform, false);
        var sir = sio.AddComponent<RectTransform>();
        sir.anchorMin = new Vector2(0.05f, -0.05f);
        sir.anchorMax = new Vector2(0.95f, 0.02f);
        sir.offsetMin = Vector2.zero;
        sir.offsetMax = Vector2.zero;
        sigilInstructionText = sio.AddComponent<TextMeshProUGUI>();
        sigilInstructionText.alignment = TextAlignmentOptions.Center;
        sigilInstructionText.fontSize = 16;
        sigilInstructionText.color = new Color(0.7f, 0.9f, 1f);

        // Collect (Done) button
        var cbo = new GameObject("CollectButton");
        cbo.transform.SetParent(pn.transform, false);
        var cbr = cbo.AddComponent<RectTransform>();
        cbr.anchorMin = new Vector2(0.3f, 0.05f);
        cbr.anchorMax = new Vector2(0.7f, 0.18f);
        cbr.offsetMin = Vector2.zero;
        cbr.offsetMax = Vector2.zero;
        var cbi = cbo.AddComponent<Image>();
        cbi.color = new Color(0.2f, 0.5f, 0.2f);
        collectLootButton = cbo.AddComponent<Button>();
        collectLootButton.targetGraphic = cbi;
        collectLootButton.onClick.AddListener(OnCollectLootClicked);

        var cbto = new GameObject("Text");
        cbto.transform.SetParent(cbo.transform, false);
        var cbtr = cbto.AddComponent<RectTransform>();
        cbtr.anchorMin = Vector2.zero;
        cbtr.anchorMax = Vector2.one;
        cbtr.offsetMin = Vector2.zero;
        cbtr.offsetMax = Vector2.zero;
        var cbtx = cbto.AddComponent<TextMeshProUGUI>();
        cbtx.text = "DONE";
        cbtx.alignment = TextAlignmentOptions.Center;
        cbtx.fontSize = 24;
        cbtx.fontStyle = FontStyles.Bold;
        cbtx.color = Color.white;

        return pn;
    }

    public void ShowLootPanel(int gold, int xp, Player p, NodeBase node, string title = null, bool isElite = false, bool isBoss = false, bool dropsSigil = false, bool dropsRelic = false)
    {
        pendingGold = gold;
        pendingXP = xp;
        pendingPlayer = p;
        pendingNode = node;
        goldCollected = false;
        relicCollected = false;
        pendingSigil = Element.None;
        pendingRelic = null;
        isEnchanting = false;

        if (lootTitleText != null) lootTitleText.text = isBoss ? "BOSS DEFEATED!" : "Reward";
        if (goldRewardButton != null) goldRewardButton.interactable = true;
        if (lootGoldText != null) lootGoldText.text = $"<color=#FFD700>\u2022</color>Gold:+{gold}<size=16>(Click to collect)</size>";
        if (lootXPText != null) lootXPText.text = $"<color=#6CF>\u2022</color>Experience:+{xp}<size=16>(Auto)</size>";

        bool sigilsDisabled = p != null && p.AreSigilsDisabled();
        bool showSigil = (dropsSigil || isElite) && !sigilsDisabled;
        if (sigilRewardButton != null)
        {
            if (showSigil)
            {
                var els = new Element[] { Element.Fire, Element.Ice, Element.Water, Element.Wind, Element.Rock, Element.Lightning };
                pendingSigil = els[Random.Range(0, els.Length)];
                var si = sigilRewardButton.GetComponent<Image>();
                if (si != null) si.color = ElementColors.Get(pendingSigil) * 0.4f;
                var st = sigilRewardButton.GetComponentInChildren<TextMeshProUGUI>();
                if (st != null)
                {
                    st.text = $"<color=#{ColorUtility.ToHtmlStringRGB(ElementColors.Get(pendingSigil))}>\u25C8</color>{pendingSigil} Sigil<size=16>(Click to enchant)</size>";
                    st.color = ElementColors.Get(pendingSigil);
                }
                sigilRewardButton.GetComponent<Button>().interactable = true;
                sigilRewardButton.SetActive(true);
            }
            else
            {
                sigilRewardButton.SetActive(false);
            }
        }

        if (relicRewardButton != null)
        {
            if (dropsRelic && DataCache.Relics != null && DataCache.Relics.Count > 0)
            {
                // Rarity-based selection: bosses drop Legendary, elites 50/50, regular = Common
                string dropRarity = isBoss ? "Legendary" : (isElite && Random.value < 0.5f ? "Legendary" : "Common");
                pendingRelic = GetRandomRelicByRarity(dropRarity);
                if (pendingRelic == null) pendingRelic = GetRandomRelicByRarity("Common");
                if (pendingRelic != null)
                {
                    var ri = relicRewardButton.GetComponent<Image>();
                    if (ri != null) ri.color = new Color(0.6f, 0.4f, 0.8f, 1f);
                    var rt = relicRewardButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (rt != null) rt.text = $"<color=#96f>\u2666</color>{pendingRelic.DisplayName}<size=16>(Click to collect)</size>";
                    relicRewardButton.GetComponent<Button>().interactable = true;
                    relicRewardButton.SetActive(true);
                }
                else
                {
                    relicRewardButton.SetActive(false);
                }
            }
            else
            {
                relicRewardButton.SetActive(false);
            }
        }

        if (sigilInstructionText != null) sigilInstructionText.text = "";
        lootPanel.SetActive(true);
    }

    public void HideCombat()
    {
        if (lootPanel != null) lootPanel.SetActive(false);
        if (enchantOverlay != null) { Destroy(enchantOverlay); enchantOverlay = null; }
    }

    private void OnGoldRewardClicked()
    {
        if (goldCollected || pendingPlayer == null) return;
        pendingPlayer.AddGold(pendingGold);
        goldCollected = true;
        if (lootGoldText != null) lootGoldText.text = $"<color=#888><s>Gold:+{pendingGold}</s>(Collected)</color>";
        if (goldRewardButton != null) goldRewardButton.interactable = false;
    }

    private void OnSigilRewardClicked()
    {
        if (pendingSigil == Element.None) return;
        isEnchanting = true;
        if (sigilInstructionText != null) sigilInstructionText.text = $"Click a skill to enchant with {pendingSigil}";
        lootPanel.SetActive(false);
        ShowEnchantmentSkillSelection();
    }

    private void OnRelicRewardClicked()
    {
        if (pendingRelic == null || relicCollected || pendingPlayer == null) return;
        pendingPlayer.AddRelic(pendingRelic);
        relicCollected = true;
        if (relicRewardButton != null)
        {
            relicRewardButton.GetComponent<Button>().interactable = false;
            var t = relicRewardButton.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = $"<color=#96f>\u2666</color>{pendingRelic.DisplayName}<size=16>(Collected!)</size>";
        }
    }

    private void ShowEnchantmentSkillSelection()
    {
        var ov = new GameObject("EnchantOverlay");
        ov.transform.SetParent(lootPanel.transform.parent, false);
        var or2 = ov.AddComponent<RectTransform>();
        or2.anchorMin = Vector2.zero;
        or2.anchorMax = Vector2.one;
        or2.offsetMin = Vector2.zero;
        or2.offsetMax = Vector2.zero;
        var ob = ov.AddComponent<Image>();
        ob.color = new Color(0, 0, 0, 0.7f);

        var io = new GameObject("Instr");
        io.transform.SetParent(ov.transform, false);
        var ir = io.AddComponent<RectTransform>();
        ir.anchorMin = new Vector2(0.2f, 0.6f);
        ir.anchorMax = new Vector2(0.8f, 0.8f);
        ir.offsetMin = Vector2.zero;
        ir.offsetMax = Vector2.zero;
        var it = io.AddComponent<TextMeshProUGUI>();
        it.text = $"Select skill to enchant with <color=#{ColorUtility.ToHtmlStringRGB(ElementColors.Get(pendingSigil))}>{pendingSigil}</color>";
        it.alignment = TextAlignmentOptions.Center;
        it.fontSize = 28;
        it.color = Color.white;

        CreateEnchantmentSkillButtons(ov.transform);
        enchantOverlay = ov;
    }

    private void CreateEnchantmentSkillButtons(Transform parent)
    {
        if (pendingPlayer == null) return;
        var ch = pendingPlayer.GetCharacter();
        if (ch == null) return;

        var bc = new GameObject("SkillBtns");
        bc.transform.SetParent(parent, false);
        var bcr = bc.AddComponent<RectTransform>();
        bcr.anchorMin = new Vector2(0.1f, 0.25f);
        bcr.anchorMax = new Vector2(0.9f, 0.55f);
        bcr.offsetMin = Vector2.zero;
        bcr.offsetMax = Vector2.zero;

        string[] nms = { ch.Skill1, ch.Skill2, ch.Skill3, ch.Skill4, ch.Skill5 };
        float bw = 0.18f, sp = 0.02f, sx = 0.5f - (2.5f * bw + 2 * sp);

        for (int i = 0; i < 5; i++)
        {
            int sn = i + 1;
            string nm = nms[i];
            Element ce = pendingPlayer.GetSkillElement(sn);

            var bo = new GameObject($"Skill{sn}Btn");
            bo.transform.SetParent(bc.transform, false);
            var br = bo.AddComponent<RectTransform>();
            float xp = sx + i * (bw + sp);
            br.anchorMin = new Vector2(xp, 0.1f);
            br.anchorMax = new Vector2(xp + bw, 0.9f);
            br.offsetMin = Vector2.zero;
            br.offsetMax = Vector2.zero;

            var bi = bo.AddComponent<Image>();
            bi.color = ce != Element.None ? ElementColors.Get(ce) * 0.5f : new Color(0.25f, 0.25f, 0.3f);

            var bt = bo.AddComponent<Button>();
            bt.targetGraphic = bi;
            bt.onClick.AddListener(() => OnEnchantSkillClicked(sn));

            var to = new GameObject("Text");
            to.transform.SetParent(bo.transform, false);
            var tr2 = to.AddComponent<RectTransform>();
            tr2.anchorMin = Vector2.zero;
            tr2.anchorMax = Vector2.one;
            tr2.offsetMin = new Vector2(3, 3);
            tr2.offsetMax = new Vector2(-3, -3);
            var tx = to.AddComponent<TextMeshProUGUI>();
            tx.text = ce != Element.None
                ? $"<size=12>{sn}</size>\n{nm}\n<color=#{ColorUtility.ToHtmlStringRGB(ElementColors.Get(ce))}><b>[{ce}]</b></color>"
                : $"<size=12>{sn}</size>\n{nm}\n<color=#666>[None]</color>";
            tx.alignment = TextAlignmentOptions.Center;
            tx.fontSize = 12;
            tx.color = Color.white;
        }

        // Cancel button
        var co = new GameObject("CancelBtn");
        co.transform.SetParent(parent, false);
        var cr2 = co.AddComponent<RectTransform>();
        cr2.anchorMin = new Vector2(0.4f, 0.08f);
        cr2.anchorMax = new Vector2(0.6f, 0.18f);
        cr2.offsetMin = Vector2.zero;
        cr2.offsetMax = Vector2.zero;
        var ci = co.AddComponent<Image>();
        ci.color = new Color(0.5f, 0.2f, 0.2f);
        var cb = co.AddComponent<Button>();
        cb.targetGraphic = ci;
        cb.onClick.AddListener(OnCancelEnchantment);

        var cto = new GameObject("Text");
        cto.transform.SetParent(co.transform, false);
        var ctr = cto.AddComponent<RectTransform>();
        ctr.anchorMin = Vector2.zero;
        ctr.anchorMax = Vector2.one;
        var ctx = cto.AddComponent<TextMeshProUGUI>();
        ctx.text = "Cancel";
        ctx.alignment = TextAlignmentOptions.Center;
        ctx.fontSize = 16;
        ctx.color = Color.white;
    }

    private void OnEnchantSkillClicked(int sn)
    {
        if (!isEnchanting || pendingSigil == Element.None || pendingPlayer == null) return;
        pendingPlayer.EnchantSkill(sn, pendingSigil);
        isEnchanting = false;
        pendingSigil = Element.None;
        if (enchantOverlay != null) { Destroy(enchantOverlay); enchantOverlay = null; }
        if (sigilRewardButton != null)
        {
            var t = sigilRewardButton.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = "<color=#888><s>Sigil</s>(Used)</color>";
            sigilRewardButton.GetComponent<Button>().interactable = false;
        }
        lootPanel.SetActive(true);
        if (sigilInstructionText != null) sigilInstructionText.text = $"Skill {sn} enchanted!";
    }

    private void OnCancelEnchantment()
    {
        isEnchanting = false;
        if (enchantOverlay != null) { Destroy(enchantOverlay); enchantOverlay = null; }
        lootPanel.SetActive(true);
        if (sigilInstructionText != null) sigilInstructionText.text = "";
    }

    private void OnCollectLootClicked()
    {
        if (!goldCollected && pendingPlayer != null)
        {
            pendingPlayer.AddGold(pendingGold);
            goldCollected = true;
        }
        lootPanel.SetActive(false);
        if (enchantOverlay != null) { Destroy(enchantOverlay); enchantOverlay = null; }
        if (combatManager == null) combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager != null) combatManager.OnLootCollected();
    }
    
    private static RelicData GetRandomRelicByRarity(string rarity)
    {
        if (DataCache.Relics == null || DataCache.Relics.Count == 0) return null;
        
        var pool = new System.Collections.Generic.List<RelicData>();
        foreach (var r in DataCache.Relics)
        {
            if (r.Rarity == rarity) pool.Add(r);
        }
        
        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

    #endregion
}
