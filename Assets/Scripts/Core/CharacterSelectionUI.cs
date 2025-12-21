using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectionUI : MonoBehaviour
{
    private GameObject selectionPanel;
    private List<CharacterData> choices = new List<CharacterData>();
    private Action<CharacterData> onCharacterSelected;
    private bool isActive = false;

    void Awake()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[CharacterSelectionUI] Canvas not found");
            return;
        }

        selectionPanel = CreateSelectionPanel(canvas.transform);
        selectionPanel.SetActive(false);
    }

    private GameObject CreateSelectionPanel(Transform parent)
    {
        var panel = new GameObject("CharacterSelectionPanel");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.75f);
        titleRect.anchorMax = new Vector2(1, 0.9f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "CHOOSE YOUR CHARACTER";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 42;
        titleText.color = new Color(0.9f, 0.9f, 0.9f);

        var subtitleObj = new GameObject("Subtitle");
        subtitleObj.transform.SetParent(panel.transform, false);
        var subtitleRect = subtitleObj.AddComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0, 0.65f);
        subtitleRect.anchorMax = new Vector2(1, 0.75f);
        subtitleRect.offsetMin = Vector2.zero;
        subtitleRect.offsetMax = Vector2.zero;
        var subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
        subtitleText.text = "Your character determines your skills and combat style";
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.fontSize = 20;
        subtitleText.color = new Color(0.7f, 0.7f, 0.7f);

        return panel;
    }

    public void Show(Action<CharacterData> callback)
    {
        onCharacterSelected = callback;
        choices = GetRandomCharacters(3);
        
        ClearChoiceButtons();
        CreateChoiceButtons();
        
        selectionPanel.SetActive(true);
        isActive = true;

        var refs = FindFirstObjectByType<Referencer>();
        if (refs != null && refs.playerStatsUI != null && refs.playerStatsUI.IsOpen())
        {
            refs.playerStatsUI.UpdateStats();
        }
    }

    private List<CharacterData> GetRandomCharacters(int count)
    {
        var allCharacters = new List<CharacterData>(DataCache.Characters);
        var result = new List<CharacterData>();

        while (result.Count < count && allCharacters.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, allCharacters.Count);
            result.Add(allCharacters[index]);
            allCharacters.RemoveAt(index);
        }

        return result;
    }

    private void ClearChoiceButtons()
    {
        var container = selectionPanel.transform.Find("ChoiceContainer");
        if (container != null)
        {
            Destroy(container.gameObject);
        }
    }

    private void CreateChoiceButtons()
    {
        var container = new GameObject("ChoiceContainer");
        container.transform.SetParent(selectionPanel.transform, false);

        var containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.05f, 0.1f);
        containerRect.anchorMax = new Vector2(0.95f, 0.6f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        var layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        for (int i = 0; i < choices.Count; i++)
        {
            CreateCharacterButton(container.transform, choices[i]);
        }
    }

    private void CreateCharacterButton(Transform parent, CharacterData character)
    {
        var btnObj = new GameObject($"Btn_{character.DisplayName}");
        btnObj.transform.SetParent(parent, false);

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        
        CharacterData capturedCharacter = character;
        btn.onClick.AddListener(() => OnChoiceClicked(capturedCharacter));

        var layout = btnObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.padding = new RectOffset(15, 15, 20, 20);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(btnObj.transform, false);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = character.DisplayName.ToUpper();
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = 28;
        nameText.color = new Color(1f, 0.85f, 0.2f);
        var nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.preferredHeight = 40;

        var statsObj = new GameObject("Stats");
        statsObj.transform.SetParent(btnObj.transform, false);
        var statsText = statsObj.AddComponent<TextMeshProUGUI>();
        statsText.text = $"HP: {character.MaxHealth} | Crit: {character.CritChance}%";
        statsText.alignment = TextAlignmentOptions.Center;
        statsText.fontSize = 16;
        statsText.color = new Color(0.6f, 0.6f, 0.6f);
        var statsLayout = statsObj.AddComponent<LayoutElement>();
        statsLayout.preferredHeight = 25;

        var damageObj = new GameObject("Damage");
        damageObj.transform.SetParent(btnObj.transform, false);
        var damageText = damageObj.AddComponent<TextMeshProUGUI>();
        damageText.text = $"+{character.Damage} Damage";
        damageText.alignment = TextAlignmentOptions.Center;
        damageText.fontSize = 20;
        damageText.color = new Color(1f, 0.4f, 0.4f);
        var damageLayout = damageObj.AddComponent<LayoutElement>();
        damageLayout.preferredHeight = 30;

        var skillsHeaderObj = new GameObject("SkillsHeader");
        skillsHeaderObj.transform.SetParent(btnObj.transform, false);
        var skillsHeaderText = skillsHeaderObj.AddComponent<TextMeshProUGUI>();
        skillsHeaderText.text = "Skills:";
        skillsHeaderText.alignment = TextAlignmentOptions.Center;
        skillsHeaderText.fontSize = 14;
        skillsHeaderText.color = new Color(0.5f, 0.5f, 0.5f);
        var skillsHeaderLayout = skillsHeaderObj.AddComponent<LayoutElement>();
        skillsHeaderLayout.preferredHeight = 20;

        var skillsObj = new GameObject("Skills");
        skillsObj.transform.SetParent(btnObj.transform, false);
        var skillsText = skillsObj.AddComponent<TextMeshProUGUI>();
        skillsText.text = $"{character.Skill1}\n{character.Skill2}\n{character.Skill3}";
        skillsText.alignment = TextAlignmentOptions.Center;
        skillsText.fontSize = 16;
        skillsText.color = new Color(0.7f, 0.85f, 1f);
        var skillsLayout = skillsObj.AddComponent<LayoutElement>();
        skillsLayout.preferredHeight = 70;
    }

    private void OnChoiceClicked(CharacterData character)
    {
        selectionPanel.SetActive(false);
        isActive = false;

        Debug.Log($"[CharacterSelectionUI] Player chose {character.DisplayName}");
        onCharacterSelected?.Invoke(character);

        var refs = FindFirstObjectByType<Referencer>();
        if (refs != null && refs.playerStatsUI != null && refs.playerStatsUI.IsOpen())
        {
            refs.playerStatsUI.UpdateStats();
        }
    }

    public bool IsActive() => isActive;
}
