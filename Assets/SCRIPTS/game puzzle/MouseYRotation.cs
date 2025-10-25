using UnityEngine;

public class MouseYRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 5f; // Rotation sensitivity

    void Update()
    {
        // Rotate while left mouse button is held
        if (Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X");

            // Flip direction so dragging right rotates right
            float rotationY = -mouseX * rotationSpeed;

            // Rotate only on Y-axis
            transform.Rotate(0f, rotationY, 0f, Space.World);
        }
    }
}