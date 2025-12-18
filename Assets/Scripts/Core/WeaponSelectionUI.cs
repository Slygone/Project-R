using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponSelectionUI : MonoBehaviour
{
    private GameObject selectionPanel;
    private List<WeaponData> choices = new List<WeaponData>();
    private Action<WeaponData> onWeaponSelected;
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
            Debug.LogError("[WeaponSelectionUI] Canvas not found");
            return;
        }

        selectionPanel = CreateSelectionPanel(canvas.transform);
        selectionPanel.SetActive(false);
    }

    private GameObject CreateSelectionPanel(Transform parent)
    {
        var panel = new GameObject("WeaponSelectionPanel");
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
        titleText.text = "CHOOSE YOUR WEAPON";
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
        subtitleText.text = "Your weapon determines your skills and adds to your base damage";
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.fontSize = 20;
        subtitleText.color = new Color(0.7f, 0.7f, 0.7f);

        return panel;
    }

    public void Show(Action<WeaponData> callback)
    {
        onWeaponSelected = callback;
        choices = GetRandomWeapons(3);
        
        ClearChoiceButtons();
        CreateChoiceButtons();
        
        selectionPanel.SetActive(true);
        isActive = true;
    }

    private List<WeaponData> GetRandomWeapons(int count)
    {
        var allWeapons = new List<WeaponData>(DataCache.Weapons);
        var result = new List<WeaponData>();

        while (result.Count < count && allWeapons.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, allWeapons.Count);
            result.Add(allWeapons[index]);
            allWeapons.RemoveAt(index);
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
            CreateWeaponButton(container.transform, choices[i]);
        }
    }

    private void CreateWeaponButton(Transform parent, WeaponData weapon)
    {
        var btnObj = new GameObject($"Btn_{weapon.DisplayName}");
        btnObj.transform.SetParent(parent, false);

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        
        WeaponData capturedWeapon = weapon;
        btn.onClick.AddListener(() => OnChoiceClicked(capturedWeapon));

        var layout = btnObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.padding = new RectOffset(15, 15, 20, 20);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(btnObj.transform, false);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = weapon.DisplayName.ToUpper();
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = 28;
        nameText.color = new Color(1f, 0.85f, 0.2f);
        var nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.preferredHeight = 40;

        var typeObj = new GameObject("Type");
        typeObj.transform.SetParent(btnObj.transform, false);
        var typeText = typeObj.AddComponent<TextMeshProUGUI>();
        typeText.text = weapon.WeaponType;
        typeText.alignment = TextAlignmentOptions.Center;
        typeText.fontSize = 16;
        typeText.color = new Color(0.6f, 0.6f, 0.6f);
        var typeLayout = typeObj.AddComponent<LayoutElement>();
        typeLayout.preferredHeight = 25;

        var damageObj = new GameObject("Damage");
        damageObj.transform.SetParent(btnObj.transform, false);
        var damageText = damageObj.AddComponent<TextMeshProUGUI>();
        damageText.text = $"+{weapon.Damage} Damage";
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
        skillsText.text = $"{weapon.Skill1}\n{weapon.Skill2}\n{weapon.Skill3}";
        skillsText.alignment = TextAlignmentOptions.Center;
        skillsText.fontSize = 16;
        skillsText.color = new Color(0.7f, 0.85f, 1f);
        var skillsLayout = skillsObj.AddComponent<LayoutElement>();
        skillsLayout.preferredHeight = 70;
    }

    private void OnChoiceClicked(WeaponData weapon)
    {
        selectionPanel.SetActive(false);
        isActive = false;

        Debug.Log($"[WeaponSelectionUI] Player chose {weapon.DisplayName}");
        onWeaponSelected?.Invoke(weapon);
    }

    public bool IsActive() => isActive;
}
