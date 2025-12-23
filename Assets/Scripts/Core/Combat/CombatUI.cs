using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
    private TextMeshProUGUI lootXPText;
    private TextMeshProUGUI lootTitleText;
    private Button collectLootButton;
    private int pendingGold;
    private int pendingXP;
    private Player pendingPlayer;
    private NodeBase pendingNode;
    private TextMeshProUGUI combatTitleText;
    
    private bool isTargeting = false;
    private int selectedSkillNumber = 0;
    private int selectedTargetIndex = 0;
    private bool isAoESkill = false;
    private List<CombatEnemy> currentEnemies = new List<CombatEnemy>();
    private Button backButton;
    private GameObject actionButtonContainer;
    
    private GameObject potionContainer;
    private List<Button> potionButtons = new List<Button>();
    private List<TextMeshProUGUI> potionTexts = new List<TextMeshProUGUI>();
    private int selectedPotionIndex = -1;
    private Element selectedPotionElement = Element.None;
    private bool isPotionTargeting = false;
    
    private GameObject potionTooltipPanel;
    private TextMeshProUGUI potionTooltipText;

    void Awake()
    {
        combatManager = FindFirstObjectByType<CombatManager>();
        SetupUI();
    }

    void Update()
    {
        if (!isTargeting) return;
        
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
        {
            ExitTargetingMode();
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            CycleTarget(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            CycleTarget(1);
        }
        
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            ConfirmTarget();
        }
    }

    private void SetupUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            GameLog.Error(
                GameLogCategory.System,
                "[CombatUI]",
                GameLog.Join(
                    "SetupFail",
                    GameLog.KV("reason", "CanvasNotFound")
                )
            );
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

        var titleObj = new GameObject("CombatTitle");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.3f, 0.88f);
        titleRect.anchorMax = new Vector2(0.7f, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        combatTitleText = titleObj.AddComponent<TextMeshProUGUI>();
        combatTitleText.text = "COMBAT";
        combatTitleText.alignment = TextAlignmentOptions.Center;
        combatTitleText.fontSize = 28;
        combatTitleText.color = Color.white;

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
        actionButtonContainer = container;
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
        
        backButton = CreateActionButton(container.transform, "Back", "BACK", new Color(0.5f, 0.5f, 0.5f, 1f), OnBackClicked, out _, out _);
        backButton.gameObject.SetActive(false);
        
        CreatePotionContainer(parent);
    }

    private void CreatePotionContainer(Transform parent)
    {
        potionContainer = new GameObject("PotionContainer");
        potionContainer.transform.SetParent(parent, false);
        var containerRect = potionContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.05f, 0.16f);
        containerRect.anchorMax = new Vector2(0.40f, 0.26f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        var layout = potionContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 5;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.padding = new RectOffset(5, 5, 5, 5);

        var bg = potionContainer.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.15f, 0.2f, 0.8f);
        
        CreatePotionTooltip(parent);
    }

    private void CreatePotionTooltip(Transform parent)
    {
        potionTooltipPanel = new GameObject("PotionTooltip");
        potionTooltipPanel.transform.SetParent(parent, false);
        var rect = potionTooltipPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.05f, 0.27f);
        rect.anchorMax = new Vector2(0.40f, 0.38f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = potionTooltipPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        var textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(potionTooltipPanel.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8, 4);
        textRect.offsetMax = new Vector2(-8, -4);

        potionTooltipText = textObj.AddComponent<TextMeshProUGUI>();
        potionTooltipText.fontSize = 12;
        potionTooltipText.color = Color.white;
        potionTooltipText.alignment = TextAlignmentOptions.Left;

        potionTooltipPanel.SetActive(false);
    }

    private void RefreshPotionButtons(Player player)
    {
        if (potionTooltipPanel != null)
        {
            potionTooltipPanel.SetActive(false);
        }

        if (potionContainer != null)
        {
            for (int i = potionContainer.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(potionContainer.transform.GetChild(i).gameObject);
            }
        }

        potionButtons.Clear();
        potionTexts.Clear();

        var potions = player.GetPotions();
        for (int i = 0; i < player.GetMaxPotions(); i++)
        {
            CreatePotionButton(i, i < potions.Count ? potions[i] : null, player);
        }
    }

    private void CreatePotionButton(int index, PotionData potion, Player player)
    {
        var btnObj = new GameObject($"Potion_{index}");
        btnObj.transform.SetParent(potionContainer.transform, false);

        var btnImage = btnObj.AddComponent<Image>();
        
        if (potion != null)
        {
            btnImage.color = GetPotionColor(potion);
            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImage;
            potionButtons.Add(btn);

            int capturedIndex = index;
            btn.onClick.AddListener(() => OnPotionClicked(capturedIndex, player));
            
            var eventTrigger = btnObj.AddComponent<EventTrigger>();
            var pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            PotionData capturedPotion = potion;
            pointerEnter.callback.AddListener((data) => ShowPotionTooltip(capturedPotion));
            eventTrigger.triggers.Add(pointerEnter);
            
            var pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            pointerExit.callback.AddListener((data) => HidePotionTooltip());
            eventTrigger.triggers.Add(pointerExit);
        }
        else
        {
            btnImage.color = new Color(0.15f, 0.15f, 0.2f, 0.5f);
            potionButtons.Add(null);
        }

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textObj.AddComponent<TextMeshProUGUI>();
        if (potion != null)
        {
            string label = GetPotionLabel(potion);
            text.text = label;
            text.color = Color.white;
        }
        else
        {
            text.text = $"{index + 1}";
            text.color = new Color(0.4f, 0.4f, 0.4f);
        }
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 11;
        potionTexts.Add(text);
    }

    private string GetPotionLabel(PotionData potion)
    {
        var stat = potion.StatAffected.ToLower().Trim();
        if (stat.Contains("health"))
            return $"HP\n+{potion.Amount}";
        if (stat.Contains("elemental"))
            return $"DMG\n{potion.Amount}";
        if (stat.Contains("crit rate"))
            return $"CR\n+{potion.Amount}%";
        if (stat.Contains("crit damage"))
            return $"CD\n+{potion.Amount}%";
        return potion.DisplayName;
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

    private void ShowPotionTooltip(PotionData potion)
    {
        if (potionTooltipPanel == null) return;
        
        var stat = potion.StatAffected.ToLower().Trim();
        string desc = "";
        
        if (stat.Contains("health"))
            desc = $"<color=#ff5555>{potion.DisplayName}</color>\nRestores <color=#55ff55>{potion.Amount}</color> HP";
        else if (stat.Contains("elemental"))
            desc = $"<color=#aa77ff>{potion.DisplayName}</color>\nDeals <color=#ffaa55>{potion.Amount}</color> elemental damage (random element)\n<color=#888888>Requires target selection</color>";
        else if (stat.Contains("crit rate"))
            desc = $"<color=#ffdd55>{potion.DisplayName}</color>\nIncreases crit chance by <color=#55ff55>+{potion.Amount}%</color>\n<color=#888888>Lasts until world ends</color>";
        else if (stat.Contains("crit damage"))
            desc = $"<color=#ff55ff>{potion.DisplayName}</color>\nIncreases crit damage by <color=#55ff55>+{potion.Amount}%</color>\n<color=#888888>Lasts until world ends</color>";
        else
            desc = potion.DisplayName;
        
        potionTooltipText.text = desc;
        potionTooltipPanel.SetActive(true);
    }

    private void HidePotionTooltip()
    {
        if (potionTooltipPanel != null)
            potionTooltipPanel.SetActive(false);
    }

    private void OnPotionClicked(int index, Player player)
    {
        if (isTargeting) return;
        
        var potion = player.GetPotion(index);
        if (potion == null) return;

        var stat = potion.StatAffected.ToLower().Trim();
        
        if (stat.Contains("elemental"))
        {
            selectedPotionIndex = index;
            selectedPotionElement = RollPotionElement();
            isPotionTargeting = true;
            EnterPotionTargetingMode(potion);
        }
        else
        {
            if (player.UsePotion(index, null))
            {
                RefreshPotionButtons(player);
                
                var refs = FindFirstObjectByType<Referencer>();
                if (refs != null && refs.playerStatsUI != null)
                {
                    refs.playerStatsUI.UpdateStats();
                }
                UpdatePlayerHealth(player);
            }
        }
    }

    private Element RollPotionElement()
    {
        var elements = new Element[] { Element.Fire, Element.Ice, Element.Water, Element.Wind, Element.Rock };
        return elements[Random.Range(0, elements.Length)];
    }

    private void EnterPotionTargetingMode(PotionData potion)
    {
        isTargeting = true;
        isAoESkill = false;
        
        var aliveEnemies = GetAliveEnemies();
        if (aliveEnemies.Count == 0)
        {
            ExitPotionTargetingMode();
            return;
        }
        
        selectedTargetIndex = 0;
        
        attackButton.gameObject.SetActive(false);
        skill1Button.gameObject.SetActive(false);
        skill2Button.gameObject.SetActive(false);
        skill3Button.gameObject.SetActive(false);
        backButton.gameObject.SetActive(true);
        
        var backText = backButton.GetComponentInChildren<TextMeshProUGUI>();
        if (backText != null)
        {
            backText.text = $"BACK\n({selectedPotionElement})";
        }
        
        foreach (var btn in potionButtons)
        {
            if (btn != null) btn.gameObject.SetActive(false);
        }
        
        UpdateTargetArrows();
    }

    private void ExitPotionTargetingMode()
    {
        isPotionTargeting = false;
        selectedPotionIndex = -1;
        selectedPotionElement = Element.None;
        
        var backText = backButton.GetComponentInChildren<TextMeshProUGUI>();
        if (backText != null)
        {
            backText.text = "BACK";
        }
        
        ExitTargetingMode();
        
        foreach (var btn in potionButtons)
        {
            if (btn != null) btn.gameObject.SetActive(true);
        }
    }

    private void ConfirmPotionTarget(Player player)
    {
        var aliveEnemies = GetAliveEnemies();
        if (aliveEnemies.Count == 0 || selectedTargetIndex >= aliveEnemies.Count) return;
        
        var target = aliveEnemies[selectedTargetIndex];
        
        if (player.UsePotion(selectedPotionIndex, target, selectedPotionElement))
        {
            UpdateEnemyHealth(target);
            RefreshPotionButtons(player);
        }
        
        ExitPotionTargetingMode();
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
        EnterTargetingMode(0, false);
    }

    private void OnSkill1Clicked()
    {
        bool isAoE = IsSkillAoE(1);
        EnterTargetingMode(1, isAoE);
    }

    private void OnSkill2Clicked()
    {
        bool isAoE = IsSkillAoE(2);
        EnterTargetingMode(2, isAoE);
    }

    private void OnSkill3Clicked()
    {
        bool isAoE = IsSkillAoE(3);
        EnterTargetingMode(3, isAoE);
    }

    private void OnBackClicked()
    {
        if (isPotionTargeting)
        {
            ExitPotionTargetingMode();
        }
        else
        {
            ExitTargetingMode();
        }
    }

    private bool IsSkillAoE(int skillNumber)
    {
        if (combatManager == null) return false;
        var enemies = combatManager.GetEnemies();
        if (enemies == null || enemies.Count <= 1) return false;
        
        var refs = FindFirstObjectByType<Referencer>();
        if (refs == null || refs.player == null) return false;
        
        var character = refs.player.GetCharacter();
        if (character == null) return false;
        
        string skillName = skillNumber switch
        {
            1 => character.Skill1,
            2 => character.Skill2,
            3 => character.Skill3,
            _ => ""
        };
        
        string lower = skillName.ToLower();
        return lower == "bladestorm" || lower == "tripleshot" || lower == "holynova";
    }

    private void EnterTargetingMode(int skillNumber, bool isAoE)
    {
        isTargeting = true;
        selectedSkillNumber = skillNumber;
        isAoESkill = isAoE;
        
        var aliveEnemies = GetAliveEnemies();
        if (aliveEnemies.Count == 0)
        {
            isTargeting = false;
            return;
        }
        
        selectedTargetIndex = 0;
        
        attackButton.gameObject.SetActive(false);
        skill1Button.gameObject.SetActive(false);
        skill2Button.gameObject.SetActive(false);
        skill3Button.gameObject.SetActive(false);
        backButton.gameObject.SetActive(true);
        
        foreach (var btn in potionButtons)
        {
            if (btn != null) btn.gameObject.SetActive(false);
        }
        
        UpdateTargetArrows();
    }

    private void ExitTargetingMode()
    {
        isTargeting = false;
        selectedSkillNumber = 0;
        isAoESkill = false;
        
        attackButton.gameObject.SetActive(true);
        skill1Button.gameObject.SetActive(true);
        skill2Button.gameObject.SetActive(true);
        skill3Button.gameObject.SetActive(true);
        backButton.gameObject.SetActive(false);
        
        foreach (var btn in potionButtons)
        {
            if (btn != null) btn.gameObject.SetActive(true);
        }
        
        HideAllArrows();
    }

    private void CycleTarget(int direction)
    {
        var aliveEnemies = GetAliveEnemies();
        if (aliveEnemies.Count == 0) return;
        
        selectedTargetIndex += direction;
        
        if (selectedTargetIndex < 0)
            selectedTargetIndex = aliveEnemies.Count - 1;
        else if (selectedTargetIndex >= aliveEnemies.Count)
            selectedTargetIndex = 0;
        
        UpdateTargetArrows();
    }

    private void ConfirmTarget()
    {
        if (isPotionTargeting)
        {
            var refs = FindFirstObjectByType<Referencer>();
            if (refs != null && refs.player != null)
            {
                ConfirmPotionTarget(refs.player);
            }
            return;
        }
        
        var aliveEnemies = GetAliveEnemies();
        if (aliveEnemies.Count == 0 || selectedTargetIndex >= aliveEnemies.Count) return;
        
        var target = aliveEnemies[selectedTargetIndex];
        
        if (combatManager == null)
        {
            combatManager = FindFirstObjectByType<CombatManager>();
        }
        
        if (combatManager != null)
        {
            if (selectedSkillNumber == 0)
            {
                combatManager.OnPlayerAttackTarget(target);
            }
            else
            {
                combatManager.OnPlayerSkillTarget(selectedSkillNumber, target);
            }
        }
        
        ExitTargetingMode();
    }

    private List<CombatEnemy> GetAliveEnemies()
    {
        var alive = new List<CombatEnemy>();
        foreach (var enemy in currentEnemies)
        {
            if (enemy.IsAlive()) alive.Add(enemy);
        }
        return alive;
    }

    private void UpdateTargetArrows()
    {
        var aliveEnemies = GetAliveEnemies();
        
        foreach (var kvp in enemySlots)
        {
            var enemy = kvp.Key;
            var slot = kvp.Value;
            
            if (slot.Arrow == null) continue;
            
            if (!enemy.IsAlive())
            {
                slot.Arrow.SetActive(false);
                continue;
            }
            
            int aliveIndex = aliveEnemies.IndexOf(enemy);
            
            if (isAoESkill)
            {
                slot.Arrow.SetActive(true);
                var arrowImage = slot.Arrow.GetComponent<Image>();
                if (arrowImage != null)
                {
                    if (aliveIndex == selectedTargetIndex)
                    {
                        arrowImage.color = new Color(1f, 0.8f, 0.2f);
                        slot.Arrow.transform.localScale = Vector3.one;
                    }
                    else
                    {
                        arrowImage.color = new Color(1f, 0.5f, 0.2f, 0.6f);
                        slot.Arrow.transform.localScale = Vector3.one * 0.7f;
                    }
                }
            }
            else
            {
                bool isSelected = aliveIndex == selectedTargetIndex;
                slot.Arrow.SetActive(isSelected);
                if (isSelected)
                {
                    var arrowImage = slot.Arrow.GetComponent<Image>();
                    if (arrowImage != null)
                    {
                        arrowImage.color = new Color(1f, 0.8f, 0.2f);
                    }
                    slot.Arrow.transform.localScale = Vector3.one;
                }
            }
        }
    }

    private void HideAllArrows()
    {
        foreach (var slot in enemySlots.Values)
        {
            if (slot.Arrow != null)
            {
                slot.Arrow.SetActive(false);
            }
        }
    }

    private void OnEnemyClicked(CombatEnemy enemy)
    {
        if (!isTargeting) return;
        if (!enemy.IsAlive()) return;
        
        var aliveEnemies = GetAliveEnemies();
        int index = aliveEnemies.IndexOf(enemy);
        if (index >= 0)
        {
            selectedTargetIndex = index;
            ConfirmTarget();
        }
    }

    private void OnEnemyHover(CombatEnemy enemy)
    {
        if (!isTargeting) return;
        if (!enemy.IsAlive()) return;
        
        var aliveEnemies = GetAliveEnemies();
        int index = aliveEnemies.IndexOf(enemy);
        if (index >= 0)
        {
            selectedTargetIndex = index;
            UpdateTargetArrows();
        }
    }

    public void ShowCombat(List<CombatEnemy> enemies, Player player, string title = null)
    {
        if (combatPanel == null)
        {
            GameLog.Error(
                GameLogCategory.System,
                "[CombatUI]",
                GameLog.Join(
                    "ShowCombatFail",
                    GameLog.KV("reason", "CombatPanelNull")
                )
            );
            SetupUI();
        }

        currentEnemies = new List<CombatEnemy>(enemies);
        isTargeting = false;
        
        ClearEnemySlots();

        foreach (var enemy in enemies)
        {
            CreateEnemySlot(enemy);
        }

        if (combatTitleText != null)
        {
            combatTitleText.text = title ?? "COMBAT";
            combatTitleText.color = string.IsNullOrEmpty(title) ? Color.white : new Color(1f, 0.5f, 0.2f);
        }

        UpdateSkillButtons(player);
        UpdatePlayerHealth(player);
        RefreshPotionButtons(player);
        combatPanel.SetActive(true);
        GameLog.System(GameLog.Join(
            "CombatUIShow",
            GameLog.KV("enemies", enemies.Count),
            GameLog.KV("title", title ?? "COMBAT")
        ), GameLogVerbosity.Verbose);
        SetPlayerTurn(true);
        isPotionTargeting = false;
        selectedPotionIndex = -1;
        selectedPotionElement = Element.None;
        
        if (backButton != null) backButton.gameObject.SetActive(false);
    }

    public void UpdateSkillButtons(Player player)
    {
        var character = player.GetCharacter();
        if (character != null)
        {
            // Skill 1 - show cooldown if on cooldown, and DirtyStab stacks if applicable
            int cd1 = player.GetSkillCooldown(0);
            string skill1Name = character.Skill1;
            string dirtyStabIndicator = "";
            
            // Show DirtyStab stack indicator for Rogue
            if (skill1Name.ToLower() == "dirtystab")
            {
                int stacks = player.GetDirtyStabStacks();
                if (stacks > 0)
                {
                    dirtyStabIndicator = $" <color=#ffaa00>[+{stacks * 20}%]</color>";
                }
            }
            
            if (cd1 > 0)
            {
                if (skill1Text != null) skill1Text.text = $"{skill1Name}{dirtyStabIndicator}\n<size=12><color=#888888>CD: {cd1}</color></size>";
                if (skill1Button != null) skill1Button.interactable = false;
            }
            else
            {
                if (skill1Text != null) skill1Text.text = $"{skill1Name}{dirtyStabIndicator}";
                if (skill1Button != null) skill1Button.interactable = true;
            }
            
            // Skill 2 - show cooldown if on cooldown
            int cd2 = player.GetSkillCooldown(1);
            if (cd2 > 0)
            {
                if (skill2Text != null) skill2Text.text = $"{character.Skill2}\n<size=12><color=#888888>CD: {cd2}</color></size>";
                if (skill2Button != null) skill2Button.interactable = false;
            }
            else
            {
                if (skill2Text != null) skill2Text.text = character.Skill2;
                if (skill2Button != null) skill2Button.interactable = true;
            }
            
            // Skill 3 - show energy and cooldown, disable if not enough energy or on cooldown
            int cd3 = player.GetSkillCooldown(2);
            int currentEnergy = player.GetEnergy();
            int energyCost = character.Skill3EnergyCost;
            bool canUseSkill3 = player.CanUseSkill3();
            
            string skill3Status = "";
            if (cd3 > 0)
            {
                skill3Status = $"\n<size=12><color=#888888>CD: {cd3}</color></size>";
            }
            else
            {
                skill3Status = $"\n<size=12><color={(canUseSkill3 ? "#44ff44" : "#ff4444")}>{currentEnergy}/{energyCost}</color></size>";
            }
            
            if (skill3Text != null) skill3Text.text = $"{character.Skill3}{skill3Status}";
            if (skill3Button != null) skill3Button.interactable = canUseSkill3;
            
            // Update tooltips
            if (skill1Tooltip != null) skill1Tooltip.SetTooltip($"<b>{character.Skill1}</b>\n{PlayerStatsUI.GetSkillDescription(character.Skill1)}\nEnergy Gain: +{character.Skill1EnergyGain}\nCooldown: {character.Skill1Cooldown} turn(s)");
            if (skill2Tooltip != null) skill2Tooltip.SetTooltip($"<b>{character.Skill2}</b>\n{PlayerStatsUI.GetSkillDescription(character.Skill2)}\nEnergy Gain: +{character.Skill2EnergyGain}\nCooldown: {character.Skill2Cooldown} turn(s)");
            if (skill3Tooltip != null) skill3Tooltip.SetTooltip($"<b>{character.Skill3}</b>\n{PlayerStatsUI.GetSkillDescription(character.Skill3)}\nEnergy Cost: {character.Skill3EnergyCost}\nCooldown: {character.Skill3Cooldown} turn(s)");
        }
        else
        {
            if (skill1Text != null) skill1Text.text = "---";
            if (skill2Text != null) skill2Text.text = "---";
            if (skill3Text != null) skill3Text.text = "---";
            
            if (skill1Button != null) skill1Button.interactable = false;
            if (skill2Button != null) skill2Button.interactable = false;
            if (skill3Button != null) skill3Button.interactable = false;
            
            if (skill1Tooltip != null) skill1Tooltip.SetTooltip("");
            if (skill2Tooltip != null) skill2Tooltip.SetTooltip("");
            if (skill3Tooltip != null) skill3Tooltip.SetTooltip("");
        }
    }
    
    public void UpdatePlayerEnergy(Player player)
    {
        // Energy is shown on Skill 3 button, so just update skill buttons
        UpdateSkillButtons(player);
    }

    public void HideCombat()
    {
        var tooltipUI = FindFirstObjectByType<TooltipUI>();
        if (tooltipUI != null)
        {
            tooltipUI.Hide();
        }
        
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
        
        var clickBtn = slot.AddComponent<Button>();
        clickBtn.targetGraphic = bg;
        CombatEnemy capturedEnemy = enemy;
        clickBtn.onClick.AddListener(() => OnEnemyClicked(capturedEnemy));
        
        var eventTrigger = slot.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => OnEnemyHover(capturedEnemy));
        eventTrigger.triggers.Add(pointerEnter);

        var layout = slot.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        var arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(slot.transform, false);
        var arrowRect = arrowObj.AddComponent<RectTransform>();
        var arrowLayout = arrowObj.AddComponent<LayoutElement>();
        arrowLayout.preferredHeight = 30;
        arrowLayout.preferredWidth = 40;
        var arrowText = arrowObj.AddComponent<TextMeshProUGUI>();
        arrowText.text = "▼";
        arrowText.alignment = TextAlignmentOptions.Center;
        arrowText.fontSize = 28;
        arrowText.color = new Color(1f, 0.8f, 0.2f);
        arrowObj.SetActive(false);

        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(slot.transform, false);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = enemy.Name;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = 20;
        nameText.color = Color.white;
        var nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.preferredHeight = 30;

        var elementObj = new GameObject("Element");
        elementObj.transform.SetParent(slot.transform, false);
        var elementText = elementObj.AddComponent<TextMeshProUGUI>();
        if (enemy.IsBoss)
        {
            elementText.text = "BOSS";
            elementText.color = new Color(1f, 0.3f, 0.3f);
        }
        else
        {
            elementText.text = enemy.Affinity.ToString();
            elementText.color = GetElementColor(enemy.Affinity);
        }
        elementText.alignment = TextAlignmentOptions.Center;
        elementText.fontSize = 14;
        var elementLayout = elementObj.AddComponent<LayoutElement>();
        elementLayout.preferredHeight = 20;

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
            NameText = nameText,
            Arrow = arrowObj,
            ClickArea = clickBtn
        };
    }

    public void UpdateEnemyHealth(CombatEnemy enemy)
    {
        if (!enemySlots.ContainsKey(enemy)) return;

        var slot = enemySlots[enemy];
        float healthPercent = (float)enemy.Health / enemy.MaxHealth;
        slot.HealthFill.fillAmount = healthPercent;
        
        // Build health text with status effects
        string healthDisplay = $"{enemy.Health}/{enemy.MaxHealth}";
        
        // Show status effect indicators
        var effects = enemy.GetStatusEffects();
        foreach (var effect in effects)
        {
            if (effect.Type == StatusEffectType.DoT)
            {
                string stackText = effect.StackCount > 1 ? $"x{effect.StackCount}" : "";
                healthDisplay += $" <color=#ff6600>-{(int)effect.Value} DoT{stackText} ({effect.Duration}t)</color>";
            }
            else if (effect.Type == StatusEffectType.Stun)
            {
                healthDisplay += $" <color=#ffff00>STUNNED ({effect.Duration}t)</color>";
            }
        }
        
        slot.HealthText.text = healthDisplay;

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
        
        // Display shield if player has any
        int shield = player.GetShield();
        string shieldText = shield > 0 ? $" <color=#44aaff>[+{shield}]</color>" : "";
        playerHealthText.text = $"HP: {player.GetHealth()}/{player.GetMaxHealth()}{shieldText}";

        if (healthPercent > 0.5f)
            playerHealthFill.color = Color.green;
        else if (healthPercent > 0.25f)
            playerHealthFill.color = Color.yellow;
        else
            playerHealthFill.color = Color.red;
    }

    public void ShowDamageToEnemy(CombatEnemy enemy, int damage, bool isCrit = false)
    {
        GameLog.System(GameLog.Join(
            "FloatingText",
            GameLog.KV("target", enemy != null ? enemy.Name : "null"),
            GameLog.KV("type", "DamageToEnemy"),
            GameLog.KV("amount", damage),
            GameLog.KV("crit", isCrit)
        ), GameLogVerbosity.Verbose);
        
        // Show floating text
        var fctManager = FloatingTextManager.Instance;
        if (fctManager == null)
        {
            GameLog.Warn(
                GameLogCategory.System,
                "[CombatUI]",
                GameLog.Join(
                    "FloatingTextMissing",
                    GameLog.KV("reason", "InstanceNull")
                )
            );
            return;
        }
        
        Transform enemyTransform = GetEnemyTransform(enemy);
        if (enemyTransform == null)
        {
            GameLog.Warn(
                GameLogCategory.System,
                "[CombatUI]",
                GameLog.Join(
                    "FloatingTextMissing",
                    GameLog.KV("reason", "EnemyTransformNull"),
                    GameLog.KV("enemy", enemy != null ? enemy.Name : "null")
                )
            );
            return;
        }
        
        fctManager.ShowDamage(enemyTransform, damage, isCrit);
    }

    public void ShowDamageToPlayer(int damage)
    {
        GameLog.System(GameLog.Join(
            "FloatingText",
            GameLog.KV("target", "Player"),
            GameLog.KV("type", "DamageToPlayer"),
            GameLog.KV("amount", damage)
        ), GameLogVerbosity.Verbose);
        
        // Show floating text
        var fctManager = FloatingTextManager.Instance;
        if (fctManager != null && playerHealthBar != null)
        {
            fctManager.ShowDamageTaken(playerHealthBar.transform, damage);
        }
    }
    
    public void ShowHealToPlayer(int amount)
    {
        GameLog.System(GameLog.Join(
            "FloatingText",
            GameLog.KV("target", "Player"),
            GameLog.KV("type", "HealToPlayer"),
            GameLog.KV("amount", amount)
        ), GameLogVerbosity.Verbose);
        
        var fctManager = FloatingTextManager.Instance;
        if (fctManager != null && playerHealthBar != null)
        {
            fctManager.ShowHeal(playerHealthBar.transform, amount);
        }
    }
    
    public void ShowShieldToPlayer(int amount, FloatingTextType shieldType)
    {
        var fctManager = FloatingTextManager.Instance;
        if (fctManager != null && playerHealthBar != null)
        {
            fctManager.ShowShield(playerHealthBar.transform, amount, shieldType);
        }
    }
    
    public void ShowStatusToEnemy(CombatEnemy enemy, string statusName, bool gained, int stacksDelta = 0)
    {
        var fctManager = FloatingTextManager.Instance;
        if (fctManager != null)
        {
            Transform enemyTransform = GetEnemyTransform(enemy);
            if (enemyTransform != null)
            {
                fctManager.ShowStatus(enemyTransform, statusName, gained, stacksDelta);
            }
        }
    }
    
    public void ShowDoTTickToEnemy(CombatEnemy enemy, int damage, string dotName = "DoT")
    {
        var fctManager = FloatingTextManager.Instance;
        if (fctManager != null)
        {
            Transform enemyTransform = GetEnemyTransform(enemy);
            if (enemyTransform != null)
            {
                fctManager.ShowDoTTick(enemyTransform, damage, dotName);
            }
        }
    }
    
    public void ShowTurnSkippedToEnemy(CombatEnemy enemy, string reason = "Stunned!")
    {
        var fctManager = FloatingTextManager.Instance;
        if (fctManager != null)
        {
            Transform enemyTransform = GetEnemyTransform(enemy);
            if (enemyTransform != null)
            {
                fctManager.ShowTurnSkipped(enemyTransform, reason);
            }
        }
    }
    
    public void ShowReactionToEnemy(CombatEnemy enemy, string reactionName, float multiplier = 0f, int bonusDamage = 0)
    {
        var fctManager = FloatingTextManager.Instance;
        if (fctManager != null)
        {
            Transform enemyTransform = GetEnemyTransform(enemy);
            if (enemyTransform != null)
            {
                fctManager.ShowReaction(enemyTransform, reactionName, multiplier, bonusDamage);
            }
        }
    }
    
    public Transform GetEnemyTransform(CombatEnemy enemy)
    {
        if (enemySlots.ContainsKey(enemy))
        {
            return enemySlots[enemy].Root.transform;
        }
        return null;
    }
    
    public Transform GetPlayerTransform()
    {
        return playerHealthBar?.transform;
    }

    public void SetPlayerTurn(bool isPlayerTurn)
    {
        if (attackButton != null)
            attackButton.interactable = isPlayerTurn;
            
        if (isPlayerTurn)
        {
            // Update skill buttons with cooldown/energy state
            var refs = FindFirstObjectByType<Referencer>();
            if (refs != null && refs.player != null)
            {
                UpdateSkillButtons(refs.player);
            }
            UpdateReactionIndicator();
        }
        else
        {
            // Disable all skill buttons during enemy turn
            if (skill1Button != null) skill1Button.interactable = false;
            if (skill2Button != null) skill2Button.interactable = false;
            if (skill3Button != null) skill3Button.interactable = false;
        }
    }
    
    public void UpdateReactionIndicator()
    {
        var refs = FindFirstObjectByType<Referencer>();
        if (refs == null || refs.player == null) return;
        
        var player = refs.player;
        bool reactionReady = player.HasReactionReady();
        
        Color normalColor = new Color(0.2f, 0.5f, 0.7f, 1f);
        Color attackNormalColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        Color unavailableColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        
        // Check skill availability (cooldown and energy)
        int cd1 = player.GetSkillCooldown(0);
        int cd2 = player.GetSkillCooldown(1);
        int cd3 = player.GetSkillCooldown(2);
        var character = player.GetCharacter();
        int energyCost = character != null ? character.Skill3EnergyCost : 0;
        bool canUseSkill3 = player.GetEnergy() >= energyCost && cd3 <= 0;
        bool canUseSkill1 = cd1 <= 0;
        bool canUseSkill2 = cd2 <= 0;
        
        if (reactionReady)
        {
            var orbSystem = player.GetOrbSystem();
            Color colorA = GetElementColor(orbSystem.OrbAMark);
            Color colorB = GetElementColor(orbSystem.OrbBMark);
            
            // Attack is always available
            SetButtonDiagonalSplit(attackButton, colorA, colorB);
            
            // Only apply reaction colors to skills that are actually usable
            if (canUseSkill1)
                SetButtonDiagonalSplit(skill1Button, colorA, colorB);
            else
                ClearButtonDiagonalSplit(skill1Button, unavailableColor);
                
            if (canUseSkill2)
                SetButtonDiagonalSplit(skill2Button, colorA, colorB);
            else
                ClearButtonDiagonalSplit(skill2Button, unavailableColor);
                
            if (canUseSkill3)
                SetButtonDiagonalSplit(skill3Button, colorA, colorB);
            else
                ClearButtonDiagonalSplit(skill3Button, unavailableColor);
        }
        else
        {
            ClearButtonDiagonalSplit(attackButton, attackNormalColor);
            ClearButtonDiagonalSplit(skill1Button, canUseSkill1 ? normalColor : unavailableColor);
            ClearButtonDiagonalSplit(skill2Button, canUseSkill2 ? normalColor : unavailableColor);
            ClearButtonDiagonalSplit(skill3Button, canUseSkill3 ? normalColor : unavailableColor);
        }
    }
    
    private void SetButtonDiagonalSplit(Button button, Color topColor, Color bottomColor)
    {
        if (button == null) return;
        
        var buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = Color.clear;
        }
        
        var diagonalOverlay = button.transform.Find("DiagonalOverlay");
        RawImage rawImage;
        
        if (diagonalOverlay == null)
        {
            var obj = new GameObject("DiagonalOverlay");
            obj.transform.SetParent(button.transform, false);
            obj.transform.SetAsFirstSibling();
            
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            rawImage = obj.AddComponent<RawImage>();
            rawImage.raycastTarget = false;
            diagonalOverlay = obj.transform;
        }
        else
        {
            rawImage = diagonalOverlay.GetComponent<RawImage>();
            diagonalOverlay.gameObject.SetActive(true);
        }
        
        rawImage.texture = CreateDiagonalTexture(topColor, bottomColor);
        
        var outline = button.GetComponent<Outline>();
        if (outline == null)
        {
            outline = button.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(2, 2);
        outline.enabled = true;
    }
    
    private Texture2D CreateDiagonalTexture(Color topColor, Color bottomColor)
    {
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (x + y < size)
                {
                    tex.SetPixel(x, y, topColor);
                }
                else
                {
                    tex.SetPixel(x, y, bottomColor);
                }
            }
        }
        
        tex.Apply();
        return tex;
    }
    
    private void ClearButtonDiagonalSplit(Button button, Color normalColor)
    {
        if (button == null) return;
        
        var buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = normalColor;
        }
        
        var diagonalOverlay = button.transform.Find("DiagonalOverlay");
        if (diagonalOverlay != null)
        {
            diagonalOverlay.gameObject.SetActive(false);
        }
        
        var outline = button.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }
    }

    private Color GetElementColor(Element element)
    {
        return element switch
        {
            Element.Fire => new Color(1f, 0.4f, 0.2f),
            Element.Ice => new Color(0.4f, 0.8f, 1f),
            Element.Water => new Color(0.2f, 0.5f, 1f),
            Element.Wind => new Color(0.6f, 1f, 0.6f),
            Element.Rock => new Color(0.7f, 0.5f, 0.3f),
            _ => Color.white
        };
    }

    private class EnemyUISlot
    {
        public GameObject Root;
        public Image HealthFill;
        public TextMeshProUGUI HealthText;
        public TextMeshProUGUI NameText;
        public GameObject Arrow;
        public Button ClickArea;
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
        lootTitleText = titleObj.AddComponent<TextMeshProUGUI>();
        lootTitleText.text = "VICTORY!";
        lootTitleText.alignment = TextAlignmentOptions.Center;
        lootTitleText.fontSize = 32;
        lootTitleText.color = new Color(1f, 0.85f, 0.2f);

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

        var xpObj = new GameObject("XPText");
        xpObj.transform.SetParent(panel.transform, false);
        var xpRect = xpObj.AddComponent<RectTransform>();
        xpRect.anchorMin = new Vector2(0.1f, 0.3f);
        xpRect.anchorMax = new Vector2(0.9f, 0.5f);
        xpRect.offsetMin = Vector2.zero;
        xpRect.offsetMax = Vector2.zero;
        lootXPText = xpObj.AddComponent<TextMeshProUGUI>();
        lootXPText.alignment = TextAlignmentOptions.Center;
        lootXPText.fontSize = 24;
        lootXPText.color = new Color(0.4f, 0.9f, 1f);

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

    public void ShowLootPanel(int gold, int xp, Player player, NodeBase node, string title = null)
    {
        pendingGold = gold;
        pendingXP = xp;
        pendingPlayer = player;
        pendingNode = node;

        if (lootTitleText != null)
        {
            lootTitleText.text = title ?? "VICTORY!";
        }
        lootGoldText.text = $"Gold: +{gold}";
        lootXPText.text = $"XP: +{xp}";

        combatPanel.SetActive(false);
        lootPanel.SetActive(true);
    }

    private void OnCollectLootClicked()
    {
        if (pendingPlayer != null)
        {
            pendingPlayer.AddGold(pendingGold);
            // XP is already added in CombatManager.EndCombat (Phase 1)
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
