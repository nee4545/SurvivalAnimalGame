using System.Collections;
using UnityEngine;

public class RiverSceneJumpRideable : MonoBehaviour
{
    public enum AfterLandingMoveDirection
    {
        UseSpawnerDefault,
        MoveAlongRiverForward,
        MoveOppositeRiverForward
    }

    public enum AfterLandingFacingMode
    {
        KeepJumpFacing,
        FaceRiverForward,
        FaceOppositeRiverForward,
        FaceMovementDirection,
        FaceOppositeMovementDirection
    }

    [Header("References")]
    public RiverRideableObject rideable;
    public RiverRideableSpawner normalSpawner;
    public Transform playerReference;
    public Transform landingPoint;

    [Header("Scrolling Tile Handling")]
    public bool detachFromScrollingTileOnActivate = true;

    [Tooltip("Use a non-scrolling runtime parent. Example: RiverMiniGameWorld/RuntimeRideables.")]
    public Transform runtimeParentAfterActivate;

    [Header("Return To Original Tile")]
    public bool returnToOriginalTileOnDespawn = true;
    public bool restoreOriginalLocalTransform = true;
    public bool playIdleAnimationAfterReturn = true;

    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Vector3 originalLocalScale;
    private bool cachedOriginalTransform;

    [Header("After Landing Flow")]
    public AfterLandingMoveDirection afterLandingMoveDirection =
        AfterLandingMoveDirection.UseSpawnerDefault;

    [Header("After Landing Facing")]
    public AfterLandingFacingMode afterLandingFacingMode =
        AfterLandingFacingMode.FaceMovementDirection;

    public float afterLandingYawOffset = 0f;

    [Header("Activation")]
    public bool activateByDistance = true;
    public float activationDistance = 18f;
    public bool useFlatDistance = true;

    [Header("Activation Distance Offset")]
    [Tooltip("Optional point used as animal-side distance check center. If empty, this object's transform is used.")]
    public Transform animalDistanceCheckPoint;

    [Tooltip("Local offset from animalDistanceCheckPoint / this transform.")]
    public Vector3 animalDistanceCheckLocalOffset;

    [Tooltip("World offset added after local offset.")]
    public Vector3 animalDistanceCheckWorldOffset;

    [Tooltip("Local offset from player transform.")]
    public Vector3 playerDistanceCheckLocalOffset;

    [Tooltip("World offset added after player local offset.")]
    public Vector3 playerDistanceCheckWorldOffset;

    [Header("Landing Point Reset")]
    public bool detachLandingPointIfChildOfAnimal = true;
    public bool restoreLandingPointOnReturn = true;

    private Transform originalLandingPointParent;
    private Vector3 originalLandingPointLocalPosition;
    private Quaternion originalLandingPointLocalRotation;
    private Vector3 originalLandingPointLocalScale;
    private bool cachedLandingPointTransform;

    [Header("Re-arm After Return")]
    public float activationCooldownAfterReturn = 1f;
    public bool requirePlayerExitRangeAfterReturn = true;
    public float rearmDistanceMultiplier = 1.25f;

    private float activationCooldownTimer;
    private bool waitingForPlayerToLeaveRange;

    [Header("Jump")]
    public float jumpDuration = 0.8f;
    public float jumpHeight = 5f;

    public AnimationCurve jumpCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public bool faceLandingDirection = true;

    [Header("Animation")]
    public CuteAnimalAnimHandler animHandler;
    public bool playJumpAnimation = true;
    public bool playRunAnimationAfterLanding = true;

    private bool activated;
    private bool landed;
    private Coroutine jumpRoutine;

    private void Awake()
    {
        CacheOriginalTileTransform();
        CacheLandingPointTransform();

        if (rideable == null)
            rideable = GetComponent<RiverRideableObject>();

        if (animHandler == null)
            animHandler = GetComponentInChildren<CuteAnimalAnimHandler>();
    }

    private void OnEnable()
    {
        ResetSceneJumpState();
    }

    private void Update()
    {
        if (activated || landed)
            return;

        if (!activateByDistance)
            return;

        if (playerReference == null)
            return;

        if (activationCooldownTimer > 0f)
        {
            activationCooldownTimer -= Time.deltaTime;
            return;
        }

        if (waitingForPlayerToLeaveRange)
        {
            float rearmDistance =
                activationDistance * Mathf.Max(1f, rearmDistanceMultiplier);

            if (!IsPlayerInsideRange(rearmDistance))
                waitingForPlayerToLeaveRange = false;

            return;
        }

        if (IsPlayerCloseEnough())
            ActivateJump();
    }

    private void CacheOriginalTileTransform()
    {
        if (cachedOriginalTransform)
            return;

        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        originalLocalScale = transform.localScale;

        cachedOriginalTransform = true;
    }

    private void ResetSceneJumpState()
    {
        activated = false;
        landed = false;

        if (jumpRoutine != null)
        {
            StopCoroutine(jumpRoutine);
            jumpRoutine = null;
        }

        if (rideable != null)
        {
            rideable.isSceneAuthoredRideable = true;
            rideable.canBeMounted = false;
            rideable.SetTargetHighlighted(false);
            rideable.SetRideExpireWarningActive(false);
        }

        RiverRideableAIBehavior ai =
            GetComponent<RiverRideableAIBehavior>();

        if (ai != null)
        {
            ai.SetManagedBySpawner(false);
            ai.StopSelfFlow();
        }
    }

    public void ActivateJump()
    {
        if (activated || landed)
            return;

        if (landingPoint == null)
            return;

        activated = true;

        if (jumpRoutine != null)
            StopCoroutine(jumpRoutine);

        jumpRoutine = StartCoroutine(JumpIntoRiverRoutine());
    }

    private bool IsPlayerCloseEnough()
    {
        return IsPlayerInsideRange(activationDistance);
    }

    private bool IsPlayerInsideRange(float distance)
    {
        Vector3 animalCheckPosition = GetAnimalDistanceCheckPosition();
        Vector3 playerCheckPosition = GetPlayerDistanceCheckPosition();

        Vector3 toPlayer = playerCheckPosition - animalCheckPosition;

        if (useFlatDistance)
            toPlayer.y = 0f;

        return toPlayer.sqrMagnitude <= distance * distance;
    }


    private void CacheLandingPointTransform()
    {
        if (cachedLandingPointTransform)
            return;

        if (landingPoint == null)
            return;

        originalLandingPointParent = landingPoint.parent;
        originalLandingPointLocalPosition = landingPoint.localPosition;
        originalLandingPointLocalRotation = landingPoint.localRotation;
        originalLandingPointLocalScale = landingPoint.localScale;

        cachedLandingPointTransform = true;
    }

    private void KeepLandingPointStableDuringJump()
    {
        if (!detachLandingPointIfChildOfAnimal)
            return;

        if (landingPoint == null)
            return;

        if (landingPoint == transform)
            return;

        if (!landingPoint.IsChildOf(transform))
            return;

        Transform temporaryParent =
            originalParent != null
                ? originalParent
                : transform.parent;

        landingPoint.SetParent(temporaryParent, true);
    }

    private void RestoreLandingPointTransform()
    {
        if (!restoreLandingPointOnReturn)
            return;

        if (landingPoint == null)
            return;

        if (!cachedLandingPointTransform)
            return;

        landingPoint.SetParent(originalLandingPointParent, false);
        landingPoint.localPosition = originalLandingPointLocalPosition;
        landingPoint.localRotation = originalLandingPointLocalRotation;
        landingPoint.localScale = originalLandingPointLocalScale;
    }

    private Vector3 GetAnimalDistanceCheckPosition()
    {
        Transform checkTransform =
            animalDistanceCheckPoint != null
                ? animalDistanceCheckPoint
                : transform;

        return
            checkTransform.TransformPoint(animalDistanceCheckLocalOffset) +
            animalDistanceCheckWorldOffset;
    }

    private Vector3 GetPlayerDistanceCheckPosition()
    {
        if (playerReference == null)
            return Vector3.zero;

        return
            playerReference.TransformPoint(playerDistanceCheckLocalOffset) +
            playerDistanceCheckWorldOffset;
    }

    private IEnumerator JumpIntoRiverRoutine()
    {
        if (rideable != null)
        {
            rideable.canBeMounted = false;
            rideable.SetTargetHighlighted(false);
            rideable.SetRideExpireWarningActive(false);
        }

        Vector3 startPosition = transform.position;

        // Cache landing world position before detaching.
        // Landing point may be child of a scrolling tile.
        Vector3 endPosition = landingPoint.position;

        if (normalSpawner != null && rideable != null)
        {
            endPosition.y =
                normalSpawner.GetWaterYForExternalUse() +
                rideable.heightAboveWater;
        }

        // If landing point is under the animal, detach it before the animal jumps.
        // Otherwise the landing point travels with the animal.
        KeepLandingPointStableDuringJump();

        if (detachFromScrollingTileOnActivate)
        {
            Transform newParent = runtimeParentAfterActivate;

            if (newParent == null && normalSpawner != null)
                newParent = normalSpawner.sceneRideableRuntimeParent;

            if (newParent != null && !newParent.gameObject.activeInHierarchy)
            {
                Debug.LogError(
                    "[RiverSceneJumpRideable] Runtime parent is inactive. " +
                    "Animal would disappear after reparenting. Parent: " +
                    newParent.name,
                    newParent
                );

                newParent = null;
            }

            transform.SetParent(newParent, true);
        }

        if (playJumpAnimation && animHandler != null)
            animHandler.SetAnimation(eCuteAnimalAnims.JUMP);

        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = startRotation;

        Vector3 flatDirection = endPosition - startPosition;
        flatDirection.y = 0f;

        if (faceLandingDirection && flatDirection.sqrMagnitude > 0.001f)
            endRotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);

        float timer = 0f;
        float duration = Mathf.Max(0.05f, jumpDuration);

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float rawT = Mathf.Clamp01(timer / duration);
            float curvedT = jumpCurve.Evaluate(rawT);

            Vector3 position = Vector3.Lerp(startPosition, endPosition, curvedT);

            float arc = Mathf.Sin(rawT * Mathf.PI) * jumpHeight;
            position.y += arc;

            transform.position = position;
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, curvedT);

            yield return null;
        }

        transform.position = endPosition;
        transform.rotation = endRotation;

        landed = true;
        jumpRoutine = null;

        bool useMoveOverride;
        bool moveOppositeRiverForward;

        GetMoveDirectionOverride(
            out useMoveOverride,
            out moveOppositeRiverForward
        );

        if (normalSpawner != null && rideable != null)
        {
            normalSpawner.RegisterSceneRideable(
                rideable,
                useMoveOverride,
                moveOppositeRiverForward,
                runtimeParentAfterActivate
            );

            ApplyAfterLandingFacing(useMoveOverride, moveOppositeRiverForward);
        }
        else if (rideable != null)
        {
            rideable.canBeMounted = true;
            ApplyAfterLandingFacing(useMoveOverride, moveOppositeRiverForward);
        }

        if (playRunAnimationAfterLanding && animHandler != null)
            animHandler.SetAnimation(eCuteAnimalAnims.RUN);
    }

    private void GetMoveDirectionOverride(
        out bool useOverride,
        out bool moveOppositeRiverForward
    )
    {
        useOverride = false;
        moveOppositeRiverForward = false;

        if (afterLandingMoveDirection ==
            AfterLandingMoveDirection.UseSpawnerDefault)
        {
            return;
        }

        useOverride = true;

        if (afterLandingMoveDirection ==
            AfterLandingMoveDirection.MoveAlongRiverForward)
        {
            moveOppositeRiverForward = false;
        }
        else if (afterLandingMoveDirection ==
                 AfterLandingMoveDirection.MoveOppositeRiverForward)
        {
            moveOppositeRiverForward = true;
        }
    }

    private void ApplyAfterLandingFacing(
        bool useMoveOverride,
        bool moveOppositeRiverForwardOverride
    )
    {
        if (rideable == null)
            return;

        if (afterLandingFacingMode == AfterLandingFacingMode.KeepJumpFacing)
            return;

        bool finalMoveOppositeRiverForward = moveOppositeRiverForwardOverride;

        if (!useMoveOverride && normalSpawner != null)
        {
            finalMoveOppositeRiverForward =
                normalSpawner.GetMoveOppositeRiverForwardForExternalUse();
        }

        RiverRideableAIBehavior behavior =
            rideable.GetComponent<RiverRideableAIBehavior>();

        if (behavior != null)
        {
            switch (afterLandingFacingMode)
            {
                case AfterLandingFacingMode.FaceMovementDirection:
                    behavior.faceOppositeScrollDirection = false;
                    break;

                case AfterLandingFacingMode.FaceOppositeMovementDirection:
                    behavior.faceOppositeScrollDirection = true;
                    break;

                case AfterLandingFacingMode.FaceRiverForward:
                    behavior.faceOppositeScrollDirection =
                        finalMoveOppositeRiverForward;
                    break;

                case AfterLandingFacingMode.FaceOppositeRiverForward:
                    behavior.faceOppositeScrollDirection =
                        !finalMoveOppositeRiverForward;
                    break;
            }

            behavior.yawOffset = afterLandingYawOffset;
            behavior.SnapRotationToCorrectFacing();
            return;
        }

        Vector3 facingDirection = GetFallbackFacingDirection(
            finalMoveOppositeRiverForward
        );

        if (facingDirection.sqrMagnitude <= 0.001f)
            return;

        transform.rotation =
            Quaternion.LookRotation(facingDirection.normalized, Vector3.up) *
            Quaternion.Euler(0f, afterLandingYawOffset, 0f);
    }

    private Vector3 GetFallbackFacingDirection(bool finalMoveOppositeRiverForward)
    {
        Vector3 riverForward = Vector3.forward;

        if (normalSpawner != null)
            riverForward = normalSpawner.GetRiverForwardForExternalUse();

        riverForward.y = 0f;

        if (riverForward.sqrMagnitude <= 0.001f)
            riverForward = Vector3.forward;

        riverForward.Normalize();

        Vector3 movementDirection =
            finalMoveOppositeRiverForward
                ? -riverForward
                : riverForward;

        switch (afterLandingFacingMode)
        {
            case AfterLandingFacingMode.FaceRiverForward:
                return riverForward;

            case AfterLandingFacingMode.FaceOppositeRiverForward:
                return -riverForward;

            case AfterLandingFacingMode.FaceMovementDirection:
                return movementDirection;

            case AfterLandingFacingMode.FaceOppositeMovementDirection:
                return -movementDirection;
        }

        return transform.forward;
    }

    public void ReturnToOriginalTile()
    {
        if (!returnToOriginalTileOnDespawn)
            return;

        Debug.LogWarning(
        "[RiverSceneJumpRideable] ReturnToOriginalTile CALLED: " +
        name +
        "\nPosition before return: " + transform.position +
        "\nOriginal parent: " + (originalParent != null ? originalParent.name : "NULL") +
        "\nStack:\n" + System.Environment.StackTrace,
        this
     );

        if (jumpRoutine != null)
        {
            StopCoroutine(jumpRoutine);
            jumpRoutine = null;
        }

        if (normalSpawner != null && rideable != null)
            normalSpawner.UnregisterSceneRideable(rideable);

        gameObject.SetActive(true);

        if (cachedOriginalTransform)
        {
            transform.SetParent(originalParent, false);

            if (restoreOriginalLocalTransform)
            {
                transform.localPosition = originalLocalPosition;
                transform.localRotation = originalLocalRotation;
                transform.localScale = originalLocalScale;
            }
        }

        RestoreLandingPointTransform();

        if (rideable != null)
        {
            rideable.PrepareForPool();
            rideable.ResetForSpawnerReuse();

            rideable.isSceneAuthoredRideable = true;
            rideable.canBeMounted = false;
            rideable.isMovingAgainstRiver = false;

            rideable.SetTargetHighlighted(false);
            rideable.SetRideExpireWarningActive(false);
        }

        RiverRideableAIBehavior ai =
            GetComponent<RiverRideableAIBehavior>();

        if (ai != null)
        {
            ai.SetManagedBySpawner(false);
            ai.StopSelfFlow();
        }

        activated = false;
        landed = false;

        activationCooldownTimer = activationCooldownAfterReturn;
        waitingForPlayerToLeaveRange = requirePlayerExitRangeAfterReturn;

        if (playIdleAnimationAfterReturn && animHandler != null)
            animHandler.SetAnimation(eCuteAnimalAnims.IDLE);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 animalCheckPosition = GetAnimalDistanceCheckPosition();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(animalCheckPosition, activationDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(animalCheckPosition, 0.35f);

        if (playerReference != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(GetPlayerDistanceCheckPosition(), 0.35f);
        }

        if (landingPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, landingPoint.position);
            Gizmos.DrawWireSphere(landingPoint.position, 0.6f);
        }
    }
#endif
}