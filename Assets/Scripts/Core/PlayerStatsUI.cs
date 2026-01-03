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
        statsRect.anchorMin = new Vector2(0, 0.42f);
        statsRect.anchorMax = new Vector2(1, 0.88f);
        statsRect.offsetMin = new Vector2(15, 5);
        statsRect.offsetMax = new Vector2(-15, 0);
        statsText = statsObj.AddComponent<TextMeshProUGUI>();
        statsText.alignment = TextAlignmentOptions.TopLeft;
        statsText.fontSize = 14;
        statsText.color = Color.white;
        statsText.overflowMode = TextOverflowModes.Truncate;

        var relicsTitleObj = new GameObject("RelicsTitle");
        relicsTitleObj.transform.SetParent(panel.transform, false);
        var relicsTitleRect = relicsTitleObj.AddComponent<RectTransform>();
        relicsTitleRect.anchorMin = new Vector2(0, 0.34f);
        relicsTitleRect.anchorMax = new Vector2(1, 0.40f);
        relicsTitleRect.offsetMin = new Vector2(15, 0);
        relicsTitleRect.offsetMax = new Vector2(-15, 0);
        var relicsTitleText = relicsTitleObj.AddComponent<TextMeshProUGUI>();
        relicsTitleText.text = "RELICS";
        relicsTitleText.alignment = TextAlignmentOptions.Left;
        relicsTitleText.fontSize = 18;
        relicsTitleText.fontStyle = FontStyles.Bold;
        relicsTitleText.color = new Color(0.6f, 0.8f, 1f);

        var relicsObj = new GameObject("RelicsList");
        relicsObj.transform.SetParent(panel.transform, false);
        var relicsRect = relicsObj.AddComponent<RectTransform>();
        relicsRect.anchorMin = new Vector2(0, 0.06f);
        relicsRect.anchorMax = new Vector2(1, 0.34f);
        relicsRect.offsetMin = new Vector2(15, 0);
        relicsRect.offsetMax = new Vector2(-15, 0);
        relicsText = relicsObj.AddComponent<TextMeshProUGUI>();
        relicsText.alignment = TextAlignmentOptions.TopLeft;
        relicsText.fontSize = 14;
        relicsText.color = new Color(0.8f, 0.8f, 0.8f);
        relicsText.overflowMode = TextOverflowModes.Ellipsis;

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
        int shield = player.GetShield();
        if (shield > 0)
        {
            sb.AppendLine($"<color=#44aaff>Shield: {shield}</color>");
        }
        sb.AppendLine($"Gold: {player.GetGold()}");
        sb.AppendLine($"Energy: {player.GetEnergy()} / {player.GetMaxEnergy()}");
        sb.AppendLine($"Crit Rate: {player.GetCritChance()}%");
        sb.AppendLine($"Crit Damage: x{player.GetCritDamage():F1}");
        sb.AppendLine(divider);

        var character = player.GetCharacter();
        string characterName = character != null ? character.DisplayName : "None";
        int characterDmg = player.GetCharacterDamage();
        int totalBaseDamage = player.GetTotalDamage();
        int totalMin = Mathf.RoundToInt(totalBaseDamage * CombatConfig.VARIANCE_MIN);
        int totalMax = Mathf.RoundToInt(totalBaseDamage * CombatConfig.VARIANCE_MAX);
        sb.AppendLine("<color=#ffffff><b>LOADOUT</b></color>");
        sb.AppendLine($"Character: {characterName}");
        
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
        
        // Health & Shield permanent stats
        sb.AppendLine("<color=#ffffff><b>HEALTH</b></color>");
        sb.AppendLine($"<color=#ff3333>Health:</color> {player.GetHealth()}/{player.GetMaxHealth()}");
        sb.AppendLine($"<color=#4488ff>Shield:</color> {player.GetShield()}");
        
        int affinityBonus = player.GetAffinityBonus();
        string dmgRangeLabel = character != null && !string.IsNullOrEmpty(character.DamageRangeLabel)
            ? character.DamageRangeLabel
            : $"{characterDmg}-{characterDmg}";
        int baseMin = characterDmg, baseMax = characterDmg;
        var parts = dmgRangeLabel.Split('-');
        if (parts.Length == 2)
        {
            int.TryParse(parts[0], out baseMin);
            int.TryParse(parts[1], out baseMax);
        }

        // Active elemental bonuses depend on player's choice (pair or single affinity)
        int totalBonus = 0;
        sb.AppendLine($"Base Damage: {baseMin}-{baseMax}");

        if (player.HasElementPair())
        {
            var elemA = player.GetOrbAElement();
            var elemB = player.GetOrbBElement();
            int bonusA = player.GetElementalBonus(elemA);
            int bonusB = player.GetElementalBonus(elemB);
            totalBonus = bonusA + bonusB;
            string colorA = GetElementColor(elemA);
            string colorB = GetElementColor(elemB);
            sb.AppendLine($"Bonus <color={colorA}>{elemA}</color> Damage: {bonusA}");
            sb.AppendLine($"Bonus <color={colorB}>{elemB}</color> Damage: {bonusB}");
        }
        else
        {
            var aff = player.GetAffinity();
            int affBonus = aff != Element.None ? player.GetElementalBonus(aff) : 0;
            totalBonus = affBonus;
            string affColor = GetElementColor(aff);
            string affLabel = aff != Element.None ? $"<color={affColor}>{aff}</color>" : "None";
            sb.AppendLine($"Bonus {affLabel} Damage: {affBonus}");
        }

        int totalMinRange = baseMin + totalBonus;
        int totalMaxRange = baseMax + totalBonus;
        sb.AppendLine($"Total Damage: {totalMinRange}-{totalMaxRange}");
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
        
        // Try JSON skill lookup first
        string skillId = $"skill_{skillName.ToLower()}";
        var skillDef = GameDataLoader.GetSkill(skillId);
        if (skillDef != null)
        {
            float damagePercent = 100f;
            if (skillDef.executions != null)
            {
                foreach (var exec in skillDef.executions)
                {
                    if (exec.effectId == "eff_deal_damage" && exec.@params?.scaling != null)
                    {
                        damagePercent = exec.@params.scaling.multiplier * 100f;
                        break;
                    }
                }
            }
            string desc = skillDef.description ?? "";
            return $"{desc}{(string.IsNullOrEmpty(desc) ? "" : "\n")}Damage: {damagePercent}%";
        }
        
        // Try character data as secondary source
        var refs = Object.FindFirstObjectByType<Referencer>();
        var player = refs != null ? refs.player : null;
        var character = player != null ? player.GetCharacter() : null;
        if (character != null)
        {
            string s1 = character.Skill1?.ToLower();
            string s2 = character.Skill2?.ToLower();
            string s3 = character.Skill3?.ToLower();
            string key = skillName.ToLower();
            if (key == s1)
            {
                string effect = string.IsNullOrEmpty(character.Skill1Effect) ? "" : character.Skill1Effect;
                float pct = character.Skill1DamagePercent;
                return $"{effect}{(string.IsNullOrEmpty(effect) ? "" : "\n")}Damage: {pct}%";
            }
            if (key == s2)
            {
                string effect = string.IsNullOrEmpty(character.Skill2Effect) ? "" : character.Skill2Effect;
                float pct = character.Skill2DamagePercent;
                return $"{effect}{(string.IsNullOrEmpty(effect) ? "" : "\n")}Damage: {pct}%";
            }
            if (key == s3)
            {
                string effect = string.IsNullOrEmpty(character.Skill3Effect) ? "" : character.Skill3Effect;
                float pct = character.Skill3DamagePercent;
                return $"{effect}{(string.IsNullOrEmpty(effect) ? "" : "\n")}Damage: {pct}%";
            }
        }
        
        return "Unknown skill";
    }
}
