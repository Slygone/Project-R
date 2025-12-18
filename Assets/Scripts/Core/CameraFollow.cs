using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public bool useInitialOffset = true;
    public Vector3 offset = new Vector3(0f, 12f, -10f);
    public float smoothTime = 0.15f;

    private Vector3 velocity;
    private bool offsetInitialized;

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
