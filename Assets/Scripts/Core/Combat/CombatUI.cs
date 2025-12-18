using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatUI : MonoBehaviour
{
    private GameObject combatPanel;
    private GameObject lootPanel;
    private GameObject playerHealthBar;
    private TextMeshProUGUI playerHealthText;
    private Image playerHealthFill;
    private Button attackButton;
    private Button skill1Button;
    private Button skill2Button;
    private Button skill3Button;
    private TextMeshProUGUI skill1Text;
    private TextMeshProUGUI skill2Text;
    private TextMeshProUGUI skill3Text;
    private TooltipTrigger skill1Tooltip;
    private TooltipTrigger skill2Tooltip;
    private TooltipTrigger skill3Tooltip;
    private Transform enemyContainer;
    private Dictionary<CombatEnemy, EnemyUISlot> enemySlots = new Dictionary<CombatEnemy, EnemyUISlot>();
    private CombatManager combatManager;
    
    private TextMeshProUGUI lootGoldText;
    private TextMeshProUGUI lootRelicText;
    private Button collectLootButton;
    private int pendingGold;
    private RelicData pendingRelic;
    private Player pendingPlayer;
    private CombatNode pendingNode;

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
            Debug.LogError("[CombatUI] Canvas not found");
            return;
        }

        combatPanel = CreateCombatPanel(canvas.transform);
        combatPanel.SetActive(false);
        
        lootPanel = CreateLootPanel(canvas.transform);
        lootPanel.SetActive(false);
    }

    private GameObject CreateCombatPanel(Transform parent)
    {
        var panel = new GameObject("CombatPanel");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.85f);

        CreatePlayerHealthBar(panel.transform);
        CreateEnemyContainer(panel.transform);
        CreateAttackButton(panel.transform);

        return panel;
    }

    private void CreatePlayerHealthBar(Transform parent)
    {
        var container = new GameObject("PlayerHealth");
        container.transform.SetParent(parent, false);

        var rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0.1f);
        rect.anchorMax = new Vector2(0.4f, 0.15f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bgImage = container.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(container.transform, false);

        var fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);

        playerHealthFill = fill.AddComponent<Image>();
        playerHealthFill.color = Color.green;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(container.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        playerHealthText = textObj.AddComponent<TextMeshProUGUI>();
        playerHealthText.alignment = TextAlignmentOptions.Center;
        playerHealthText.fontSize = 18;
        playerHealthText.color = Color.white;

        playerHealthBar = container;
    }

    private void CreateEnemyContainer(Transform parent)
    {
        var container = new GameObject("EnemyContainer");
        container.transform.SetParent(parent, false);

        var rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0.4f);
        rect.anchorMax = new Vector2(0.9f, 0.85f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        enemyContainer = container.transform;
    }

    private void CreateAttackButton(Transform parent)
    {
        var container = new GameObject("ActionButtons");
        container.transform.SetParent(parent, false);
        var containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.45f, 0.05f);
        containerRect.anchorMax = new Vector2(0.95f, 0.22f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        var layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        attackButton = CreateActionButton(container.transform, "Attack", "ATTACK", new Color(0.8f, 0.2f, 0.2f, 1f), OnAttackClicked, out _, out _);
        skill1Button = CreateActionButton(container.transform, "Skill1", "Skill 1", new Color(0.2f, 0.5f, 0.7f, 1f), OnSkill1Clicked, out skill1Text, out skill1Tooltip);
        skill2Button = CreateActionButton(container.transform, "Skill2", "Skill 2", new Color(0.2f, 0.5f, 0.7f, 1f), OnSkill2Clicked, out skill2Text, out skill2Tooltip);
        skill3Button = CreateActionButton(container.transform, "Skill3", "Skill 3", new Color(0.2f, 0.5f, 0.7f, 1f), OnSkill3Clicked, out skill3Text, out skill3Tooltip);
    }

    private Button CreateActionButton(Transform parent, string name, string label, Color color, UnityEngine.Events.UnityAction onClick, out TextMeshProUGUI textComponent, out TooltipTrigger tooltip)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = color;

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.onClick.AddListener(onClick);

        tooltip = btnObj.AddComponent<TooltipTrigger>();

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = label;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.fontSize = 16;
        textComponent.color = Color.white;

        return btn;
    }

    private void OnAttackClicked()
    {
        if (combatManager == null)
        {
            combatManager = FindFirstObjectByType<CombatManager>();
        }
        
        if (combatManager != null)
        {
            combatManager.OnPlayerAttack();
        }
    }

    private void OnSkill1Clicked()
    {
        if (combatManager == null)
        {
            combatManager = FindFirstObjectByType<CombatManager>();
        }
        
        if (combatManager != null)
        {
            combatManager.OnPlayerSkill(1);
        }
    }

    private void OnSkill2Clicked()
    {
        if (combatManager == null)
        {
            combatManager = FindFirstObjectByType<CombatManager>();
        }
        
        if (combatManager != null)
        {
            combatManager.OnPlayerSkill(2);
        }
    }

    private void OnSkill3Clicked()
    {
        if (combatManager == null)
        {
            combatManager = FindFirstObjectByType<CombatManager>();
        }
        
        if (combatManager != null)
        {
            combatManager.OnPlayerSkill(3);
        }
    }

    public void ShowCombat(List<CombatEnemy> enemies, Player player)
    {
        if (combatPanel == null)
        {
            Debug.LogError("[CombatUI] combatPanel is null - SetupUI may have failed");
            SetupUI();
        }

        ClearEnemySlots();

        foreach (var enemy in enemies)
        {
            CreateEnemySlot(enemy);
        }

        UpdateSkillButtons(player);
        UpdatePlayerHealth(player);
        combatPanel.SetActive(true);
        Debug.Log($"[CombatUI] Combat panel shown with {enemies.Count} enemies");
        SetPlayerTurn(true);
    }

    private void UpdateSkillButtons(Player player)
    {
        var weapon = player.GetWeapon();
        if (weapon != null)
        {
            if (skill1Text != null) skill1Text.text = weapon.Skill1;
            if (skill2Text != null) skill2Text.text = weapon.Skill2;
            if (skill3Text != null) skill3Text.text = weapon.Skill3;
            
            if (skill1Tooltip != null) skill1Tooltip.SetTooltip($"<b>{weapon.Skill1}</b>\n{PlayerStatsUI.GetSkillDescription(weapon.Skill1)}");
            if (skill2Tooltip != null) skill2Tooltip.SetTooltip($"<b>{weapon.Skill2}</b>\n{PlayerStatsUI.GetSkillDescription(weapon.Skill2)}");
            if (skill3Tooltip != null) skill3Tooltip.SetTooltip($"<b>{weapon.Skill3}</b>\n{PlayerStatsUI.GetSkillDescription(weapon.Skill3)}");
        }
        else
        {
            if (skill1Text != null) skill1Text.text = "---";
            if (skill2Text != null) skill2Text.text = "---";
            if (skill3Text != null) skill3Text.text = "---";
            
            if (skill1Tooltip != null) skill1Tooltip.SetTooltip("");
            if (skill2Tooltip != null) skill2Tooltip.SetTooltip("");
            if (skill3Tooltip != null) skill3Tooltip.SetTooltip("");
        }
    }

    public void HideCombat()
    {
        combatPanel.SetActive(false);
        ClearEnemySlots();
    }

    private void ClearEnemySlots()
    {
        foreach (var slot in enemySlots.Values)
        {
            if (slot.Root != null)
                Destroy(slot.Root);
        }
        enemySlots.Clear();
    }

    private void CreateEnemySlot(CombatEnemy enemy)
    {
        var slot = new GameObject($"Enemy_{enemy.Name}");
        slot.transform.SetParent(enemyContainer, false);

        var rect = slot.AddComponent<RectTransform>();

        var bg = slot.AddComponent<Image>();
        bg.color = new Color(0.3f, 0.1f, 0.1f, 1f);

        var layout = slot.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(slot.transform, false);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = enemy.Name;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = 20;
        nameText.color = Color.white;
        var nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.preferredHeight = 30;

        var healthBarBg = new GameObject("HealthBarBg");
        healthBarBg.transform.SetParent(slot.transform, false);
        var healthBgRect = healthBarBg.AddComponent<RectTransform>();
        var healthBgImage = healthBarBg.AddComponent<Image>();
        healthBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var healthBgLayout = healthBarBg.AddComponent<LayoutElement>();
        healthBgLayout.preferredHeight = 20;

        var healthFill = new GameObject("Fill");
        healthFill.transform.SetParent(healthBarBg.transform, false);
        var fillRect = healthFill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);
        var fillImage = healthFill.AddComponent<Image>();
        fillImage.color = Color.red;

        var healthTextObj = new GameObject("HealthText");
        healthTextObj.transform.SetParent(slot.transform, false);
        var healthText = healthTextObj.AddComponent<TextMeshProUGUI>();
        healthText.text = $"{enemy.Health}/{enemy.MaxHealth}";
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.fontSize = 16;
        healthText.color = Color.white;
        var healthTextLayout = healthTextObj.AddComponent<LayoutElement>();
        healthTextLayout.preferredHeight = 25;

        var damageObj = new GameObject("Damage");
        damageObj.transform.SetParent(slot.transform, false);
        var damageText = damageObj.AddComponent<TextMeshProUGUI>();
        damageText.text = $"DMG: {enemy.Damage}";
        damageText.alignment = TextAlignmentOptions.Center;
        damageText.fontSize = 14;
        damageText.color = new Color(1f, 0.6f, 0.6f, 1f);
        var damageLayout = damageObj.AddComponent<LayoutElement>();
        damageLayout.preferredHeight = 20;

        enemySlots[enemy] = new EnemyUISlot
        {
            Root = slot,
            HealthFill = fillImage,
            HealthText = healthText,
            NameText = nameText
        };
    }

    public void UpdateEnemyHealth(CombatEnemy enemy)
    {
        if (!enemySlots.ContainsKey(enemy)) return;

        var slot = enemySlots[enemy];
        float healthPercent = (float)enemy.Health / enemy.MaxHealth;
        slot.HealthFill.fillAmount = healthPercent;
        slot.HealthText.text = $"{enemy.Health}/{enemy.MaxHealth}";

        if (!enemy.IsAlive())
        {
            slot.Root.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            slot.NameText.color = Color.gray;
        }
    }

    public void UpdatePlayerHealth(Player player)
    {
        float healthPercent = (float)player.GetHealth() / player.GetMaxHealth();
        playerHealthFill.fillAmount = healthPercent;
        playerHealthText.text = $"HP: {player.GetHealth()}/{player.GetMaxHealth()}";

        if (healthPercent > 0.5f)
            playerHealthFill.color = Color.green;
        else if (healthPercent > 0.25f)
            playerHealthFill.color = Color.yellow;
        else
            playerHealthFill.color = Color.red;
    }

    public void ShowDamageToEnemy(CombatEnemy enemy, int damage)
    {
        Debug.Log($"[CombatUI] Enemy {enemy.Name} took {damage} damage");
    }

    public void ShowDamageToPlayer(int damage)
    {
        Debug.Log($"[CombatUI] Player took {damage} damage");
    }

    public void SetPlayerTurn(bool isPlayerTurn)
    {
        if (attackButton != null)
            attackButton.interactable = isPlayerTurn;
        if (skill1Button != null)
            skill1Button.interactable = isPlayerTurn;
        if (skill2Button != null)
            skill2Button.interactable = isPlayerTurn;
        if (skill3Button != null)
            skill3Button.interactable = isPlayerTurn;
    }

    private class EnemyUISlot
    {
        public GameObject Root;
        public Image HealthFill;
        public TextMeshProUGUI HealthText;
        public TextMeshProUGUI NameText;
    }

    private GameObject CreateLootPanel(Transform parent)
    {
        var panel = new GameObject("LootPanel");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.3f, 0.3f);
        rect.anchorMax = new Vector2(0.7f, 0.7f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.75f);
        titleRect.anchorMax = new Vector2(1, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "VICTORY!";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 32;
        titleText.color = new Color(1f, 0.85f, 0.2f);

        var goldObj = new GameObject("GoldText");
        goldObj.transform.SetParent(panel.transform, false);
        var goldRect = goldObj.AddComponent<RectTransform>();
        goldRect.anchorMin = new Vector2(0.1f, 0.5f);
        goldRect.anchorMax = new Vector2(0.9f, 0.65f);
        goldRect.offsetMin = Vector2.zero;
        goldRect.offsetMax = Vector2.zero;
        lootGoldText = goldObj.AddComponent<TextMeshProUGUI>();
        lootGoldText.alignment = TextAlignmentOptions.Center;
        lootGoldText.fontSize = 24;
        lootGoldText.color = new Color(1f, 0.85f, 0.2f);

        var relicObj = new GameObject("RelicText");
        relicObj.transform.SetParent(panel.transform, false);
        var relicRect = relicObj.AddComponent<RectTransform>();
        relicRect.anchorMin = new Vector2(0.1f, 0.3f);
        relicRect.anchorMax = new Vector2(0.9f, 0.5f);
        relicRect.offsetMin = Vector2.zero;
        relicRect.offsetMax = Vector2.zero;
        lootRelicText = relicObj.AddComponent<TextMeshProUGUI>();
        lootRelicText.alignment = TextAlignmentOptions.Center;
        lootRelicText.fontSize = 20;
        lootRelicText.color = new Color(0.6f, 0.8f, 1f);

        var btnObj = new GameObject("CollectButton");
        btnObj.transform.SetParent(panel.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.25f, 0.08f);
        btnRect.anchorMax = new Vector2(0.75f, 0.22f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;
        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);
        collectLootButton = btnObj.AddComponent<Button>();
        collectLootButton.targetGraphic = btnImage;
        collectLootButton.onClick.AddListener(OnCollectLootClicked);

        var btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        var btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;
        var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "COLLECT";
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.fontSize = 22;
        btnText.color = Color.white;

        return panel;
    }

    public void ShowLootPanel(int gold, RelicData relic, Player player, CombatNode node)
    {
        pendingGold = gold;
        pendingRelic = relic;
        pendingPlayer = player;
        pendingNode = node;

        lootGoldText.text = $"Gold: +{gold}";
        lootRelicText.text = $"Relic: {relic.DisplayName}\n({relic.StatAffected} +{relic.Amount})";

        combatPanel.SetActive(false);
        lootPanel.SetActive(true);
    }

    private void OnCollectLootClicked()
    {
        if (pendingPlayer != null)
        {
            pendingPlayer.AddGold(pendingGold);
            pendingPlayer.AddRelic(pendingRelic);
        }

        lootPanel.SetActive(false);

        if (combatManager == null)
        {
            combatManager = FindFirstObjectByType<CombatManager>();
        }

        if (combatManager != null)
        {
            combatManager.OnLootCollected();
        }
    }
}
