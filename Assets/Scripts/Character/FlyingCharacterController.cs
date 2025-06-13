using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FlyingCharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;          // Movement speed in horizontal directions
    public float flightSpeed = 3f;        // Additional speed factor for vertical movement
    public float mouseSensitivity = 2f;   // Mouse sensitivity for looking around

    private CharacterController controller;
    private float verticalLookRotation = 0f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        // Optionally lock the cursor for a better flying experience
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    private void HandleMouseLook()
    {
        // Horizontal rotation (yaw)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(0, mouseX, 0);

        // Vertical rotation (pitch)
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        verticalLookRotation -= mouseY;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);

        // If you have a child Camera attached to this GameObject, rotate it
        if (Camera.main != null)
        {
            Camera.main.transform.localRotation = Quaternion.Euler(verticalLookRotation, 0, 0);
        }
    }

    private void HandleMovement()
    {
        // Get horizontal movement input (WASD)
        float moveX = Input.GetAxis("Horizontal"); // A/D
        float moveZ = Input.GetAxis("Vertical");   // W/S

        // Create a movement vector relative to the player's orientation
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        // Vertical movement for flying.
        // Press Space to go up and Left Control or C to go down.
        if (Input.GetKey(KeyCode.Space))
        {
            move += Vector3.up;
        }
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C))
        {
            move += Vector3.down;
        }

        // Normalize to prevent faster diagonal movement.
        if (move.magnitude > 1f)
        {
            move.Normalize();
        }

        // Apply movement speed. Vertical movement is optionally scaled separately with flightSpeed.
        Vector3 horizontalMove = new Vector3(move.x, 0, move.z) * moveSpeed;
        float verticalMove = move.y * flightSpeed;
        Vector3 finalMove = horizontalMove + Vector3.up * verticalMove;

        // Use CharacterController.Move to move the player. Time.deltaTime ensures frame-rate–independent movement.
        controller.Move(finalMove * Time.deltaTime);
    }
}
