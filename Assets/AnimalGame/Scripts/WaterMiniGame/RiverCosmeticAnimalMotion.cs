using UnityEngine;

public class RiverCosmeticAnimalMotion : MonoBehaviour
{
    public enum CosmeticMoveDirection
    {
        Back,
        Left,
        Right,
        BackLeft,
        BackRight
    }

    [Header("Motion Target")]
    [Tooltip("Usually the animal root. If empty, this transform moves.")]
    public Transform motionTarget;

    [Tooltip("Optional visual/model root for rotation. If empty, Motion Target rotates.")]
    public Transform rotationTarget;

    [Header("Renderers")]
    [Tooltip("If empty, renderers are found automatically.")]
    public Renderer[] renderersToCheck;

    [Header("Visibility")]
    public bool moveOnlyWhenVisible = true;
    public float visibilityCheckInterval = 0.2f;

    [Header("Movement")]
    [Tooltip("Small movement distance. Try 0.15 to 0.35.")]
    public float moveDistance = 0.25f;

    [Tooltip("Walk movement speed. Try 0.4 to 1.0.")]
    public float moveSpeed = 0.75f;

    public float minPauseTime = 0.8f;
    public float maxPauseTime = 2.2f;

    [Tooltip("If ON, animal can choose Back, Left, Right, BackLeft, BackRight.")]
    public bool randomizeDirection = true;

    public CosmeticMoveDirection fixedDirection = CosmeticMoveDirection.Back;

    [Header("Allowed Directions")]
    public bool allowBack = true;
    public bool allowLeft = true;
    public bool allowRight = true;
    public bool allowBackLeft = true;
    public bool allowBackRight = true;

    [Header("Ground Follow")]
    public bool followGround = true;

    [Tooltip("Set this to your Terrain / Ground / RiverBank layer.")]
    public LayerMask groundMask = ~0;

    [Tooltip("Ray starts this high above the animal.")]
    public float groundRaycastHeight = 3f;

    [Tooltip("How far downward the ray checks.")]
    public float groundRaycastDistance = 8f;

    [Tooltip("Keeps animal slightly above ground.")]
    public float groundOffset = 0.04f;

    [Tooltip("How quickly animal height follows ground.")]
    public float groundSnapSharpness = 14f;

    [Tooltip("Small Y changes below this are ignored to reduce jitter.")]
    public float groundYDeadZone = 0.015f;

    [Tooltip("Usually OFF for small cosmetic animals. ON may jitter on uneven mesh triangles.")]
    public bool alignToGroundNormal = false;

    public float groundAlignSharpness = 4f;

    private Vector3 currentGroundNormal = Vector3.up;

    [Header("Rotation")]
    public bool rotateWhileMoving = true;

    [Tooltip("How quickly animal turns toward movement direction.")]
    public float rotationSharpness = 7f;

    [Tooltip("Use 90, -90, or 180 if your model faces the wrong way.")]
    public float modelYawOffset = 0f;

    [Tooltip("Return to original rotation when idle.")]
    public bool returnRotationWhenIdle = true;

    [Header("Animation")]
    public CuteAnimalAnimHandler animHandler;
    public bool playAnimations = true;

    public eCuteAnimalAnims idleAnimation = eCuteAnimalAnims.IDLE;
    public eCuteAnimalAnims walkAnimation = eCuteAnimalAnims.WALK;

    [Header("Invisible Behaviour")]
    public bool returnToStartWhenInvisible = true;
    public float returnSharpness = 5f;

    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;

    private Vector3 moveStartLocalPosition;
    private Vector3 moveTargetLocalPosition;
    private Vector3 currentMoveLocalDirection;

    private float visibilityTimer;
    private float pauseTimer;
    private float moveProgress;

    private bool isVisible;
    private bool isMoving;

    private bool hasCurrentAnimation;
    private eCuteAnimalAnims currentAnimation;

    private void Awake()
    {
        if (motionTarget == null)
            motionTarget = transform;

        if (rotationTarget == null)
            rotationTarget = motionTarget;

        startLocalPosition = motionTarget.localPosition;
        startLocalRotation = rotationTarget.localRotation;

        if (renderersToCheck == null || renderersToCheck.Length == 0)
            renderersToCheck = GetComponentsInChildren<Renderer>(true);

        if (animHandler == null)
            animHandler = GetComponentInChildren<CuteAnimalAnimHandler>();

        pauseTimer = Random.Range(minPauseTime, maxPauseTime);
    }

    private void OnEnable()
    {
        if (motionTarget != null)
            motionTarget.localPosition = startLocalPosition;

        if (rotationTarget != null)
            rotationTarget.localRotation = startLocalRotation;

        currentGroundNormal = Vector3.up;

        isMoving = false;
        isVisible = false;
        moveProgress = 0f;
        visibilityTimer = 0f;
        pauseTimer = Random.Range(minPauseTime, maxPauseTime);

        hasCurrentAnimation = false;
        PlayIdle();
    }

    private void Update()
    {
        UpdateVisibilityCheck();

        if (moveOnlyWhenVisible && !isVisible)
        {
            if (returnToStartWhenInvisible)
                ReturnToStart();

            return;
        }

        if (isMoving)
            UpdateMovement();
        else
            UpdatePause();
    }

    private void UpdateVisibilityCheck()
    {
        visibilityTimer -= Time.deltaTime;

        if (visibilityTimer > 0f)
            return;

        visibilityTimer = Mathf.Max(0.05f, visibilityCheckInterval);
        isVisible = IsAnyRendererVisible();
    }

    private bool IsAnyRendererVisible()
    {
        if (renderersToCheck == null)
            return false;

        for (int i = 0; i < renderersToCheck.Length; i++)
        {
            Renderer renderer = renderersToCheck[i];

            if (renderer == null)
                continue;

            if (!renderer.enabled)
                continue;

            if (renderer.isVisible)
                return true;
        }

        return false;
    }

    private void UpdatePause()
    {
        ApplyGroundFollow();

        if (returnRotationWhenIdle)
            ReturnRotationToStart();

        pauseTimer -= Time.deltaTime;

        if (pauseTimer > 0f)
            return;

        BeginSmallMove();
    }

    private void BeginSmallMove()
    {
        currentMoveLocalDirection = GetMoveDirection();

        moveStartLocalPosition = motionTarget.localPosition;

        moveTargetLocalPosition =
            startLocalPosition + currentMoveLocalDirection * moveDistance;

        // Important:
        // Do not control Y through movement.
        // Ground raycast controls Y.
        moveTargetLocalPosition.y = motionTarget.localPosition.y;

        moveProgress = 0f;
        isMoving = true;

        PlayWalk();
    }

    private void UpdateMovement()
    {
        moveProgress += Time.deltaTime * moveSpeed;

        float t = Mathf.Clamp01(moveProgress);
        t = Mathf.SmoothStep(0f, 1f, t);

        Vector3 newLocalPosition = Vector3.Lerp(
            moveStartLocalPosition,
            moveTargetLocalPosition,
            t
        );

        // Important:
        // Preserve current Y. Ground follow owns vertical placement.
        newLocalPosition.y = motionTarget.localPosition.y;

        motionTarget.localPosition = newLocalPosition;

        ApplyGroundFollow();

        if (rotateWhileMoving)
            RotateTowardMoveDirection();

        if (moveProgress >= 1f)
        {
            isMoving = false;
            pauseTimer = Random.Range(minPauseTime, maxPauseTime);
            PlayIdle();
        }
    }

    private Vector3 GetMoveDirection()
    {
        CosmeticMoveDirection direction =
            randomizeDirection
                ? PickRandomAllowedDirection()
                : fixedDirection;

        switch (direction)
        {
            case CosmeticMoveDirection.Back:
                return Vector3.back;

            case CosmeticMoveDirection.Left:
                return Vector3.left;

            case CosmeticMoveDirection.Right:
                return Vector3.right;

            case CosmeticMoveDirection.BackLeft:
                return (Vector3.back + Vector3.left).normalized;

            case CosmeticMoveDirection.BackRight:
                return (Vector3.back + Vector3.right).normalized;
        }

        return Vector3.back;
    }

    private CosmeticMoveDirection PickRandomAllowedDirection()
    {
        CosmeticMoveDirection[] directions = new CosmeticMoveDirection[5];
        int count = 0;

        if (allowBack)
            directions[count++] = CosmeticMoveDirection.Back;

        if (allowLeft)
            directions[count++] = CosmeticMoveDirection.Left;

        if (allowRight)
            directions[count++] = CosmeticMoveDirection.Right;

        if (allowBackLeft)
            directions[count++] = CosmeticMoveDirection.BackLeft;

        if (allowBackRight)
            directions[count++] = CosmeticMoveDirection.BackRight;

        if (count == 0)
            return CosmeticMoveDirection.Back;

        return directions[Random.Range(0, count)];
    }

    private void ApplyGroundFollow()
    {
        if (!followGround)
            return;

        if (motionTarget == null)
            return;

        Vector3 rayOrigin =
            motionTarget.position + Vector3.up * groundRaycastHeight;

        float rayDistance =
            groundRaycastHeight + groundRaycastDistance;

        RaycastHit hit;

        if (!Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out hit,
                rayDistance,
                groundMask,
                QueryTriggerInteraction.Ignore
            ))
        {
            return;
        }

        Vector3 position = motionTarget.position;

        float targetY = hit.point.y + groundOffset;
        float yDifference = Mathf.Abs(position.y - targetY);

        if (yDifference > groundYDeadZone)
        {
            position.y = Mathf.Lerp(
                position.y,
                targetY,
                1f - Mathf.Exp(-groundSnapSharpness * Time.deltaTime)
            );

            motionTarget.position = position;
        }

        currentGroundNormal = Vector3.Slerp(
            currentGroundNormal,
            hit.normal,
            1f - Mathf.Exp(-groundAlignSharpness * Time.deltaTime)
        );
    }

    private void RotateTowardMoveDirection()
    {
        if (rotationTarget == null)
            return;

        Vector3 worldDirection;

        if (motionTarget.parent != null)
            worldDirection =
                motionTarget.parent.TransformDirection(currentMoveLocalDirection);
        else
            worldDirection = currentMoveLocalDirection;

        worldDirection.y = 0f;

        if (worldDirection.sqrMagnitude < 0.001f)
            return;

        Vector3 upDirection =
            alignToGroundNormal
                ? currentGroundNormal
                : Vector3.up;

        if (alignToGroundNormal)
        {
            worldDirection =
                Vector3.ProjectOnPlane(worldDirection, upDirection);

            if (worldDirection.sqrMagnitude < 0.001f)
                return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(worldDirection.normalized, upDirection) *
            Quaternion.Euler(0f, modelYawOffset, 0f);

        rotationTarget.rotation = Quaternion.Slerp(
            rotationTarget.rotation,
            targetRotation,
            1f - Mathf.Exp(-rotationSharpness * Time.deltaTime)
        );
    }

    private void ReturnRotationToStart()
    {
        if (rotationTarget == null)
            return;

        rotationTarget.localRotation = Quaternion.Slerp(
            rotationTarget.localRotation,
            startLocalRotation,
            1f - Mathf.Exp(-rotationSharpness * Time.deltaTime)
        );
    }

    private void ReturnToStart()
    {
        if (motionTarget != null)
        {
            Vector3 targetLocalPosition = startLocalPosition;

            // Preserve Y. Ground raycast controls vertical placement.
            targetLocalPosition.y = motionTarget.localPosition.y;

            motionTarget.localPosition = Vector3.Lerp(
                motionTarget.localPosition,
                targetLocalPosition,
                1f - Mathf.Exp(-returnSharpness * Time.deltaTime)
            );

            ApplyGroundFollow();
        }

        if (returnRotationWhenIdle)
            ReturnRotationToStart();

        if (isMoving)
        {
            isMoving = false;
            PlayIdle();
        }
    }

    private void PlayWalk()
    {
        PlayAnimation(walkAnimation);
    }

    private void PlayIdle()
    {
        PlayAnimation(idleAnimation);
    }

    private void PlayAnimation(eCuteAnimalAnims animation)
    {
        if (!playAnimations)
            return;

        if (animHandler == null)
            return;

        if (hasCurrentAnimation && currentAnimation == animation)
            return;

        currentAnimation = animation;
        hasCurrentAnimation = true;

        animHandler.SetAnimation(animation);
    }
}