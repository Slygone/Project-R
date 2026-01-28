using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public bool useInitialOffset = true;
    public Vector3 offset = new Vector3(0f, 12f, -10f);
    public float smoothTime = 0.15f;

    private Vector3 velocity;
    private bool offsetInitialized;
    private bool followEnabled = true;

    void Start()
    {
        TryResolveTarget();

        if (useInitialOffset && target != null && !offsetInitialized)
        {
            offset = transform.position - target.position;
            offsetInitialized = true;
        }
    }

    void LateUpdate()
    {
        // Don't follow if disabled (e.g. during combat)
        if (!followEnabled) return;
        
        if (target == null)
        {
            TryResolveTarget();
            if (target == null) return;

            if (useInitialOffset && !offsetInitialized)
            {
                offset = transform.position - target.position;
                offsetInitialized = true;
            }
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

        
    }
    
    /// <summary>
    /// Enable camera following (normal free roam behavior)
    /// </summary>
    public void EnableFollow()
    {
        followEnabled = true;
    }
    
    /// <summary>
    /// Disable camera following (for combat - camera stays in place)
    /// </summary>
    public void DisableFollow()
    {
        followEnabled = false;
    }
    
    /// <summary>
    /// Move camera to look at a specific world position (used for combat centering)
    /// </summary>
    public void SetCombatPosition(Vector3 centerPosition)
    {
        transform.position = centerPosition + offset;
        velocity = Vector3.zero; // Reset velocity so it doesn't drift
    }

    private void TryResolveTarget()
    {
        if (target != null) return;

        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            target = pc.transform;
            return;
        }

        var player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            target = player.transform;
        }
    }
}
