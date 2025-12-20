using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PlayerStatsUI : MonoBehaviour
{
    private GameObject statsPanel;
    private TextMeshProUGUI statsText;
    private TextMeshProUGUI relicsText;
    private GameObject resistInfoIcon;
    private GameObject resistTooltipPanel;
    private TextMeshProUGUI resistTooltipText;
    private bool isOpen = false;
    private Referencer refs;

    void Start()
    {
        refs = FindFirstObjectByType<Referencer>();
        SetupUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleStatsPanel();
        }
    }

    private void SetupUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[PlayerStatsUI] Canvas not found");
            return;
        }

        statsPanel = CreateStatsPanel(canvas.transform);
        statsPanel.SetActive(false);
    }

    private GameObject CreateStatsPanel(Transform parent)
    {
        var panel = new GameObject("PlayerStatsPanel");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.05f, 0.05f);
        rect.anchorMax = new Vector2(0.42f, 0.95f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.9f);
        titleRect.anchorMax = new Vector2(1, 1f);
        titleRect.offsetMin = new Vector2(10, 0);
        titleRect.offsetMax = new Vector2(-10, -5);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "PLAYER STATS";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 28;
        titleText.color = new Color(1f, 0.85f, 0.2f);

        var infoObj = new GameObject("ResistInfo");
        infoObj.transform.SetParent(panel.transform, false);
        var infoRect = infoObj.AddComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0.92f, 0.90f);
        infoRect.anchorMax = new Vector2(0.98f, 0.98f);
        infoRect.offsetMin = Vector2.zero;
        infoRect.offsetMax = Vector2.zero;

        var infoBg = infoObj.AddComponent<Image>();
        infoBg.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);

        var infoTextObj = new GameObject("Text");
        infoTextObj.transform.SetParent(infoObj.transform, false);
        var infoTextRect = infoTextObj.AddComponent<RectTransform>();
        infoTextRect.anchorMin = Vector2.zero;
        infoTextRect.anchorMax = Vector2.one;
        infoTextRect.offsetMin = Vector2.zero;
        infoTextRect.offsetMax = Vector2.zero;

        var infoText = infoTextObj.AddComponent<TextMeshProUGUI>();
        infoText.text = "?";
        infoText.alignment = TextAlignmentOptions.Center;
        infoText.fontSize = 20;
        infoText.color = new Color(0.8f, 0.9f, 1f);

        resistInfoIcon = infoObj;

        var tooltipObj = new GameObject("ResistTooltip");
        tooltipObj.transform.SetParent(panel.transform, false);
        var tooltipRect = tooltipObj.AddComponent<RectTransform>();
        tooltipRect.anchorMin = new Vector2(0.45f, 0.78f);
        tooltipRect.anchorMax = new Vector2(0.98f, 0.90f);
        tooltipRect.offsetMin = Vector2.zero;
        tooltipRect.offsetMax = Vector2.zero;

        var tooltipBg = tooltipObj.AddComponent<Image>();
        tooltipBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        var tooltipTextObj = new GameObject("TooltipText");
        tooltipTextObj.transform.SetParent(tooltipObj.transform, false);
        var tooltipTextRect = tooltipTextObj.AddComponent<RectTransform>();
        tooltipTextRect.anchorMin = Vector2.zero;
        tooltipTextRect.anchorMax = Vector2.one;
        tooltipTextRect.offsetMin = new Vector2(8, 6);
        tooltipTextRect.offsetMax = new Vector2(-8, -6);

        resistTooltipText = tooltipTextObj.AddComponent<TextMeshProUGUI>();
        resistTooltipText.fontSize = 12;
        resistTooltipText.color = Color.white;
        resistTooltipText.alignment = TextAlignmentOptions.TopLeft;
        resistTooltipText.text = "Elemental resistance reduces incoming damage of that element.\nAll elements use Base Resistance.\nYour Affinity element also gets Bonus Resistance.";

        resistTooltipPanel = tooltipObj;
        resistTooltipPanel.SetActive(false);

        var infoTrigger = infoObj.AddComponent<EventTrigger>();
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener((data) => { if (resistTooltipPanel != null) resistTooltipPanel.SetActive(true); });
        infoTrigger.triggers.Add(enter);
        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener((data) => { if (resistTooltipPanel != null) resistTooltipPanel.SetActive(false); });
        infoTrigger.triggers.Add(exit);

        var statsObj = new GameObject("Stats");
        statsObj.transform.SetParent(panel.transform, false);
        var statsRect = statsObj.AddComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0, 0.37f);
        statsRect.anchorMax = new Vector2(1, 0.88f);
        statsRect.offsetMin = new Vector2(15, 0);
        statsRect.offsetMax = new Vector2(-15, 0);
        statsText = statsObj.AddComponent<TextMeshProUGUI>();
        statsText.alignment = TextAlignmentOptions.TopLeft;
        statsText.fontSize = 15;
        statsText.color = Color.white;

        var relicsTitleObj = new GameObject("RelicsTitle");
        relicsTitleObj.transform.SetParent(panel.transform, false);
        var relicsTitleRect = relicsTitleObj.AddComponent<RectTransform>();
        relicsTitleRect.anchorMin = new Vector2(0, 0.29f);
        relicsTitleRect.anchorMax = new Vector2(1, 0.36f);
        relicsTitleRect.offsetMin = new Vector2(15, 0);
        relicsTitleRect.offsetMax = new Vector2(-15, 0);
        var relicsTitleText = relicsTitleObj.AddComponent<TextMeshProUGUI>();
        relicsTitleText.text = "RELICS";
        relicsTitleText.alignment = TextAlignmentOptions.Left;
        relicsTitleText.fontSize = 20;
        relicsTitleText.color = new Color(0.6f, 0.8f, 1f);

        var relicsObj = new GameObject("RelicsList");
        relicsObj.transform.SetParent(panel.transform, false);
        var relicsRect = relicsObj.AddComponent<RectTransform>();
        relicsRect.anchorMin = new Vector2(0, 0.05f);
        relicsRect.anchorMax = new Vector2(1, 0.29f);
        relicsRect.offsetMin = new Vector2(15, 0);
        relicsRect.offsetMax = new Vector2(-15, 0);
        relicsText = relicsObj.AddComponent<TextMeshProUGUI>();
        relicsText.alignment = TextAlignmentOptions.TopLeft;
        relicsText.fontSize = 15;
        relicsText.color = new Color(0.8f, 0.8f, 0.8f);

        var hintObj = new GameObject("Hint");
        hintObj.transform.SetParent(panel.transform, false);
        var hintRect = hintObj.AddComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0, 0);
        hintRect.anchorMax = new Vector2(1, 0.05f);
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;
        var hintText = hintObj.AddComponent<TextMeshProUGUI>();
        hintText.text = "Press C to close";
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.fontSize = 14;
        hintText.color = new Color(0.5f, 0.5f, 0.5f);

        return panel;
    }

    private void ToggleStatsPanel()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            UpdateStats();
        }

        statsPanel.SetActive(isOpen);
    }

    public void UpdateStats()
    {
        if (refs == null || refs.player == null) return;

        var player = refs.player;
        var sb = new StringBuilder();

        const string divider = "<color=#444444>------------------------------</color>";

        sb.AppendLine("<color=#ffffff><b>PLAYER STATS</b></color>");
        sb.AppendLine($"Health: {player.GetHealth()} / {player.GetMaxHealth()}");
        sb.AppendLine($"Gold: {player.GetGold()}");
        sb.AppendLine($"Energy: {player.GetEnergy()} / {player.GetMaxEnergy()}");
        sb.AppendLine($"Crit Rate: {player.GetCritChance()}%");
        sb.AppendLine($"Crit Damage: x{player.GetCritDamage():F1}");
        sb.AppendLine(divider);

        var character = player.GetCharacter();
        string characterName = character != null ? character.DisplayName : "None";
        int characterDmg = player.GetCharacterDamage();
        sb.AppendLine("<color=#ffffff><b>LOADOUT</b></color>");
        sb.AppendLine($"Character: {characterName} (+{characterDmg})");
        
        var affinity = player.GetAffinity();
        
        if (player.HasElementPair())
        {
            var orbA = player.GetOrbAElement();
            var orbB = player.GetOrbBElement();
            string orbAColor = GetElementColor(orbA);
            string orbBColor = GetElementColor(orbB);
            sb.AppendLine($"Orb A: <color={orbAColor}>{orbA}</color>");
            sb.AppendLine($"Orb B: <color={orbBColor}>{orbB}</color>");
            
            var orbSystem = player.GetOrbSystem();
            if (orbSystem.IsOrbAActive || orbSystem.IsOrbBActive)
            {
                string markStatus = "";
                if (orbSystem.IsOrbAActive) markStatus += $"A:{orbSystem.OrbAMark} ";
                if (orbSystem.IsOrbBActive) markStatus += $"B:{orbSystem.OrbBMark}";
                sb.AppendLine($"<color=#ffaa55>Marks: {markStatus.Trim()}</color>");
            }
        }
        else
        {
            string affinityColor = GetElementColor(affinity);
            string affinityName = affinity != Element.None ? affinity.ToString() : "None";
            sb.AppendLine($"Affinity: <color={affinityColor}>{affinityName}</color>");
        }
        
        int affinityBonus = player.GetAffinityBonus();
        if (player.HasElementPair())
        {
            var aElem = player.GetOrbAElement();
            var bElem = player.GetOrbBElement();
            int aBonus = player.GetElementalBonus(aElem);
            int bBonus = player.GetElementalBonus(bElem);
            int totalElem = aBonus + bBonus;
            sb.AppendLine($"Damage: {player.GetTotalDamage()} (Character {characterDmg} + Elemental {totalElem})");
            string aColor = GetElementColor(aElem);
            string bColor = GetElementColor(bElem);
            sb.AppendLine($"Elemental Sources: <color={aColor}>{aElem}</color> (+{aBonus}) -> <color={bColor}>{bElem}</color> (+{bBonus})");
        }
        else
        {
            sb.AppendLine($"Damage: {player.GetTotalDamage()} (Character {characterDmg} + Elemental {affinityBonus})");
        }
        sb.AppendLine(divider);

        sb.AppendLine("<color=#ffffff><b>ELEMENTS</b></color>");
        sb.AppendLine("Bonuses:");
        sb.AppendLine($"  {FormatElementBonus(Element.Fire, player, affinity)}");
        sb.AppendLine($"  {FormatElementBonus(Element.Ice, player, affinity)}");
        sb.AppendLine($"  {FormatElementBonus(Element.Water, player, affinity)}");
        sb.AppendLine($"  {FormatElementBonus(Element.Wind, player, affinity)}");
        sb.AppendLine($"  {FormatElementBonus(Element.Rock, player, affinity)}");
        sb.AppendLine();

        sb.AppendLine("Resistances:");
        int baseRes = player.GetBaseResistance();
        int bonusRes = player.GetBonusResistance();
        sb.AppendLine($"  {FormatElementResistance(Element.Fire, player, affinity, baseRes, bonusRes)}");
        sb.AppendLine($"  {FormatElementResistance(Element.Ice, player, affinity, baseRes, bonusRes)}");
        sb.AppendLine($"  {FormatElementResistance(Element.Water, player, affinity, baseRes, bonusRes)}");
        sb.AppendLine($"  {FormatElementResistance(Element.Wind, player, affinity, baseRes, bonusRes)}");
        sb.AppendLine($"  {FormatElementResistance(Element.Rock, player, affinity, baseRes, bonusRes)}");

        statsText.text = sb.ToString();

        var relics = player.GetRelics();
        if (relics.Count == 0)
        {
            relicsText.text = "<color=#666666>No relics collected</color>";
        }
        else
        {
            var relicSb = new StringBuilder();
            foreach (var relic in relics)
            {
                relicSb.AppendLine($"• {relic.DisplayName} ({relic.StatAffected} +{relic.Amount})");
            }
            relicsText.text = relicSb.ToString();
        }
    }

    public bool IsOpen() => isOpen;

    private string GetElementColor(Element element)
    {
        switch (element)
        {
            case Element.Fire: return "#ff4422";
            case Element.Ice: return "#44ddff";
            case Element.Water: return "#4488ff";
            case Element.Wind: return "#88ff88";
            case Element.Rock: return "#aa8866";
            default: return "#888888";
        }
    }

    private string FormatElementBonus(Element element, Player player, Element affinity)
    {
        int bonus = player.GetElementalBonus(element);
        string color = GetElementColor(element);
        string marker = "";
        
        if (player.HasElementPair())
        {
            bool isA = element == player.GetOrbAElement();
            bool isB = element == player.GetOrbBElement();
            if (isA && isB)
            {
                marker = " <-A,B"; // edge case if both are same (shouldn't happen with distinct pairs)
            }
            else if (isA)
            {
                marker = " <-A";
            }
            else if (isB)
            {
                marker = " <-B";
            }
        }
        else
        {
            // Legacy single-affinity indication
            if (element == affinity)
            {
                marker = " <-";
            }
        }
        
        return $"<color={color}>{element}:</color> +{bonus}{marker}";
    }

    private string FormatElementResistance(Element element, Player player, Element affinity, int baseRes, int bonusRes)
    {
        bool getsBonus;
        string marker = "";
        if (player.HasElementPair())
        {
            bool isA = element == player.GetOrbAElement();
            bool isB = element == player.GetOrbBElement();
            getsBonus = isA || isB;
            if (isA && isB)
                marker = " <-A,B";
            else if (isA)
                marker = " <-A";
            else if (isB)
                marker = " <-B";
        }
        else
        {
            getsBonus = (element == affinity && affinity != Element.None);
            if (getsBonus) marker = " <-";
        }

        int total = baseRes + (getsBonus ? bonusRes : 0);
        string color = GetElementColor(element);
        string bonusMarker = getsBonus ? $" (+{bonusRes}%)" : "";
        return $"<color={color}>{element}:</color> {total}%{bonusMarker}{marker}";
    }

    public static string GetSkillDescription(string skillName)
    {
        if (string.IsNullOrEmpty(skillName)) return "";
        
        switch (skillName.ToLower())
        {
            case "slash": return "Basic sword strike (100% damage)";
            case "riposte": return "Defensive counter (80% damage)";
            case "bladestorm": return "Whirlwind attack (150% damage + 50% to all)";
            case "bolt": return "Quick magic projectile (90% damage)";
            case "ray": return "Focused beam (120% damage)";
            case "meteor": return "Devastating impact (200% damage)";
            case "aimedshot": return "Precise shot (130% damage)";
            case "tripleshot": return "Hit all enemies (50% damage each)";
            case "doubleup": return "Two arrows, one target (200% damage)";
            case "dirtystab": return "Underhanded strike (110% damage)";
            case "cheapshot": return "Quick jab (70% damage)";
            case "ambush": return "Strike from shadows (180% damage)";
            case "shock": return "Electric jolt (90% damage)";
            case "judgement": return "Divine strike (140% damage)";
            case "holynova": return "Holy explosion (120% damage to all)";
            default: return "Unknown skill";
        }
    }
}
