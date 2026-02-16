using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager for character meta-progression (ascension levels, talent selections).
/// Uses PlayerPrefs for persistence.
/// </summary>
public class MetaProgressionManager : MonoBehaviour
{
    private static MetaProgressionManager _instance;
    public static MetaProgressionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("MetaProgressionManager");
                _instance = go.AddComponent<MetaProgressionManager>();
                DontDestroyOnLoad(go);
                _instance.LoadProgress();
            }
            return _instance;
        }
    }
    
    private const string SAVE_KEY = "MetaProgression";
    
    [Serializable]
    private class SaveData
    {
        public List<CharacterProgressData> CharacterProgress = new List<CharacterProgressData>();
        public List<ElementProgressData> ElementProgress = new List<ElementProgressData>();
        public int RegularCores = 0;
        public int AscendedCores = 0;
    }
    
    private SaveData saveData = new SaveData();
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        LoadProgress();
    }
    
    // ===== ESSENCE CORE INVENTORY =====
    
    public int GetRegularCores() => saveData.RegularCores;
    public int GetAscendedCores() => saveData.AscendedCores;
    
    /// <summary>
    /// Add Essence Cores to the player's inventory. Called after combat rewards.
    /// </summary>
    public void AddCores(int regular, int ascended)
    {
        if (regular > 0)
        {
            saveData.RegularCores += regular;
            Debug.Log($"[Meta] CoreGained | type=Regular amount={regular} total={saveData.RegularCores}");
        }
        if (ascended > 0)
        {
            saveData.AscendedCores += ascended;
            Debug.Log($"[Meta] CoreGained | type=Ascended amount={ascended} total={saveData.AscendedCores}");
        }
        if (regular > 0 || ascended > 0)
        {
            SaveProgress();
        }
    }
    
    /// <summary>
    /// Check if the player can afford the given core cost.
    /// </summary>
    public bool CanAfford(int regularCost, int ascendedCost)
    {
        return saveData.RegularCores >= regularCost && saveData.AscendedCores >= ascendedCost;
    }
    
    /// <summary>
    /// Spend cores. Returns false if insufficient funds.
    /// </summary>
    private bool SpendCores(int regularCost, int ascendedCost)
    {
        if (!CanAfford(regularCost, ascendedCost))
            return false;
        
        saveData.RegularCores -= regularCost;
        saveData.AscendedCores -= ascendedCost;
        return true;
    }
    
    // ===== CHARACTER PROGRESSION =====
    
    /// <summary>
    /// Get progress data for a character. Creates default progress if none exists.
    /// </summary>
    public CharacterProgressData GetProgress(int characterId)
    {
        foreach (var p in saveData.CharacterProgress)
        {
            if (p.CharacterID == characterId)
            {
                return p;
            }
        }
        
        // Create new progress for this character
        var newProgress = new CharacterProgressData(characterId);
        saveData.CharacterProgress.Add(newProgress);
        SaveProgress();
        return newProgress;
    }
    
    /// <summary>
    /// Check if a character can be leveled up (has enough cores and not at max level).
    /// </summary>
    public bool CanLevelUpCharacter(int characterId)
    {
        var progress = GetProgress(characterId);
        int nextLevel = progress.AscensionLevel + 1;
        int maxLevel = DataCache.GetCharacterMaxAscensionLevel();
        
        if (progress.AscensionLevel >= maxLevel)
            return false;
        
        var cost = DataCache.GetCharacterAscensionCost(nextLevel);
        if (cost == null) return false;
        
        return CanAfford(cost.RegularCost, cost.AscendedCost);
    }
    
    /// <summary>
    /// Try to level up a character by spending Essence Cores.
    /// Returns true if level-up occurred.
    /// </summary>
    public bool TryLevelUpCharacter(int characterId, string characterName = "")
    {
        var progress = GetProgress(characterId);
        int nextLevel = progress.AscensionLevel + 1;
        int maxLevel = DataCache.GetCharacterMaxAscensionLevel();
        
        if (progress.AscensionLevel >= maxLevel)
            return false;
        
        var cost = DataCache.GetCharacterAscensionCost(nextLevel);
        if (cost == null) return false;
        
        if (!SpendCores(cost.RegularCost, cost.AscendedCost))
            return false;
        
        progress.AscensionLevel = nextLevel;
        SaveProgress();
        
        Debug.Log($"[Meta] CharacterLevelUp | characterId={characterId} name={characterName} newLevel={nextLevel} regularSpent={cost.RegularCost} ascendedSpent={cost.AscendedCost}");
        return true;
    }
    
    /// <summary>
    /// Set the talent choice for a character's tier and save.
    /// </summary>
    public void SetTalentChoice(int characterId, string characterName, int tierIndex, int choice, string perkId)
    {
        var progress = GetProgress(characterId);
        progress.SetTalentChoice(tierIndex, choice);
        SaveProgress();
        
        string choiceStr = choice == 1 ? "A" : "B";
        Debug.Log($"[Meta] TalentSelected | characterId={characterId} name={characterName} tier={tierIndex} choice={choiceStr} perkId={perkId}");
    }
    
    // ===== ELEMENT PROGRESSION =====
    
    /// <summary>
    /// Get progress data for an element. Creates default progress if none exists.
    /// </summary>
    public ElementProgressData GetElementProgress(string elementName)
    {
        foreach (var p in saveData.ElementProgress)
        {
            if (p.ElementName == elementName)
            {
                return p;
            }
        }
        
        // Create new progress for this element
        var newProgress = new ElementProgressData(elementName);
        saveData.ElementProgress.Add(newProgress);
        SaveProgress();
        return newProgress;
    }
    
    /// <summary>
    /// Check if an element can be leveled up (has enough cores and not at max level).
    /// </summary>
    public bool CanLevelUpElement(string elementName)
    {
        var progress = GetElementProgress(elementName);
        int maxLevel = DataCache.GetElementMaxLevel(elementName);
        
        if (progress.AscensionLevel >= maxLevel)
            return false;
        
        int nextLevel = progress.AscensionLevel + 1;
        var cost = DataCache.GetElementalAscensionCost(elementName, nextLevel);
        if (cost == null) return false;
        
        return CanAfford(cost.RegularCost, cost.AscendedCost);
    }
    
    /// <summary>
    /// Try to level up an element by spending Essence Cores.
    /// Returns true if level-up occurred.
    /// </summary>
    public bool TryLevelUpElement(string elementName)
    {
        var progress = GetElementProgress(elementName);
        int maxLevel = DataCache.GetElementMaxLevel(elementName);
        
        if (progress.AscensionLevel >= maxLevel)
            return false;
        
        int nextLevel = progress.AscensionLevel + 1;
        var cost = DataCache.GetElementalAscensionCost(elementName, nextLevel);
        if (cost == null) return false;
        
        if (!SpendCores(cost.RegularCost, cost.AscendedCost))
            return false;
        
        progress.AscensionLevel = nextLevel;
        SaveProgress();
        
        Debug.Log($"[Meta] ElementAscensionLevelUp | element={elementName} newLevel={nextLevel} regularSpent={cost.RegularCost} ascendedSpent={cost.AscendedCost}");
        return true;
    }
    
    // ===== PERSISTENCE =====
    
    /// <summary>
    /// Save all progress to PlayerPrefs.
    /// </summary>
    public void SaveProgress()
    {
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Load progress from PlayerPrefs.
    /// </summary>
    public void LoadProgress()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            try
            {
                saveData = JsonUtility.FromJson<SaveData>(json);
                if (saveData == null)
                {
                    saveData = new SaveData();
                }
                Debug.Log($"[Meta] ProgressLoaded | characters={saveData.CharacterProgress.Count} elements={saveData.ElementProgress.Count} regularCores={saveData.RegularCores} ascendedCores={saveData.AscendedCores}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Meta] Failed to load progress, resetting: {e.Message}");
                saveData = new SaveData();
            }
        }
        else
        {
            saveData = new SaveData();
            Debug.Log("[Meta] No saved progress found, starting fresh");
        }
    }
    
    /// <summary>
    /// Clear all progress (for testing).
    /// </summary>
    public void ClearAllProgress()
    {
        saveData = new SaveData();
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
        Debug.Log("[Meta] All progress cleared");
    }
    
    /// <summary>
    /// Check if any character has an unselected unlocked tier (for potential UI indicators).
    /// </summary>
    public bool HasAnyUnselectedTier()
    {
        foreach (var p in saveData.CharacterProgress)
        {
            if (p.HasUnselectedUnlockedTier())
            {
                return true;
            }
        }
        return false;
    }
}

