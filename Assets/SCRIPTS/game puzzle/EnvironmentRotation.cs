using UnityEngine;

public class EnvironmentRotation : MonoBehaviour
{
    public float rotationSpeed = 0.3f;  // Adjust for sensitivity
    public float minTilt = -30f;        // Minimum vertical tilt
    public float maxTilt = 45f;         // Maximum vertical tilt

    private Vector3 lastMousePosition;
    private bool isDragging = false;
    private float currentTilt = 0f;     // Tracks how far we've tilted

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        HandleMouseRotation();
#elif UNITY_ANDROID || UNITY_IOS
        HandleTouchRotation();
#endif
    }

    void HandleMouseRotation()
    {
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
            isDragging = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            float rotationY = delta.x * rotationSpeed;
            float rotationX = -delta.y * rotationSpeed;

            // Rotate horizontally
            transform.Rotate(Vector3.up, rotationY, Space.World);

            // Handle vertical tilt with clamp
            currentTilt = Mathf.Clamp(currentTilt + rotationX, minTilt, maxTilt);
            transform.localEulerAngles = new Vector3(currentTilt, transform.localEulerAngles.y, 0);

            lastMousePosition = Input.mousePosition;
        }
    }

    void HandleTouchRotation()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                lastMousePosition = touch.position;
                isDragging = true;
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                Vector2 delta = touch.deltaPosition;
                float rotationY = delta.x * rotationSpeed;
                float rotationX = -delta.y * rotationSpeed;

                transform.Rotate(Vector3.up, rotationY, Space.World);

                // Clamp vertical tilt
                currentTilt = Mathf.Clamp(currentTilt + rotationX, minTilt, maxTilt);
                transform.localEulerAngles = new Vector3(currentTilt, transform.localEulerAngles.y, 0);
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                isDragging = false;
            }
        }
    }
}
