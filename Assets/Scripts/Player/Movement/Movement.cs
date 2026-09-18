using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Movement : MonoBehaviour
{
    public enum BodyTurnMode
    {
        /// <summary>Body is always glued to the look direction. Fine for a capsule, stiff for a humanoid.</summary>
        FaceLook,

        /// <summary>Body turns to the look direction while walking, and stays put while standing still.</summary>
        FaceLookWhenMoving,

        /// <summary>Body is never turned here. Leave it to an animator or a turn-in-place system.</summary>
        Free
    }

    [Header("Walking")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 60f;

    [Header("Jumping")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -22f;

    [Tooltip("Grace period after walking off a ledge where a jump still counts.")]
    [SerializeField] private float coyoteTime = 0.12f;

    [Tooltip("How early a jump press is remembered before landing.")]
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Body")]
    [SerializeField] private PlayerCamera playerCamera;
    [SerializeField] private BodyTurnMode bodyTurn = BodyTurnMode.FaceLookWhenMoving;

    [Tooltip("Degrees per second the body turns to catch up with the look direction.")]
    [SerializeField] private float turnSpeed = 540f;

    [Tooltip("How far the look direction may stray from the body before the body is dragged around. " +
             "This is the angle a head and spine are expected to cover on their own.")]
    [SerializeField] private float maxLookOffset = 70f;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float lastGroundedTime = float.NegativeInfinity;
    private float lastJumpPressedTime = float.NegativeInfinity;

    /// <summary>
    /// Signed degrees the look direction sits away from the body's facing. Once there is a
    /// humanoid, feed this to the head and chest bones in LateUpdate (or to a Multi-Aim
    /// Constraint) so the character looks where the camera looks without the body following.
    /// </summary>
    public float LookOffset { get; private set; }

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<PlayerCamera>();
        }
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            lastJumpPressedTime = Time.time;
        }

        Vector2 input = ReadMoveInput(keyboard);

        UpdateBodyRotation(input.sqrMagnitude > 0.01f);
        UpdateHorizontal(input);
        UpdateVertical();

        Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    // WASD (and arrow keys) 
    private Vector2 ReadMoveInput(Keyboard keyboard)
    {
        Vector2 input = Vector2.zero;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;

        // Keeps diagonals from being faster than straight lines. No bunny hopping for now :p
        return Vector2.ClampMagnitude(input, 1f);
    }

    private void UpdateBodyRotation(bool isMoving)
    {
        if (playerCamera == null || bodyTurn == BodyTurnMode.Free)
        {
            return;
        }

        float lookYaw = playerCamera.Yaw;
        float bodyYaw = transform.eulerAngles.y;
        float targetYaw;

        if (bodyTurn == BodyTurnMode.FaceLook)
        {
            targetYaw = lookYaw;
        }
        else if (isMoving)
        {
            
            targetYaw = Mathf.MoveTowardsAngle(bodyYaw, lookYaw, turnSpeed * Time.deltaTime);
        }
        else
        {
            float offset = Mathf.DeltaAngle(bodyYaw, lookYaw);

            // Standing still, the head does the work until it runs out of travel,
            // and only then does the body get dragged around to keep up.
            targetYaw = Mathf.Abs(offset) > maxLookOffset
                ? lookYaw - Mathf.Sign(offset) * maxLookOffset
                : bodyYaw;
        }

        transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
        LookOffset = Mathf.DeltaAngle(targetYaw, lookYaw);
    }

    private void UpdateHorizontal(Vector2 input)
    {
        // Relative to the look direction, not the body, so strafing stays correct
        // even while the body is lagging behind or standing still.
        Vector3 forward = playerCamera != null ? playerCamera.PlanarForward : transform.forward;
        Vector3 right = playerCamera != null ? playerCamera.PlanarRight : transform.right;

        Vector3 target = (right * input.x + forward * input.y) * moveSpeed;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, acceleration * Time.deltaTime);
    }

    private void UpdateVertical()
    {
        if (controller.isGrounded)
        {
            lastGroundedTime = Time.time;

            if (verticalVelocity < 0f)
            {
                // Small downward bias keeps the controller pinned to ramps and steps.
                verticalVelocity = -2f;
            }
        }

        bool hasGround = Time.time - lastGroundedTime <= coyoteTime;
        bool wantsJump = Time.time - lastJumpPressedTime <= jumpBufferTime;

        if (hasGround && wantsJump)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            // Consume both windows so one press cannot produce two jumps.
            lastGroundedTime = float.NegativeInfinity;
            lastJumpPressedTime = float.NegativeInfinity;
        }
        else if (verticalVelocity > 0f && (controller.collisionFlags & CollisionFlags.Above) != 0)
        {
            // Hit a ceiling
            verticalVelocity = 0f;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }
}
