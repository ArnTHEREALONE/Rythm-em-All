using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float baseSpeed = 8f;

    [SerializeField] private Vector3 moveInput;
    [SerializeField] private float actualSpeed;

    private Rigidbody rb;

    public Vector3 LastMoveDirection { get; private set; } = Vector3.forward;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
    }

    public void Initialize(PlayerConfig config)
    {
        if (config != null)
        {
            baseSpeed = config.moveSpeed;
        }
    }

    public void SetMoveInput(Vector3 input)
    {
        moveInput = input;
        if (moveInput.magnitude > 1f)
            moveInput.Normalize();

        if (moveInput.sqrMagnitude > 0.01f)
            LastMoveDirection = moveInput.normalized;
    }

    private void FixedUpdate()
    {
        actualSpeed = SpeedMultiplier.Instance != null
            ? SpeedMultiplier.Instance.GetScaledSpeed(baseSpeed)
            : baseSpeed;

        Vector3 velocity = moveInput * actualSpeed;
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);
    }
}
