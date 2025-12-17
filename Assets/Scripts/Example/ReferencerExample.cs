// using UnityEngine;
// using TMPro;
// using System.Collections.Generic;
// using System.Text.RegularExpressions;
// using UnityEngine.UI;

// //fix script execution order.
// [DefaultExecutionOrder(-10000)]
// public class ReferencerExample : MonoBehaviour
// {
//     public GameManager gm;
//     public Player pl;
//     public UIManager ui;
//     public Enemy en;
//     public PlayerInventory inv;
//     public InventoryUI invUI;
//     public LootUI lootUI;
//     public CampfireUI campfireUI;
//     public EventPanelUI eventPanelUI;
//     public EventItemSlot eventItemSlot;
//     public MonoBehaviour activeItemReceiver; // current target slot for click-to-send (campfire/event)
//     public DeckManager deck;

//     public Transform invEquippedRoot, invBackpackRoot, invSafeStashRoot, lootRoot;
//     public InventorySlotUI invSlotTemplate;

//     public GameObject doorSlamButton, lootPanel, campfirePanel, shopPanel, encounterSelectPanel, enemyPanel;
//     public TextMeshProUGUI optionOneText, optionTwoText, campfireUpgradeText;
//     public TextMeshProUGUI shopItemOneCostText, shopItemTwoCostText, shopItemThreeCostText;
//     public TextMeshProUGUI statsCritPctText, statsCritDmgText, statsBaseDamageText, statsDodgePctText;
//     public TextMeshProUGUI statsDamageReductionText, statsDamageReductionPctText, statsQualityEffectText, statsGoldText, statsEncounterText;
//     public TextMeshProUGUI statsRetaliationText, statsStartingShieldText;
//     public TextMeshProUGUI enemyNameText, enemyDamageText;
//     public TextMeshProUGUI[] statsQualityTypeTexts, statsQualityMaterialTexts;

//     public UnityEngine.UI.Button optionOneButton, optionTwoButton, campfireHealButton, campfireUpgradeButton;
//     public UnityEngine.UI.Button shopItemOneButton, shopItemTwoButton, shopItemThreeButton, shopContinueButton;
//     public UnityEngine.UI.Image shopItemOneImage, shopItemTwoImage, shopItemThreeImage;
//     public UnityEngine.UI.Image enemyPortrait, playerPortrait;

//     public Dictionary<string, Sprite> itemSpriteMap;
//     public Material matSturdy, matPrismatic, matElusive;
//     public DoorAnimator doorAnimator;
//     public HealthBarUI playerHealthBar, enemyHealthBar;
//     public DamageTextPool damageTextPool;

//     // Map sprites for node-based encounter UI
//     public Sprite mapMobSprite;
//     public Sprite mapEliteSprite;
//     public Sprite mapEventSprite;
//     public Sprite mapCampfireSprite;
//     public Sprite mapAlchemyLabSprite;
//     public Sprite mapMaterialWorkshopSprite;
//     public Sprite mapShopSprite;
//     public Sprite mapBossSprite;
//     public Sprite mapTreasureSprite;
//     public Sprite shopBuyRelicIcon;

//     void Awake()
//     {
//         gm = FindFirstObjectByType<GameManager>();
//         pl = FindFirstObjectByType<Player>();
//         ui = FindFirstObjectByType<UIManager>();
//         en = FindFirstObjectByType<Enemy>();
//         inv = FindFirstObjectByType<PlayerInventory>();
//         invUI = FindFirstObjectByType<InventoryUI>();
//         lootUI = FindFirstObjectByType<LootUI>();
//         var tc = FindFirstObjectByType<TooltipController>();
//         if (tc == null)
//         {
//             var go = new GameObject("TooltipController");
//             go.AddComponent<TooltipController>();
//         }
//         var fei = FindFirstObjectByType<FloatingEmojiIndicator>();
//         if (fei == null)
//         {
//             var go = new GameObject("FloatingEmojiIndicator");
//             go.AddComponent<FloatingEmojiIndicator>();
//         }
//         deck = FindFirstObjectByType<DeckManager>();
//         if (deck == null)
//         {
//             deck = gm.gameObject.AddComponent<DeckManager>();
//         }

//         // DoorAnimator will be configured after doorSlamButton is resolved below

//         // Load Enchantment Materials
//         matSturdy = Resources.Load<Material>("Materials/Enchant_Sturdy");
//         if (matSturdy == null) Debug.LogWarning("Material 'Materials/Enchant_Sturdy' not found in Resources.");

//         matPrismatic = Resources.Load<Material>("Materials/Enchant_Prismatic");
//         if (matPrismatic == null) Debug.LogWarning("Material 'Materials/Enchant_Prismatic' not found in Resources.");

//         matElusive = Resources.Load<Material>("Materials/Enchant_Elusive");
//         if (matElusive == null) Debug.LogWarning("Material 'Materials/Enchant_Elusive' not found in Resources.");

//         // Load map sprites (optional; other scripts should use Referencer)
//         mapMobSprite = Resources.Load<Sprite>("Sprites/Map/mob");
//         mapEliteSprite = Resources.Load<Sprite>("Sprites/Map/elite");
//         mapEventSprite = Resources.Load<Sprite>("Sprites/Map/event");
//         mapCampfireSprite = Resources.Load<Sprite>("Sprites/Map/campfire");
//         mapAlchemyLabSprite = Resources.Load<Sprite>("Sprites/Map/alchemyLab");
//         mapMaterialWorkshopSprite = Resources.Load<Sprite>("Sprites/Map/materialWorkshop");
//         mapShopSprite = Resources.Load<Sprite>("Sprites/Map/shop");
//         mapBossSprite = Resources.Load<Sprite>("Sprites/Map/boss");
//         // Treasure icon path provided by user: Resources/Sprites/treasure.png
//         mapTreasureSprite = Resources.Load<Sprite>("Sprites/Map/treasure");
//         if (mapTreasureSprite == null) Debug.LogWarning("Treasure sprite not found at Resources/Sprites/treasure");

//         // Shop UI icon for "Buy Random Relic" button
//         shopBuyRelicIcon = Resources.Load<Sprite>("Sprites/UI/QuestionMark");
//         if (shopBuyRelicIcon == null) Debug.LogWarning("Shop icon not found at Resources/Sprites/UI/QuestionMark");

//         var eq = GameObject.Find("Canvas/InventoryPanel/EquippedRoot");
//         if (eq == null) Debug.LogError("EquippedRoot not found at Canvas/InventoryPanel/EquippedRoot");
//         invEquippedRoot = eq != null ? eq.transform : null;

//         var bp = GameObject.Find("Canvas/InventoryPanel/BackpackRoot");
//         if (bp == null) Debug.LogError("BackpackRoot not found at Canvas/InventoryPanel/BackpackRoot");
//         invBackpackRoot = bp != null ? bp.transform : null;

//         var ss = GameObject.Find("Canvas/InventoryPanel/SafeStashRoot");
//         if (ss == null) Debug.LogError("SafeStashRoot not found at Canvas/InventoryPanel/SafeStashRoot");
//         invSafeStashRoot = ss != null ? ss.transform : null;

//         invSlotTemplate = Resources.Load<InventorySlotUI>("Prefabs/UI/SlotTemplate");
//         if (invSlotTemplate == null) Debug.LogError("SlotTemplate prefab not found at Resources/Prefabs/UI/SlotTemplate");

//         var sprites = Resources.LoadAll<Sprite>("Sprites/doorSlamItems");
//         if (sprites == null || sprites.Length == 0) Debug.LogError("No sprites loaded from Resources/Sprites/doorSlamItems");
//         itemSpriteMap = new Dictionary<string, Sprite>(sprites != null ? sprites.Length * 2 : 0);
//         var re = new Regex(@"^(?:.+_)?(\d+)$");
//         for (int i = 0; i < (sprites?.Length ?? 0); i++)
//         {
//             var sp = sprites[i];
//             // Register with sheet prefix
//             var m = re.Match(sp.name);
//             if (m.Success)
//             {
//                 var id = m.Groups[1].Value;
//                 itemSpriteMap[$"doorSlamItems_{id}"] = sp;
//             }
//         }

//         // Load relic icons from Sprites/icons
//         var relicSprites = Resources.LoadAll<Sprite>("Sprites/icons");
//         if (relicSprites == null || relicSprites.Length == 0) Debug.LogWarning("No sprites loaded from Resources/Sprites/icons for relics");
//         for (int i = 0; i < (relicSprites?.Length ?? 0); i++)
//         {
//             var sp = relicSprites[i];
//             var m = re.Match(sp.name);
//             if (m.Success)
//             {
//                 var id = m.Groups[1].Value;
//                 itemSpriteMap[$"icons_{id}"] = sp;
//             }
//         }

//         // Load starting items sprites from Sprites/starting_items
//         var startingSprites = Resources.LoadAll<Sprite>("Sprites/starting_items");
//         if (startingSprites != null)
//         {
//             for (int i = 0; i < startingSprites.Length; i++)
//             {
//                 var sp = startingSprites[i];
//                 var m = re.Match(sp.name);
//                 if (m.Success)
//                 {
//                     var id = m.Groups[1].Value;
//                     // Use "starting_items_" prefix
//                     itemSpriteMap[$"starting_items_{id}"] = sp;
//                 }
//             }
//         }

//         // Enchanted item materials already loaded above

//         var dsb = GameObject.Find("Canvas/DoorSlamButton");
//         if (dsb == null) Debug.LogError("DoorSlamButton not found at Canvas/DoorSlamButton");
//         doorSlamButton = dsb;
//         if (doorSlamButton != null)
//         {
//             var rt = doorSlamButton.GetComponent<RectTransform>();
//             doorAnimator = doorSlamButton.GetComponent<DoorAnimator>();
//             if (doorAnimator == null)
//             {
//                 doorAnimator = doorSlamButton.AddComponent<DoorAnimator>();
//             }
//             doorAnimator.Configure(rt);
//         }

//         var lr = GameObject.Find("Canvas/LootPanel/LootRoot");
//         if (lr == null) Debug.LogError("LootRoot not found at Canvas/LootRoot");
//         lootRoot = lr != null ? lr.transform : null;

//         // Loot panel root for activation
//         var lp = GameObject.Find("Canvas/LootPanel");
//         if (lp == null) Debug.LogError("LootPanel not found at Canvas/LootPanel");
//         lootPanel = lp;

//         // Encounter Select panel and options
//         var esp = GameObject.Find("Canvas/EncounterSelect");
//         if (esp == null) Debug.LogError("EncounterSelect panel not found at Canvas/EncounterSelect");
//         encounterSelectPanel = esp;
//         if (encounterSelectPanel != null)
//         {
//             var o1 = encounterSelectPanel.transform.Find("OptionOne"); if (o1 == null) Debug.LogError("OptionOne button not found under Canvas/EncounterSelect/OptionOne");
//             var o2 = encounterSelectPanel.transform.Find("OptionTwo"); if (o2 == null) Debug.LogError("OptionTwo button not found under Canvas/EncounterSelect/OptionTwo");
//             optionOneButton = o1 != null ? o1.GetComponent<UnityEngine.UI.Button>() : null;
//             optionTwoButton = o2 != null ? o2.GetComponent<UnityEngine.UI.Button>() : null;
//             optionOneText = optionOneButton != null ? optionOneButton.GetComponentInChildren<TMPro.TextMeshProUGUI>(true) : null;
//             optionTwoText = optionTwoButton != null ? optionTwoButton.GetComponentInChildren<TMPro.TextMeshProUGUI>(true) : null;
//         }

//         // Shop panel under Canvas/ShopPanel
//         var shp = GameObject.Find("Canvas/ShopPanel");
//         if (shp == null) Debug.LogError("ShopPanel not found at Canvas/ShopPanel");
//         shopPanel = shp;
//         if (shopPanel != null)
//         {
//             var i1 = shopPanel.transform.Find("ItemOne"); if (i1 == null) Debug.LogError("ItemOne not found under Canvas/ShopPanel");
//             var i2 = shopPanel.transform.Find("ItemTwo"); if (i2 == null) Debug.LogError("ItemTwo not found under Canvas/ShopPanel");
//             var i3 = shopPanel.transform.Find("ItemThree"); if (i3 == null) Debug.LogError("ItemThree not found under Canvas/ShopPanel");
//             var c1 = i1 != null ? i1.Find("Cost") : null; if (c1 == null) Debug.LogError("Cost text not found under ItemOne");
//             var c2 = i2 != null ? i2.Find("Cost") : null; if (c2 == null) Debug.LogError("Cost text not found under ItemTwo");
//             var c3 = i3 != null ? i3.Find("Cost") : null; if (c3 == null) Debug.LogError("Cost text not found under ItemThree");
//             shopItemOneButton = i1 != null ? i1.GetComponent<UnityEngine.UI.Button>() : null;
//             shopItemTwoButton = i2 != null ? i2.GetComponent<UnityEngine.UI.Button>() : null;
//             shopItemThreeButton = i3 != null ? i3.GetComponent<UnityEngine.UI.Button>() : null;
//             shopItemOneImage = i1 != null ? i1.GetComponent<UnityEngine.UI.Image>() : null;
//             shopItemTwoImage = i2 != null ? i2.GetComponent<UnityEngine.UI.Image>() : null;
//             shopItemThreeImage = i3 != null ? i3.GetComponent<UnityEngine.UI.Image>() : null;
//             shopItemOneCostText = c1 != null ? c1.GetComponent<TMPro.TextMeshProUGUI>() : null;
//             shopItemTwoCostText = c2 != null ? c2.GetComponent<TMPro.TextMeshProUGUI>() : null;
//             shopItemThreeCostText = c3 != null ? c3.GetComponent<TMPro.TextMeshProUGUI>() : null;
//             var cont = shopPanel.transform.Find("Continue"); if (cont == null) Debug.LogError("Continue button not found under Canvas/ShopPanel/Continue");
//             shopContinueButton = cont != null ? cont.GetComponent<UnityEngine.UI.Button>() : null;
//         }

//         // Campfire panel under Canvas/Campfire
//         var cfp = GameObject.Find("Canvas/Campfire");
//         if (cfp == null) Debug.LogError("Campfire panel not found at Canvas/Campfire");
//         campfirePanel = cfp;
//         if (campfirePanel != null)
//         {
//             campfireUI = campfirePanel.GetComponent<CampfireUI>();
//             if (campfireUI == null) Debug.LogError("CampfireUI component not found on Campfire panel");

//             // Buttons under Campfire: Heal and Upgrade
//             var heal = campfirePanel.transform.Find("Heal"); if (heal == null) Debug.LogError("Heal button not found under Canvas/Campfire/Heal");
//             var upg = campfirePanel.transform.Find("Upgrade"); if (upg == null) Debug.LogError("Upgrade button not found under Canvas/Campfire/Upgrade");
//             campfireHealButton = heal != null ? heal.GetComponent<UnityEngine.UI.Button>() : null;
//             campfireUpgradeButton = upg != null ? upg.GetComponent<UnityEngine.UI.Button>() : null;

//             // Upgrade text under Campfire/UpgradeText
//             var upgText = campfirePanel.transform.Find("UpgradeText"); if (upgText == null) Debug.LogError("UpgradeText not found under Canvas/Campfire/UpgradeText");
//             campfireUpgradeText = upgText != null ? upgText.GetComponent<TextMeshProUGUI>() : null;

//             // Ensure Campfire is not visible by default; GameManager enables it explicitly when needed
//             campfirePanel.SetActive(false);
//         }

//         // PlayerStats texts
//         var ps = GameObject.Find("Canvas/PlayerStats");
//         if (ps == null) Debug.LogError("PlayerStats root not found at Canvas/InventoryPanel/PlayerStats");
//         if (ps != null)
//         {
//             var pstr = ps.transform;
//             var c = pstr.Find("Crit%"); if (c == null) Debug.LogError("Crit% Text under PlayerStats not found");
//             var cd = pstr.Find("CritDmg"); if (cd == null) Debug.LogError("CritDmg Text under PlayerStats not found");
//             var bd = pstr.Find("BaseDamage"); if (bd == null) Debug.LogError("BaseDamage Text under PlayerStats not found");
//             var dg = pstr.Find("Dodge%"); if (dg == null) Debug.LogError("Dodge% Text under PlayerStats not found");
//             var dr = pstr.Find("DamageReduction"); if (dr == null) Debug.LogError("DamageReduction Text under PlayerStats not found");
//             var drPct = pstr.Find("DamageReduction%"); if (drPct == null) Debug.LogError("DamageReduction% Text under PlayerStats not found");
//             var qe = pstr.Find("QualityEffect"); if (qe == null) Debug.LogError("QualityEffect Text under PlayerStats not found");
//             var g = pstr.Find("Gold"); if (g == null) Debug.LogError("Gold Text under PlayerStats not found");
//             var ec = pstr.Find("Encounter"); if (ec == null) Debug.LogError("Encounter Text under PlayerStats not found");
//             var rt = pstr.Find("Retaliation"); if (rt == null) Debug.LogError("Retaliation Text under PlayerStats not found");
//             statsCritPctText = c != null ? c.GetComponent<TextMeshProUGUI>() : null;
//             statsCritDmgText = cd != null ? cd.GetComponent<TextMeshProUGUI>() : null;
//             statsBaseDamageText = bd != null ? bd.GetComponent<TextMeshProUGUI>() : null;
//             statsDodgePctText = dg != null ? dg.GetComponent<TextMeshProUGUI>() : null;
//             statsDamageReductionText = dr != null ? dr.GetComponent<TextMeshProUGUI>() : null;
//             statsDamageReductionPctText = drPct != null ? drPct.GetComponent<TextMeshProUGUI>() : null;
//             statsQualityEffectText = qe != null ? qe.GetComponent<TextMeshProUGUI>() : null;
//             statsGoldText = g != null ? g.GetComponent<TextMeshProUGUI>() : null;
//             statsEncounterText = ec != null ? ec.GetComponent<TextMeshProUGUI>() : null;
//             statsRetaliationText = rt != null ? rt.GetComponent<TextMeshProUGUI>() : null;

//             // Attach stat tooltip triggers
//             WireStatTooltip(c, "Crit%");
//             WireStatTooltip(cd, "CritDmg");
//             WireStatTooltip(bd, "BaseDamage");
//             WireStatTooltip(dg, "Dodge%");
//             WireStatTooltip(dr, "DamageReduction");
//             WireStatTooltip(drPct, "DamageReduction%");
//             WireStatTooltip(qe, "QualityEffect");
//             WireStatTooltip(g, "Gold");
//             WireStatTooltip(ec, "Encounter");
//             WireStatTooltip(rt, "Retaliation");

//             // Quality breakdown UI under PlayerStats/Quality
//             var qRoot = pstr.Find("Quality"); if (qRoot == null) Debug.LogError("Quality root under PlayerStats not found");
//             if (qRoot != null)
//             {
//                 var itemTypesRoot = qRoot.Find("ItemTypes"); if (itemTypesRoot == null) Debug.LogError("ItemTypes under PlayerStats/Quality not found");
//                 var itemMaterialsRoot = qRoot.Find("ItemMaterials"); if (itemMaterialsRoot == null) Debug.LogError("ItemMaterials under PlayerStats/Quality not found");

//                 const int qualitySlots = 6;
//                 statsQualityTypeTexts = new TextMeshProUGUI[qualitySlots];
//                 statsQualityMaterialTexts = new TextMeshProUGUI[qualitySlots];

//                 for (int i = 0; i < qualitySlots; i++)
//                 {
//                     var typeChild = itemTypesRoot != null ? itemTypesRoot.Find($"QualityText{i}") : null;
//                     if (typeChild == null) Debug.LogError($"QualityText{i} under PlayerStats/Quality/ItemTypes not found");
//                     statsQualityTypeTexts[i] = typeChild != null ? typeChild.GetComponent<TextMeshProUGUI>() : null;

//                     var matChild = itemMaterialsRoot != null ? itemMaterialsRoot.Find($"QualityText{i}") : null;
//                     if (matChild == null) Debug.LogError($"QualityText{i} under PlayerStats/Quality/ItemMaterials not found");
//                     statsQualityMaterialTexts[i] = matChild != null ? matChild.GetComponent<TextMeshProUGUI>() : null;
//                 }
//             }
//         }

//         // Enemy panel under Canvas/Enemy with children Name, Health, Damage
//         var enemyPanelGO = GameObject.Find("Canvas/Enemy");
//         if (enemyPanelGO == null) Debug.LogError("Enemy panel not found at Canvas/Enemy");
//         enemyPanel = enemyPanelGO;
//         if (enemyPanelGO != null)
//         {
//             var et = enemyPanelGO.transform;
//             var n = et.Find("Name"); if (n == null) Debug.LogError("Enemy Name text not found under Canvas/Enemy/Name");
//             var ed = et.Find("Damage"); if (ed == null) Debug.LogError("Enemy Damage text not found under Canvas/Enemy/Damage");
//             var ep = et.Find("Portrait"); // optional image slot
//             var ebar = et.Find("HealthBar"); // expected enemy health bar
//             enemyNameText = n != null ? n.GetComponent<TextMeshProUGUI>() : null;
//             enemyDamageText = ed != null ? ed.GetComponent<TextMeshProUGUI>() : null;
//             enemyPortrait = ep != null ? ep.GetComponent<UnityEngine.UI.Image>() : null;
//             enemyHealthBar = ebar != null ? ebar.GetComponent<HealthBarUI>() : null;
//         }

//         // Player portrait and health bar under new paths
//         var ppor = GameObject.Find("Canvas/PlayerStats/Portrait");
//         if (ppor == null) Debug.LogError("Player portrait not found at Canvas/PlayerStats/Portrait");
//         playerPortrait = ppor != null ? ppor.GetComponent<UnityEngine.UI.Image>() : null;
//         if (playerPortrait != null)
//         {
//             var sp = Resources.Load<Sprite>("Sprites/player");
//             if (sp == null) Debug.LogWarning("Player sprite not found at Resources/Sprites/player");
//             playerPortrait.sprite = sp;
//             playerPortrait.enabled = (sp != null);
//         }

//         var phb = GameObject.Find("Canvas/PlayerStats/HealthBar");
//         if (phb == null) Debug.LogError("PlayerHealthBar not found at Canvas/PlayerStats/HealthBar");
//         playerHealthBar = phb != null ? phb.GetComponent<HealthBarUI>() : null;

//         // Starting shield text under Canvas/Player/ShieldAtCombatStart
//         var shieldText = GameObject.Find("Canvas/PlayerStats/ShieldAtCombatStart");
//         statsStartingShieldText = shieldText != null ? shieldText.GetComponent<TextMeshProUGUI>() : null;
//         WireStatTooltip(shieldText != null ? shieldText.transform : null, "StartingShield");

//         // Floating damage text pool under Canvas/DamageTextPool (auto-create if missing)
//         var dtp = GameObject.Find("Canvas/DamageTextPool");
//         if (dtp == null)
//         {
//             var canvas = GameObject.Find("Canvas");
//             if (canvas != null)
//             {
//                 dtp = new GameObject("DamageTextPool");
//                 dtp.transform.SetParent(canvas.transform, false);
//             }
//         }
//         damageTextPool = dtp != null ? dtp.GetComponent<DamageTextPool>() : null;
//         if (damageTextPool == null && dtp != null)
//         {
//             damageTextPool = dtp.AddComponent<DamageTextPool>();
//         }
//         // Bind damage text anchors to portraits if available
//         if (damageTextPool != null)
//         {
//             damageTextPool.playerAnchor = playerPortrait != null ? playerPortrait.rectTransform : null;
//             damageTextPool.enemyAnchor = enemyPortrait != null ? enemyPortrait.rectTransform : null;
//         }

//         // Event Panel under Canvas/Event
//         var eventPanelGO = GameObject.Find("Canvas/Event");
//         if (eventPanelGO == null) Debug.LogError("Event panel not found at Canvas/Event");
//         if (eventPanelGO != null)
//         {
//             eventPanelUI = eventPanelGO.GetComponent<EventPanelUI>();

//             // Bind children
//             var t = eventPanelGO.transform;
//             var title = t.Find("Title");
//             var opt1 = t.Find("Option1");
//             var opt2 = t.Find("Option2");
//             var opt3 = t.Find("Option3");

//             if (title != null) eventPanelUI.TitleText = title.GetComponent<TextMeshProUGUI>();

//             if (opt1 != null)
//             {
//                 eventPanelUI.Option1Button = opt1.GetComponent<Button>();
//                 eventPanelUI.Option1Text = opt1.GetComponentInChildren<TextMeshProUGUI>();
//             }
//             if (opt2 != null)
//             {
//                 eventPanelUI.Option2Button = opt2.GetComponent<Button>();
//                 eventPanelUI.Option2Text = opt2.GetComponentInChildren<TextMeshProUGUI>();
//             }
//             if (opt3 != null)
//             {
//                 eventPanelUI.Option3Button = opt3.GetComponent<Button>();
//                 eventPanelUI.Option3Text = opt3.GetComponentInChildren<TextMeshProUGUI>();
//             }

//             // Bind generic Event Item Slot under Canvas/Event/ItemSlot
//             var itemSlotTr = eventPanelGO.transform.Find("ItemSlot");
//             if (itemSlotTr == null) Debug.LogError("Event ItemSlot not found at Canvas/Event/ItemSlot");
//             eventItemSlot = itemSlotTr != null ? itemSlotTr.GetComponent<EventItemSlot>() : null;

//             eventPanelGO.SetActive(false); // Hide by default
//         }
//     }

//     private void WireStatTooltip(Transform statTransform, string statKey)
//     {
//         if (statTransform == null) return;
//         var trigger = statTransform.gameObject.AddComponent<StatTooltipTrigger>();
//         trigger.Configure(statKey);
//         var tmp = statTransform.GetComponent<TMPro.TextMeshProUGUI>();
//         if (tmp != null) tmp.raycastTarget = true;
//     }
// }
