using UnityEngine;
using System.Collections;

public class DraggablePiece : MonoBehaviour
{
    [Header("Snapping Settings")]
    public Transform snapTarget;
    public float snapDistance = 0.5f;
    public float snapSpeed = 10f;

    [Header("Visual & Audio Feedback")]
    public AudioSource snapSound;
    public ParticleSystem snapEffect;
    public SnapHighlight snapHighlight;

    private Vector3 originalPosition;
    private bool isPlaced = false;
    private bool isDragging = false;
    private bool timerStarted = false;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        originalPosition = transform.position;
        if (snapHighlight != null)
            snapHighlight.Highlight(false);
    }

    void OnMouseDown()
    {
        if (isPlaced) return;

        // Start game timer only on the first drag
        if (!timerStarted)
        {
            HouseAssemblyManager.Instance.StartTimerOnce();
            timerStarted = true;
        }

        isDragging = true;
    }

    void OnMouseDrag()
    {
        if (!isDragging || isPlaced) return;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = cam.WorldToScreenPoint(transform.position).z;
        Vector3 target = cam.ScreenToWorldPoint(mousePos);

        // Keep Y constant so piece doesn’t float or dip
        transform.position = new Vector3(target.x, originalPosition.y, target.z);

        // Highlight snap area when close
        float distance = Vector3.Distance(transform.position, snapTarget.position);
        if (snapHighlight != null)
            snapHighlight.Highlight(distance <= snapDistance);
    }

    void OnMouseUp()
    {
        if (isPlaced) return;

        isDragging = false;
        float distance = Vector3.Distance(transform.position, snapTarget.position);

        if (distance <= snapDistance)
        {
            if (snapHighlight != null) snapHighlight.Highlight(false);
            StartCoroutine(SmoothSnap());
        }
        else
        {
            if (snapHighlight != null) snapHighlight.Highlight(false);
            StartCoroutine(ReturnToStart());
        }
    }

    IEnumerator SmoothSnap()
    {
        isPlaced = true;
        float t = 0;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (t < 1)
        {
            t += Time.deltaTime * snapSpeed;
            transform.position = Vector3.Lerp(startPos, snapTarget.position, t);
            transform.rotation = Quaternion.Slerp(startRot, snapTarget.rotation, t);
            yield return null;
        }

        transform.position = snapTarget.position;
        transform.rotation = snapTarget.rotation;

        if (snapSound) snapSound.Play();
        if (snapEffect) snapEffect.Play();

        // Notify that one piece is correctly placed
        HouseAssemblyManager.Instance.AddPlacedPart();

        // Disable collider so it stays locked
        GetComponent<Collider>().enabled = false;
    }

    IEnumerator ReturnToStart()
    {
        float t = 0;
        Vector3 startPos = transform.position;
        while (t < 1)
        {
            t += Time.deltaTime * (snapSpeed / 2);
            transform.position = Vector3.Lerp(startPos, originalPosition, t);
            yield return null;
        }
        transform.position = originalPosition;
    }
}
