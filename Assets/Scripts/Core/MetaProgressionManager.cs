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
    /// Award +1 ascension level to a character and save.
    /// </summary>
    public void AwardAscension(int characterId, string characterName = "")
    {
        var progress = GetProgress(characterId);
        progress.AscensionLevel++;
        SaveProgress();
        
        Debug.Log($"[Meta] AscensionGained | characterId={characterId} name={characterName} newLevel={progress.AscensionLevel}");
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
                Debug.Log($"[Meta] ProgressLoaded | characters={saveData.CharacterProgress.Count}");
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
