using UnityEngine;

public class RandomWalker : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 1.5f;
    public float walkRange = 5f;
    public float rotationOnHit = 46f;
    public float changeDirectionTime = 3f; // how often to pick new direction

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float nextChangeTime;

    private Animator animator;
    private Rigidbody rb;

    private void Start()
    {
        startPosition = transform.position;
        targetPosition = GetNewTarget();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.freezeRotation = true; // stop physics from rotating it
        }
    }

    private void Update()
    {
        // Keep Y position locked
        Vector3 pos = transform.position;
        pos.y = 0.02f;
        transform.position = pos;

        // Move toward target position
        MoveTowardsTarget();

        // Change direction after a set time
        if (Time.time > nextChangeTime)
        {
            targetPosition = GetNewTarget();
            nextChangeTime = Time.time + changeDirectionTime;
        }
    }

    private void MoveTowardsTarget()
    {
        Vector3 dir = (targetPosition - transform.position).normalized;
        dir.y = 0; // no vertical movement

        // Rotate smoothly towards target
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 2f);

        // Move forward
        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        // Play walk animation if available
        if (animator != null)
            animator.SetBool("isWalking", true);
    }

    private Vector3 GetNewTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * walkRange;
        Vector3 newTarget = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        return newTarget;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TurnAndContinue();
    }

    private void OnTriggerEnter(Collider other)
    {
        TurnAndContinue();
    }

    private void TurnAndContinue()
    {
        // Instantly rotate 46° on Y-axis
        transform.Rotate(0, rotationOnHit, 0);

        // Pick a new random direction to walk
        targetPosition = GetNewTarget();
        nextChangeTime = Time.time + changeDirectionTime;
    }
}
