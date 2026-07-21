using UnityEngine;

public class RiverRideableAIBehavior : MonoBehaviour
{
    public enum RideableBehaviorType
    {
        Straight,
        LazyDrift,
        SideToSide,
        ZigZag,
        Crossing
    }

    [Header("References")]
    public RiverRideableObject rideable;

    [Header("Behavior")]
    public RideableBehaviorType behaviorType = RideableBehaviorType.Straight;

    [Header("Unmanaged Self Flow")]
    public bool allowUnmanagedSelfFlow = true;
    public float unmanagedSelfFlowSpeed = 12f;

    private bool isManagedBySpawner;
    private bool selfFlowActive;
    private bool selfFlowMoveOppositeRiverForward = true;

    [Header("Side Movement")]
    public float sideAmplitude = 1.5f;
    public float sideSpeed = 1.2f;
    public float sideSharpness = 6f;

    [Header("Crossing")]
    public float crossingSpeed = 0.8f;

    [Header("Rotation")]
    public bool faceMoveDirection = true;
    public float rotationSharpness = 8f;

    [Header("Position Correction")]
    public bool useRiverCenterPositionCorrection = true;

    [Header("Facing")]
    [Tooltip("ON = animal faces opposite to the river tile movement direction.")]
    public bool faceOppositeScrollDirection = true;

    [Tooltip("Use this if the model itself faces sideways/backwards.")]
    public float yawOffset = 0f;

    [Header("Side Movement Visual Yaw")]
    public bool useSideMovementYaw = true;

    [Tooltip("Maximum extra yaw when the AI is moving sideways.")]
    public float maxSideMoveYawAngle = 25f;

    [Tooltip("How quickly the AI rotates into/out of side movement yaw.")]
    public float sideMoveYawSharpness = 8f;

    [Tooltip("Side speed needed to reach full side yaw. Lower = easier to reach max yaw.")]
    public float sideMoveSpeedForMaxYaw = 2f;

    [Tooltip("Use if the yaw direction feels reversed.")]
    public bool invertSideMoveYaw = false;

    [Tooltip("Extra control if left side rotation looks weaker/stronger from camera angle.")]
    public float leftSideYawMultiplier = 1f;

    [Tooltip("Extra control if right side rotation looks weaker/stronger from camera angle.")]
    public float rightSideYawMultiplier = 1f;

    private float currentSideMoveVelocity;
    private float currentSideMoveYawInput;

    private Transform riverCenterReference;
    private Transform riverDirectionReference;

    private float riverHalfWidth = 5f;
    private float baseLateralOffset;
    private float currentLateralOffset;
    private float targetLateralOffset;

    private float randomPhase;
    private float crossingDirection = 1f;
    private bool currentMoveOppositeRiverForward = true;

    private bool initialized;

    private void Awake()
    {
        if (rideable == null)
            rideable = GetComponent<RiverRideableObject>();

        randomPhase = Random.Range(0f, 100f);
    }

    public bool IsInitialized
    {
        get { return initialized; }
    }

    public void Initialize(
        Transform centerReference,
        Transform directionReference,
        float halfWidth,
        float initialLateralOffset
    )
    {
        riverCenterReference = centerReference;
        riverDirectionReference = directionReference;
        riverHalfWidth = Mathf.Max(0.5f, halfWidth);

        baseLateralOffset = initialLateralOffset;
        currentLateralOffset = initialLateralOffset;
        targetLateralOffset = initialLateralOffset;

        crossingDirection = initialLateralOffset >= 0f ? -1f : 1f;

        initialized = true;

        currentSideMoveVelocity = 0f;
        currentSideMoveYawInput = 0f;

        SnapRotationToCorrectFacing();
    }

    public void ManagedTick(float moveSpeed, bool moveOppositeRiverForward)
    {
        if (!initialized)
            return;

        currentMoveOppositeRiverForward = moveOppositeRiverForward;

        if (rideable != null && rideable.HasRider)
            return;

        Vector3 forward = GetRiverForward();
        Vector3 right = GetRiverRight(forward);

        float moveDirection = moveOppositeRiverForward ? -1f : 1f;

        transform.position += forward * moveDirection * moveSpeed * Time.deltaTime;

        UpdateSideMovement(right);

        if (faceMoveDirection)
            UpdateRotation(forward, right, moveDirection);
    }

    public void SetMoveDirectionMode(bool moveOppositeRiverForward)
    {
        currentMoveOppositeRiverForward = moveOppositeRiverForward;
    }

    public void SetManagedBySpawner(bool managed)
    {
        isManagedBySpawner = managed;

        if (isManagedBySpawner)
            selfFlowActive = false;
    }


    private void Update()
    {
        if (!selfFlowActive)
            return;

        if (isManagedBySpawner)
            return;

        if (!initialized)
            return;

        if (rideable != null && rideable.HasRider)
            return;

        ManagedTick(
            unmanagedSelfFlowSpeed,
            selfFlowMoveOppositeRiverForward
        );
    }

    public void BeginSelfFlow(float moveSpeed, bool moveOppositeRiverForward)
    {
        if (!allowUnmanagedSelfFlow)
            return;

        if (isManagedBySpawner)
            return;

        if (!initialized)
            return;

        unmanagedSelfFlowSpeed = moveSpeed;
        selfFlowMoveOppositeRiverForward = moveOppositeRiverForward;
        currentMoveOppositeRiverForward = moveOppositeRiverForward;

        selfFlowActive = true;
    }

    public void StopSelfFlow()
    {
        selfFlowActive = false;
    }

    public void SyncAIToCurrentPosition()
    {
        if (!initialized)
            return;

        if (riverCenterReference == null)
            return;

        Vector3 forward = GetRiverForward();
        Vector3 right = GetRiverRight(forward);

        Vector3 centerToObject = transform.position - riverCenterReference.position;

        float lateralOffset = Vector3.Dot(centerToObject, right);

        lateralOffset = Mathf.Clamp(
            lateralOffset,
            -riverHalfWidth,
            riverHalfWidth
        );

        baseLateralOffset = lateralOffset;
        currentLateralOffset = lateralOffset;
        targetLateralOffset = lateralOffset;

        currentSideMoveVelocity = 0f;
        currentSideMoveYawInput = 0f;
    }

    public void SnapRotationToCorrectFacing()
    {
        if (!initialized)
            return;

        Vector3 forward = GetRiverForward();
        Vector3 right = GetRiverRight(forward);

        float moveDirection = currentMoveOppositeRiverForward ? -1f : 1f;

        Quaternion targetRotation = GetTargetRotation(forward, right, moveDirection);

        transform.rotation = targetRotation;
    }

    public void InitializeFromCurrentPosition(
    Transform centerReference,
    Transform directionReference,
    float halfWidth,
    bool moveOppositeRiverForward
)
    {
        riverCenterReference = centerReference;
        riverDirectionReference = directionReference;
        riverHalfWidth = Mathf.Max(0.5f, halfWidth);

        currentMoveOppositeRiverForward = moveOppositeRiverForward;

        Vector3 forward = GetRiverForward();
        Vector3 right = GetRiverRight(forward);

        float lateralOffset = 0f;

        if (riverCenterReference != null)
        {
            Vector3 centerToObject = transform.position - riverCenterReference.position;
            lateralOffset = Vector3.Dot(centerToObject, right);
        }

        lateralOffset = Mathf.Clamp(
            lateralOffset,
            -riverHalfWidth,
            riverHalfWidth
        );

        baseLateralOffset = lateralOffset;
        currentLateralOffset = lateralOffset;
        targetLateralOffset = lateralOffset;

        currentSideMoveVelocity = 0f;
        currentSideMoveYawInput = 0f;

        crossingDirection = lateralOffset >= 0f ? -1f : 1f;

        initialized = true;

        SnapRotationToCorrectFacing();
    }

    public Vector3 GetRiverMoveDirection(bool moveOppositeRiverForward)
    {
        Vector3 forward = GetRiverForward();

        if (moveOppositeRiverForward)
            return -forward;

        return forward;
    }

    private void UpdateSideMovement(Vector3 right)
    {
        if (riverCenterReference == null)
            return;

        switch (behaviorType)
        {
            case RideableBehaviorType.Straight:
                targetLateralOffset = baseLateralOffset;
                break;

            case RideableBehaviorType.LazyDrift:
                targetLateralOffset =
                    baseLateralOffset +
                    Mathf.Sin(Time.time * sideSpeed * 0.45f + randomPhase) * sideAmplitude * 0.45f;
                break;

            case RideableBehaviorType.SideToSide:
                targetLateralOffset =
                    baseLateralOffset +
                    Mathf.Sin(Time.time * sideSpeed + randomPhase) * sideAmplitude;
                break;

            case RideableBehaviorType.ZigZag:
                float zigzag = Mathf.PingPong(Time.time * sideSpeed + randomPhase, 1f);
                zigzag = zigzag < 0.5f ? -1f : 1f;

                targetLateralOffset =
                    baseLateralOffset + zigzag * sideAmplitude;
                break;

            case RideableBehaviorType.Crossing:
                baseLateralOffset += crossingDirection * crossingSpeed * Time.deltaTime;

                if (Mathf.Abs(baseLateralOffset) >= riverHalfWidth * 0.85f)
                    crossingDirection *= -1f;

                targetLateralOffset = baseLateralOffset;
                break;
        }

        targetLateralOffset = Mathf.Clamp(
            targetLateralOffset,
            -riverHalfWidth,
            riverHalfWidth
        );

        float previousLateralOffset = currentLateralOffset;

        currentLateralOffset = Mathf.Lerp(
            currentLateralOffset,
            targetLateralOffset,
            1f - Mathf.Exp(-sideSharpness * Time.deltaTime)
        );

        if (Time.deltaTime > 0.0001f)
        {
            currentSideMoveVelocity =
                (currentLateralOffset - previousLateralOffset) / Time.deltaTime;
        }
        else
        {
            currentSideMoveVelocity = 0f;
        }

        if (!useRiverCenterPositionCorrection)
            return;

        Vector3 centerToObject = transform.position - riverCenterReference.position;
        Vector3 forward = GetRiverForward();

        float forwardAmount = Vector3.Dot(centerToObject, forward);

        Vector3 newPosition =
            riverCenterReference.position +
            forward * forwardAmount +
            right * currentLateralOffset;

        newPosition.y = transform.position.y;

        transform.position = newPosition;
    }

    private void UpdateRotation(Vector3 forward, Vector3 right, float moveDirection)
    {
        Quaternion targetRotation = GetTargetRotation(forward, right, moveDirection);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            1f - Mathf.Exp(rotationSharpness * -Time.deltaTime)
        );
    }

    private Quaternion GetTargetRotation(Vector3 forward, Vector3 right, float moveDirection)
    {
        Vector3 desiredForward;

        if (faceOppositeScrollDirection)
        {
            // Animal faces opposite the river tile movement.
            desiredForward = forward * -moveDirection;
        }
        else
        {
            desiredForward = forward * moveDirection;
        }

        desiredForward.y = 0f;

        if (desiredForward.sqrMagnitude <= 0.001f)
            desiredForward = forward;

        desiredForward.Normalize();

        float sideYaw = GetSideMovementYaw();

        Quaternion targetRotation =
            Quaternion.LookRotation(desiredForward, Vector3.up) *
            Quaternion.Euler(0f, yawOffset + sideYaw, 0f);

        return targetRotation;
    }

    private float GetSideMovementYaw()
    {
        if (!useSideMovementYaw)
        {
            currentSideMoveYawInput = Mathf.Lerp(
                currentSideMoveYawInput,
                0f,
                1f - Mathf.Exp(-sideMoveYawSharpness * Time.deltaTime)
            );

            return 0f;
        }

        float desiredInput = Mathf.Clamp(
            currentSideMoveVelocity / Mathf.Max(0.001f, sideMoveSpeedForMaxYaw),
            -1f,
            1f
        );

        if (invertSideMoveYaw)
            desiredInput *= -1f;

        currentSideMoveYawInput = Mathf.Lerp(
            currentSideMoveYawInput,
            desiredInput,
            1f - Mathf.Exp(-sideMoveYawSharpness * Time.deltaTime)
        );

        float multiplier = 1f;

        if (currentSideMoveYawInput < 0f)
            multiplier = leftSideYawMultiplier;
        else if (currentSideMoveYawInput > 0f)
            multiplier = rightSideYawMultiplier;

        return currentSideMoveYawInput * maxSideMoveYawAngle * multiplier;
    }

    public void ResetSideMovementVisualYaw()
    {
        currentSideMoveVelocity = 0f;
        currentSideMoveYawInput = 0f;
    }

    public void SnapRotationForMount()
    {
        if (!initialized)
            return;

        ResetSideMovementVisualYaw();

        Vector3 forward = GetRiverForward();

        float moveDirection = currentMoveOppositeRiverForward ? -1f : 1f;

        Vector3 desiredForward;

        if (faceOppositeScrollDirection)
            desiredForward = forward * -moveDirection;
        else
            desiredForward = forward * moveDirection;

        desiredForward.y = 0f;

        if (desiredForward.sqrMagnitude <= 0.001f)
            desiredForward = forward;

        Quaternion targetRotation =
            Quaternion.LookRotation(desiredForward.normalized, Vector3.up) *
            Quaternion.Euler(0f, yawOffset, 0f);

        transform.rotation = targetRotation;
    }

    private Vector3 GetRiverForward()
    {
        if (riverDirectionReference != null)
        {
            Vector3 forward = riverDirectionReference.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude > 0.001f)
                return forward.normalized;
        }

        return Vector3.forward;
    }

    private Vector3 GetRiverRight(Vector3 forward)
    {
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        if (right.sqrMagnitude <= 0.001f)
            right = Vector3.right;

        return right;
    }
}