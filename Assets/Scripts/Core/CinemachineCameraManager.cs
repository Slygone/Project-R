using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Manages two Cinemachine cameras: one for free roam (follows player)
/// and one for combat (centers on the combat arena).
/// Switching is handled via Cinemachine's priority system.
/// CinemachineBrain on the Main Camera handles blending automatically.
/// </summary>
public class CinemachineCameraManager : MonoBehaviour
{
    [Header("Camera References")]
    [Tooltip("Cinemachine camera that follows the player during free roam.")]
    [SerializeField] private CinemachineCamera freeroamCamera;
    
    [Tooltip("Cinemachine camera that centers on the combat arena.")]
    [SerializeField] private CinemachineCamera combatCamera;
    
    [Header("Priority")]
    [Tooltip("Priority assigned to the currently active camera.")]
    [SerializeField] private int activePriority = 20;
    
    [Tooltip("Priority assigned to the currently inactive camera.")]
    [SerializeField] private int inactivePriority = 10;
    
    // Runtime target transform for the combat camera to follow
    private Transform combatTarget;
    
    private void Awake()
    {
        // Create an empty transform to serve as the combat camera's follow target
        var targetObj = new GameObject("CombatCameraTarget");
        targetObj.transform.SetParent(transform);
        combatTarget = targetObj.transform;
        
        // Freeroam starts active, combat starts inactive
        ActivateFreeroam();
    }
    
    /// <summary>
    /// Set the follow target for the freeroam camera (typically the player).
    /// Call this once the player is available (e.g. from Referencer on Awake).
    /// </summary>
    public void SetFreeroamTarget(Transform target)
    {
        if (freeroamCamera == null)
        {
            Debug.LogWarning("[CinemachineCameraManager] FreeroamCamera is not assigned.");
            return;
        }
        
        freeroamCamera.Follow = target;
    }
    
    /// <summary>
    /// Switch to the combat camera, centered on the given world position.
    /// The combat camera's CinemachineFollow offset determines the final view angle.
    /// </summary>
    public void EnterCombat(Vector3 combatCenter)
    {
        combatTarget.position = combatCenter;
        
        if (combatCamera != null)
        {
            combatCamera.Follow = combatTarget;
        }
        else
        {
            Debug.LogWarning("[CinemachineCameraManager] CombatCamera is not assigned.");
        }
        
        ActivateCombat();
    }
    
    /// <summary>
    /// Switch back to the freeroam camera.
    /// CinemachineBrain will blend smoothly between the two cameras.
    /// </summary>
    public void ExitCombat()
    {
        ActivateFreeroam();
    }
    
    private void ActivateFreeroam()
    {
        if (freeroamCamera != null) freeroamCamera.Priority = activePriority;
        if (combatCamera != null) combatCamera.Priority = inactivePriority;
    }
    
    private void ActivateCombat()
    {
        if (combatCamera != null) combatCamera.Priority = activePriority;
        if (freeroamCamera != null) freeroamCamera.Priority = inactivePriority;
    }
}
