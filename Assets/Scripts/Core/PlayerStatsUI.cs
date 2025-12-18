using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStatsUI : MonoBehaviour
{
    private GameObject statsPanel;
    private TextMeshProUGUI statsText;
    private TextMeshProUGUI relicsText;
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

        var statsObj = new GameObject("Stats");
        statsObj.transform.SetParent(panel.transform, false);
        var statsRect = statsObj.AddComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0, 0.35f);
        statsRect.anchorMax = new Vector2(1, 0.88f);
        statsRect.offsetMin = new Vector2(15, 0);
        statsRect.offsetMax = new Vector2(-15, 0);
        statsText = statsObj.AddComponent<TextMeshProUGUI>();
        statsText.alignment = TextAlignmentOptions.TopLeft;
        statsText.fontSize = 16;
        statsText.color = Color.white;

        var relicsTitleObj = new GameObject("RelicsTitle");
        relicsTitleObj.transform.SetParent(panel.transform, false);
        var relicsTitleRect = relicsTitleObj.AddComponent<RectTransform>();
        relicsTitleRect.anchorMin = new Vector2(0, 0.28f);
        relicsTitleRect.anchorMax = new Vector2(1, 0.35f);
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
        relicsRect.anchorMax = new Vector2(1, 0.28f);
        relicsRect.offsetMin = new Vector2(15, 0);
        relicsRect.offsetMax = new Vector2(-15, 0);
        relicsText = relicsObj.AddComponent<TextMeshProUGUI>();
        relicsText.alignment = TextAlignmentOptions.TopLeft;
        relicsText.fontSize = 16;
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

        sb.AppendLine($"<color=#ff5555>Health:</color> {player.GetHealth()} / {player.GetMaxHealth()}");
        sb.AppendLine($"<color=#ffdd55>Gold:</color> {player.GetGold()}");
        sb.AppendLine($"<color=#55aaff>Energy:</color> {player.GetEnergy()} / {player.GetMaxEnergy()}");
        sb.AppendLine($"<color=#ff55ff>Crit Chance:</color> {player.GetCritChance()}%");
        sb.AppendLine($"<color=#ff55ff>Crit Damage:</color> x{player.GetCritDamage():F1}");
        sb.AppendLine();
        
        var weapon = player.GetWeapon();
        string weaponName = weapon != null ? weapon.DisplayName : "None";
        int weaponDmg = player.GetWeaponDamage();
        sb.AppendLine($"<color=#ffaa22>Weapon:</color> {weaponName} (+{weaponDmg})");
        
        var affinity = player.GetAffinity();
        string affinityColor = GetElementColor(affinity);
        string affinityName = affinity != Element.None ? affinity.ToString() : "None";
        sb.AppendLine($"<color=#aaaaaa>Affinity:</color> <color={affinityColor}>{affinityName}</color>");
        sb.AppendLine();
        sb.AppendLine($"<color=#aaaaaa>Total Damage:</color> {player.GetTotalDamage()}");
        sb.AppendLine($"  Base: {player.GetBaseDamage()} + Weapon: {weaponDmg} + Affinity: {player.GetAffinityBonus()}");
        sb.AppendLine();
        
        if (weapon != null)
        {
            sb.AppendLine("<color=#7799ff>Skills:</color>");
            sb.AppendLine($"  • {weapon.Skill1}: {GetSkillDescription(weapon.Skill1)}");
            sb.AppendLine($"  • {weapon.Skill2}: {GetSkillDescription(weapon.Skill2)}");
            sb.AppendLine($"  • {weapon.Skill3}: {GetSkillDescription(weapon.Skill3)}");
            sb.AppendLine();
        }
        
        sb.AppendLine("<color=#aaaaaa>Elemental Bonuses:</color>");
        sb.AppendLine($"  {FormatElementBonus(Element.Fire, player, affinity)}");
        sb.AppendLine($"  {FormatElementBonus(Element.Ice, player, affinity)}");
        sb.AppendLine($"  {FormatElementBonus(Element.Water, player, affinity)}");
        sb.AppendLine($"  {FormatElementBonus(Element.Wind, player, affinity)}");
        sb.AppendLine($"  {FormatElementBonus(Element.Rock, player, affinity)}");

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
        string activeMarker = element == affinity ? " <-" : "";
        return $"<color={color}>{element}:</color> +{bonus}{activeMarker}";
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
