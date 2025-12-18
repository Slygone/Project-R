using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PotionUI : MonoBehaviour
{
    private GameObject potionPanel;
    private GameObject potionContainer;
    private TextMeshProUGUI titleText;
    private GameObject tooltipPanel;
    private TextMeshProUGUI tooltipText;
    private List<GameObject> potionSlots = new List<GameObject>();
    private bool isActive = false;
    private Player player;
    private CombatManager combatManager;
    private CombatUI combatUI;
    private bool isInCombat = false;

    void Awake()
    {
        SetupUI();
    }

    void Start()
    {
        var refs = FindFirstObjectByType<Referencer>();
        if (refs != null)
        {
            player = refs.player;
            combatManager = refs.combatManager;
            combatUI = refs.combatUI;
        }
    }

    private void SetupUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[PotionUI] Canvas not found");
            return;
        }

        potionPanel = CreatePotionPanel(canvas.transform);
        potionPanel.SetActive(false);
    }

    private GameObject CreatePotionPanel(Transform parent)
    {
        var panel = new GameObject("PotionPanel");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.72f, 0.78f);
        rect.anchorMax = new Vector2(0.99f, 0.99f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.15f, 0.2f, 0.9f);

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.75f);
        titleRect.anchorMax = new Vector2(1, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "POTIONS (0/4) [C]";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 16;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.6f, 0.9f, 0.6f);

        potionContainer = new GameObject("PotionContainer");
        potionContainer.transform.SetParent(panel.transform, false);
        var containerRect = potionContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.02f, 0.05f);
        containerRect.anchorMax = new Vector2(0.98f, 0.75f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        var layout = potionContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 5;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.padding = new RectOffset(5, 5, 5, 5);

        CreateTooltip(parent);

        return panel;
    }

    private void CreateTooltip(Transform parent)
    {
        tooltipPanel = new GameObject("PotionTooltip");
        tooltipPanel.transform.SetParent(parent, false);

        var rect = tooltipPanel.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(220, 80);
        rect.pivot = new Vector2(1f, 0f);

        var bg = tooltipPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(tooltipPanel.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8, 8);
        textRect.offsetMax = new Vector2(-8, -8);
        tooltipText = textObj.AddComponent<TextMeshProUGUI>();
        tooltipText.text = "";
        tooltipText.alignment = TextAlignmentOptions.TopLeft;
        tooltipText.fontSize = 12;
        tooltipText.color = Color.white;

        tooltipPanel.SetActive(false);
    }

    void Update()
    {
        if (player == null)
        {
            var refs = FindFirstObjectByType<Referencer>();
            if (refs != null) player = refs.player;
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (isActive)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        if (isActive && Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
    }

    public void Show()
    {
        if (player == null) return;

        isInCombat = combatManager != null && combatManager.IsInCombat();
        RefreshPotionDisplay();
        potionPanel.SetActive(true);
        isActive = true;
    }

    public void Hide()
    {
        HideTooltip();
        potionPanel.SetActive(false);
        isActive = false;
    }

    private void RefreshPotionDisplay()
    {
        foreach (var slot in potionSlots)
        {
            if (slot != null) Destroy(slot);
        }
        potionSlots.Clear();

        var potions = player.GetPotions();
        titleText.text = $"POTIONS ({potions.Count}/{player.GetMaxPotions()})";

        for (int i = 0; i < player.GetMaxPotions(); i++)
        {
            if (i < potions.Count)
            {
                CreatePotionSlot(i, potions[i]);
            }
            else
            {
                CreateEmptySlot(i);
            }
        }
    }

    private void CreatePotionSlot(int index, PotionData potion)
    {
        var slotObj = new GameObject($"PotionSlot_{index}");
        slotObj.transform.SetParent(potionContainer.transform, false);
        potionSlots.Add(slotObj);

        var slotImage = slotObj.AddComponent<Image>();
        slotImage.color = GetPotionColor(potion);

        var btn = slotObj.AddComponent<Button>();
        btn.targetGraphic = slotImage;

        int capturedIndex = index;
        PotionData capturedPotion = potion;
        btn.onClick.AddListener(() => OnPotionSlotClicked(capturedIndex));

        var eventTrigger = slotObj.AddComponent<EventTrigger>();
        var pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => ShowTooltip(capturedPotion));
        eventTrigger.triggers.Add(pointerEnter);
        
        var pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => HideTooltip());
        eventTrigger.triggers.Add(pointerExit);

        var numObj = new GameObject("Number");
        numObj.transform.SetParent(slotObj.transform, false);
        var numText = numObj.AddComponent<TextMeshProUGUI>();
        numText.text = $"{index + 1}";
        numText.alignment = TextAlignmentOptions.Center;
        numText.fontSize = 18;
        numText.fontStyle = FontStyles.Bold;
        numText.color = Color.white;
        var numRect = numObj.GetComponent<RectTransform>();
        numRect.anchorMin = Vector2.zero;
        numRect.anchorMax = Vector2.one;
        numRect.offsetMin = Vector2.zero;
        numRect.offsetMax = Vector2.zero;
    }

    private void CreateEmptySlot(int index)
    {
        var slotObj = new GameObject($"EmptySlot_{index}");
        slotObj.transform.SetParent(potionContainer.transform, false);
        potionSlots.Add(slotObj);

        var slotImage = slotObj.AddComponent<Image>();
        slotImage.color = new Color(0.15f, 0.15f, 0.2f, 0.5f);

        var numObj = new GameObject("Number");
        numObj.transform.SetParent(slotObj.transform, false);
        var numText = numObj.AddComponent<TextMeshProUGUI>();
        numText.text = $"{index + 1}";
        numText.alignment = TextAlignmentOptions.Center;
        numText.fontSize = 16;
        numText.color = new Color(0.4f, 0.4f, 0.4f);
        var numRect = numObj.GetComponent<RectTransform>();
        numRect.anchorMin = Vector2.zero;
        numRect.anchorMax = Vector2.one;
        numRect.offsetMin = Vector2.zero;
        numRect.offsetMax = Vector2.zero;
    }

    private void ShowTooltip(PotionData potion)
    {
        if (tooltipPanel == null) return;
        
        string desc = GetPotionTooltipText(potion);
        tooltipText.text = desc;
        
        tooltipPanel.transform.position = Input.mousePosition + new Vector3(-10, 10, 0);
        tooltipPanel.SetActive(true);
    }

    private void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    private string GetPotionTooltipText(PotionData potion)
    {
        var stat = potion.StatAffected.ToLower().Trim();
        string effect = "";
        string usage = "";
        
        if (stat.Contains("health"))
        {
            effect = $"<color=#90EE90>Restores {potion.Amount} HP</color>";
            usage = "Use: Anytime";
        }
        else if (stat.Contains("elemental"))
        {
            effect = $"<color=#DDA0DD>Deals {potion.Amount} damage</color>\n<color=#888>(Random element)</color>";
            usage = "<color=#FF6666>Use: Combat only</color>";
        }
        else if (stat.Contains("crit rate"))
        {
            effect = $"<color=#FFD700>+{potion.Amount}% Crit Chance</color>";
            usage = "Lasts: This world";
        }
        else if (stat.Contains("crit damage"))
        {
            effect = $"<color=#FF69B4>+{potion.Amount}% Crit Damage</color>";
            usage = "Lasts: This world";
        }
        
        return $"<b>{potion.DisplayName}</b>\n{effect}\n<size=10>{usage}</size>";
    }

    private Color GetPotionColor(PotionData potion)
    {
        var stat = potion.StatAffected.ToLower().Trim();
        if (stat.Contains("health"))
            return new Color(0.4f, 0.2f, 0.2f);
        if (stat.Contains("elemental"))
            return new Color(0.3f, 0.25f, 0.4f);
        if (stat.Contains("crit rate"))
            return new Color(0.4f, 0.35f, 0.2f);
        if (stat.Contains("crit damage"))
            return new Color(0.35f, 0.2f, 0.35f);
        return new Color(0.25f, 0.3f, 0.25f);
    }

    private string GetPotionEffectText(PotionData potion)
    {
        var stat = potion.StatAffected.ToLower().Trim();
        if (stat.Contains("health"))
            return $"Heal {potion.Amount} HP";
        if (stat.Contains("elemental"))
            return $"Deal {potion.Amount} random elemental damage";
        if (stat.Contains("crit rate"))
            return $"+{potion.Amount}% Crit Chance (this world)";
        if (stat.Contains("crit damage"))
            return $"+{potion.Amount}% Crit Damage (this world)";
        return $"+{potion.Amount} {potion.StatAffected}";
    }

    private void OnPotionSlotClicked(int index)
    {
        var potions = player.GetPotions();
        if (index >= potions.Count) return;

        var potion = potions[index];
        var stat = potion.StatAffected.ToLower().Trim();

        if (stat.Contains("elemental"))
        {
            if (!isInCombat)
            {
                Debug.Log("[PotionUI] Damage potions can only be used in combat");
                return;
            }

            var enemies = combatManager.GetEnemies();
            var aliveEnemies = enemies.FindAll(e => e.IsAlive());
            if (aliveEnemies.Count == 0)
            {
                Debug.Log("[PotionUI] No enemies alive to target");
                return;
            }

            var target = aliveEnemies[0];
            if (player.UsePotion(index, target))
            {
                if (combatUI != null)
                {
                    combatUI.UpdateEnemyHealth(target);
                }
                RefreshPotionDisplay();
            }
        }
        else
        {
            if (player.UsePotion(index, null))
            {
                RefreshPotionDisplay();
            }
        }
    }

    public bool IsActive() => isActive;
}
