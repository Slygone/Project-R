using UnityEngine;
using Core;

namespace Characters
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        
        private Rigidbody2D rb;
        private Vector2 movement;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager is missing! Did you create the GameManager object?");
                return;
            }

            if (GameManager.Instance.CurrentState != GameState.FreeRoam)
            {
                movement = Vector2.zero;
                return;
            }

            // Input processing
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            if (moveX != 0 || moveY != 0)
            {
                // Debug.Log($"Input Received: {moveX}, {moveY}");
            }

            movement = new Vector2(moveX, moveY).normalized;
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance.CurrentState == GameState.FreeRoam)
            {
                rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
            }
        }
    }
}
