using UnityEngine;

public class GameManager : MonoBehaviour
{
    private Referencer refs;
    private int completedNodes = 0;
    private const int TOTAL_NODES = 10;
    private bool affinityChosen = false;
    private bool weaponChosen = false;

    void Start()
    {
        refs = FindFirstObjectByType<Referencer>();
        UpdateNodeCounter();
        
        StartAffinitySelection();
    }

    private void StartAffinitySelection()
    {
        if (refs != null && refs.playerController != null)
        {
            refs.playerController.SetCanMove(false);
        }

        if (refs != null && refs.affinitySelectionUI != null)
        {
            refs.affinitySelectionUI.Show(OnAffinityChosen);
        }
        else
        {
            Debug.LogError("[GameManager] AffinitySelectionUI not found");
            affinityChosen = true;
            if (refs != null && refs.playerController != null)
            {
                refs.playerController.SetCanMove(true);
            }
        }
    }

    private void OnAffinityChosen(Element element)
    {
        affinityChosen = true;

        if (refs != null && refs.player != null)
        {
            refs.player.SetAffinity(element);
        }

        Debug.Log($"[GameManager] Affinity chosen: {element}");
        
        StartWeaponSelection();
    }

    private void StartWeaponSelection()
    {
        if (refs != null && refs.weaponSelectionUI != null)
        {
            refs.weaponSelectionUI.Show(OnWeaponChosen);
        }
        else
        {
            Debug.LogError("[GameManager] WeaponSelectionUI not found");
            weaponChosen = true;
            if (refs != null && refs.playerController != null)
            {
                refs.playerController.SetCanMove(true);
            }
        }
    }

    private void OnWeaponChosen(WeaponData weapon)
    {
        weaponChosen = true;

        if (refs != null && refs.player != null)
        {
            refs.player.EquipWeapon(weapon);
        }

        if (refs != null && refs.playerController != null)
        {
            refs.playerController.SetCanMove(true);
        }

        Debug.Log($"[GameManager] Run started with {refs.player.GetAffinity()} affinity and {weapon.DisplayName}");
    }

    public bool HasAffinityBeenChosen() => affinityChosen;
    public bool HasWeaponBeenChosen() => weaponChosen;

    public void OnNodeCompleted()
    {
        completedNodes++;
        UpdateNodeCounter();

        if (completedNodes >= TOTAL_NODES)
        {
            ShowVictory();
        }
    }

    private void UpdateNodeCounter()
    {
        if (refs != null && refs.nodeCounterText != null)
        {
            refs.nodeCounterText.text = $"Nodes Completed: {completedNodes}/{TOTAL_NODES}";
        }
    }

    private void ShowVictory()
    {
        if (refs != null && refs.victoryPanel != null)
        {
            refs.victoryPanel.SetActive(true);
        }

        if (refs != null && refs.playerController != null)
        {
            refs.playerController.SetCanMove(false);
        }
    }
}
