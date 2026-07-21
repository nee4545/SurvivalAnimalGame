using System.Collections;
using UnityEngine;

public class RiverEscapePlayerController : MonoBehaviour
{
    private enum RiverPlayerState
    {
        Disabled,
        Mounted,
        Airborne,
        Mounting,
        Failed
    }

    [Header("References")]
    public Transform riverDirectionReference;
    public Transform riverCenterReference;
    public LineRenderer aimArcLine;

    [Header("Animation")]
    public CuteAnimalAnimHandler playerAnimHandler;
    public bool useRiverEscapeAnimations = true;

    [Header("River Direct Animator States")]
    public bool useDirectRiverAnimatorStates = true;

    public string riverIdleStateName = "Idle_0";
    public string riverRunStateName = "Run_0";
    public string riverJumpStateName = "Jump_1";

    public float riverAnimationFadeTime = 0.03f;

    [Header("Air Animation")]
    public float switchToRunInAirAfter = 0.35f;

    [Header("Mounted Position")]
    public Vector3 mountedOffset = Vector3.zero;
    public float mountedFollowSharpness = 30f;
    public float mountSnapDuration = 0.12f;

    [Header("Steering")]
    public float dragPixelsForFullInput = 160f;
    public float mountedSteerSpeed = 7f;
    public float airSteerAcceleration = 9f;
    public float riverHalfWidth = 4f;

    [Header("Jump")]
    public float jumpForwardSpeed = 10f;
    public float jumpSideSpeed = 4f;
    public float jumpUpSpeed = 5.5f;
    public float gravity = 18f;

    [Header("Height Based Lasso Radius")]
    public bool useHeightBasedLassoRadius = true;

    [Tooltip("Height above water where the lasso is smallest.")]
    public float lassoMinHeightAboveWater = 0.3f;

    [Tooltip("Height above water where the lasso reaches max size.")]
    public float lassoMaxHeightAboveWater = 4.5f;

    [Tooltip("Ring size at jump start / low height.")]
    public float lassoRadiusAtLowHeightMultiplier = 0.45f;

    [Tooltip("Ring size near jump peak / max height.")]
    public float lassoRadiusAtHighHeightMultiplier = 1.15f;

    [Tooltip("How smoothly the lasso grows/shrinks.")]
    public float lassoRadiusSmoothSharpness = 12f;

    [Header("Catch / Landing")]
    public float mountCatchRadius = 2.5f;
    public float catchForwardBias = 0.3f;
    public bool requireHoldToCatch = true;

    [Header("Fail")]
    public float waterY = 0f;
    public float fallBelowWaterDepth = 0.75f;

    [Header("Aim Arc")]
    public int arcSamples = 18;
    public float arcTimeStep = 0.06f;

    [Header("Lasso Catch Ring")]
    public RiverLassoCatchRing lassoCatchRing;
    public bool showLassoRingOnlyWhenAirborne = true;
    public bool brightenRingWhenHolding = true;

    [Header("Lasso Target Highlight")]
    public bool useLassoTargetOutline = true;

    [Header("Ride Expire Warning")]
    public bool useRideExpireWarning = true;

    [Tooltip("Show warning when this many seconds are left before sinking.")]
    public float rideExpireWarningSeconds = 2f;

    private bool rideExpireWarningShown;

    [Header("Ride Duration Upgrade")]
    public bool useRideDuration = true;

    [Tooltip("Upgrade this later. 1 = normal ride time, 1.5 = 50% longer.")]
    public float rideDurationMultiplier = 1f;

    [Tooltip("Upgrade this later. Adds flat seconds to every ride.")]
    public float rideDurationBonusSeconds = 0f;

    [Header("Rideable Release Flow")]
    public float releasedRideableFlowSpeed = 12f;

    [Tooltip("Usually same as RiverRideableSpawner > Move Opposite River Forward.")]
    public bool releasedRideableMoveOppositeRiverForward = true;

    [Header("Expired Ride Dismount")]
    public bool stopRiverScrollerOnRideExpire = true;
    public float expiredDismountForwardSpeed = 1.5f;
    public float expiredDismountUpSpeed = 1.2f;

    private float mountedRideTimeRemaining;
    private float mountedRideTimeTotal;

    private RiverRideableObject currentOutlinedTarget;
    private RiverRideableObject currentBestLassoTarget;

    private RiverEscapeMiniGameController controller;
    private RiverRideableObject currentRideable;

    private RiverPlayerState state = RiverPlayerState.Disabled;

    private bool pointerDown;
    private bool pointerDownLastFrame;
    private bool pointerReleasedThisFrame;

    private Vector2 pointerStart;
    private Vector2 pointerCurrent;

    private float dragAxis;
    private float lastDragAxis;

    private float airborneAnimTimer;
    private bool switchedToAirRun;

    private float currentLassoCatchRadius;

    private Vector3 airborneVelocity;

    private Coroutine mountRoutine;

    public void BeginRiverEscape(
        RiverEscapeMiniGameController miniGameController,
        RiverRideableObject startingRideable
    )
    {
        controller = miniGameController;
        enabled = true;

        currentLassoCatchRadius = GetMinimumLassoCatchRadius();

        if (aimArcLine != null)
            aimArcLine.enabled = false;

        if (lassoCatchRing != null)
            lassoCatchRing.Hide();

        if (playerAnimHandler == null)
            playerAnimHandler = GetComponentInChildren<CuteAnimalAnimHandler>();

        RiverRideableAIBehavior startingAI =
         startingRideable.GetComponent<RiverRideableAIBehavior>();

        if (startingAI != null)
        {
            startingAI.faceOppositeScrollDirection = true;
            startingAI.yawOffset = startingRideable.spawnYawOffset;

            startingAI.SetManagedBySpawner(false);
            startingAI.StopSelfFlow();

            startingAI.InitializeFromCurrentPosition(
                riverCenterReference,
                riverDirectionReference,
                riverHalfWidth,
                true
            );
        }

        MountRideableInstant(startingRideable);
    }

    public void RespawnOnRideable(RiverRideableObject rideable)
    {
        if (rideable == null)
        {
            FailRiverEscape();
            return;
        }

        if (!rideable.IsAvailable)
        {
            FailRiverEscape();
            return;
        }

        if (mountRoutine != null)
        {
            StopCoroutine(mountRoutine);
            mountRoutine = null;
        }

        if (lassoCatchRing != null)
            lassoCatchRing.Hide();

        HideAimArc();
        ClearOutlinedTarget();

        airborneVelocity = Vector3.zero;

        pointerDown = false;
        pointerDownLastFrame = false;
        pointerReleasedThisFrame = false;
        dragAxis = 0f;
        lastDragAxis = 0f;

        currentLassoCatchRadius = GetMinimumLassoCatchRadius();

        MountRideableInstant(rideable, false);
    }

    private void PlayRiverIdleAnimation()
    {
        PlayRiverAnimation(
            eCuteAnimalAnims.IDLE,
            riverIdleStateName
        );
    }

    private void PlayRiverJumpAnimation()
    {
        PlayRiverAnimation(
            eCuteAnimalAnims.JUMP,
            riverJumpStateName
        );
    }

    private void PlayRiverAirRunAnimation()
    {
        PlayRiverAnimation(
            eCuteAnimalAnims.RUN,
            riverRunStateName
        );
    }

    private void UpdateOutlinedTarget(RiverRideableObject target)
    {
        if (!useLassoTargetOutline)
        {
            ClearOutlinedTarget();
            return;
        }

        if (currentOutlinedTarget == target)
            return;

        ClearOutlinedTarget();

        currentOutlinedTarget = target;

        if (currentOutlinedTarget != null)
            currentOutlinedTarget.SetTargetHighlighted(true);
    }

    private void ClearOutlinedTarget()
    {
        if (currentOutlinedTarget != null)
            currentOutlinedTarget.SetTargetHighlighted(false);

        currentOutlinedTarget = null;
    }

    private void PlayRiverAnimation(
        eCuteAnimalAnims fallbackAnimation,
        string directStateName
    )
    {
        if (!useRiverEscapeAnimations)
            return;

        if (playerAnimHandler == null)
            return;

        Animator animator = playerAnimHandler.animator;

        if (useDirectRiverAnimatorStates &&
            animator != null &&
            !string.IsNullOrEmpty(directStateName))
        {
            if (riverAnimationFadeTime > 0f)
            {
                animator.CrossFadeInFixedTime(
                    directStateName,
                    riverAnimationFadeTime,
                    0,
                    0f
                );
            }
            else
            {
                animator.Play(
                    directStateName,
                    0,
                    0f
                );
            }

            return;
        }

        playerAnimHandler.SetAnimation(fallbackAnimation);
    }

    private void UpdateCurrentLassoCatchRadius()
    {
        float targetRadius = GetTargetLassoCatchRadius();

        currentLassoCatchRadius = Mathf.Lerp(
            currentLassoCatchRadius,
            targetRadius,
            1f - Mathf.Exp(-lassoRadiusSmoothSharpness * Time.deltaTime)
        );
    }

    private float GetMinimumLassoCatchRadius()
    {
        return mountCatchRadius * lassoRadiusAtLowHeightMultiplier;
    }

    private float GetTargetLassoCatchRadius()
    {
        if (!useHeightBasedLassoRadius)
            return mountCatchRadius;

        float heightAboveWater = transform.position.y - waterY;

        float t = Mathf.InverseLerp(
            lassoMinHeightAboveWater,
            lassoMaxHeightAboveWater,
            heightAboveWater
        );

        t = Mathf.Clamp01(t);
        t = Mathf.SmoothStep(0f, 1f, t);

        // Low height = small ring.
        // High height = large ring.
        float multiplier = Mathf.Lerp(
            lassoRadiusAtLowHeightMultiplier,
            lassoRadiusAtHighHeightMultiplier,
            t
        );

        return mountCatchRadius * multiplier;
    }

    private float GetCurrentLassoCatchRadius()
    {
        if (!useHeightBasedLassoRadius)
            return mountCatchRadius;

        return Mathf.Max(0.1f, currentLassoCatchRadius);
    }

    public void EndRiverEscape()
    {
        if (mountRoutine != null)
        {
            StopCoroutine(mountRoutine);
            mountRoutine = null;
        }

        if (currentRideable != null)
        {
            currentRideable.SetRider(null);
            currentRideable = null;
        }

        if (lassoCatchRing != null)
            lassoCatchRing.Hide();

        if (aimArcLine != null)
            aimArcLine.enabled = false;

        state = RiverPlayerState.Disabled;
        enabled = false;
    }

    private void Update()
    {
        if (state == RiverPlayerState.Disabled ||
            state == RiverPlayerState.Failed)
            return;

        ReadInput();

        if (state == RiverPlayerState.Mounted)
            UpdateMounted();

        if (state == RiverPlayerState.Airborne)
            UpdateAirborne();

        pointerDownLastFrame = pointerDown;
    }

    private void ReadInput()
    {
        pointerReleasedThisFrame = false;

        bool newPointerDown = false;
        Vector2 newPointerPosition = pointerCurrent;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            newPointerDown =
                touch.phase != TouchPhase.Ended &&
                touch.phase != TouchPhase.Canceled;

            newPointerPosition = touch.position;

            if (touch.phase == TouchPhase.Began)
                pointerStart = touch.position;
        }
        else
        {
            newPointerDown = Input.GetMouseButton(0);
            newPointerPosition = Input.mousePosition;

            if (Input.GetMouseButtonDown(0))
                pointerStart = Input.mousePosition;
        }

        pointerReleasedThisFrame = pointerDownLastFrame && !newPointerDown;

        pointerDown = newPointerDown;
        pointerCurrent = newPointerPosition;

        if (pointerDown)
        {
            dragAxis = Mathf.Clamp(
                (pointerCurrent.x - pointerStart.x) / dragPixelsForFullInput,
                -1f,
                1f
            );

            lastDragAxis = dragAxis;
        }
        else
        {
            dragAxis = 0f;
        }
    }

    private bool UpdateMountedRideTimer()
    {
        if (!useRideDuration)
            return false;

        if (currentRideable == null)
            return false;

        if (!currentRideable.useRideDuration)
            return false;

        mountedRideTimeRemaining -= Time.deltaTime;

        UpdateRideExpireWarning();

        if (mountedRideTimeRemaining > 0f)
            return false;

        ForceDismountFromExpiredRideable();
        return true;
    }

    private void UpdateRideExpireWarning()
    {
        if (!useRideExpireWarning)
            return;

        if (currentRideable == null)
            return;

        bool shouldShow =
            mountedRideTimeRemaining <= rideExpireWarningSeconds &&
            mountedRideTimeRemaining > 0f;

        if (rideExpireWarningShown == shouldShow)
            return;

        rideExpireWarningShown = shouldShow;

        currentRideable.SetRideExpireWarningActive(shouldShow);
    }

    private float GetFinalRideDuration(RiverRideableObject rideable)
    {
        if (rideable == null)
            return 1f;

        float duration =
            rideable.GetRideDuration() * rideDurationMultiplier +
            rideDurationBonusSeconds;

        return Mathf.Max(0.1f, duration);
    }

    private void ForceDismountFromExpiredRideable()
    {
        if (currentRideable == null)
            return;

        HideAimArc();

        RiverRideableObject expiredRideable = currentRideable;
        currentRideable = null;

        if (controller != null && stopRiverScrollerOnRideExpire)
            controller.NotifyRideableExpired();

        expiredRideable.ExpireRideAndSink(
            releasedRideableFlowSpeed,
            releasedRideableMoveOppositeRiverForward
        );

        airborneVelocity =
            GetRiverForward() * expiredDismountForwardSpeed +
            Vector3.up * expiredDismountUpSpeed;

        pointerDown = false;
        pointerDownLastFrame = false;
        pointerReleasedThisFrame = false;

        PlayRiverJumpAnimation();

        airborneAnimTimer = 0f;
        switchedToAirRun = false;

        currentLassoCatchRadius = GetMinimumLassoCatchRadius();

        state = RiverPlayerState.Airborne;
    }

    private void UpdateMounted()
    {
        if (currentRideable == null)
        {
            FailRiverEscape();
            return;
        }

        if (UpdateMountedRideTimer())
            return;

        Vector3 riverRight = GetRiverRight();

        if (pointerDown)
        {
            float steerAmount =
                dragAxis *
                mountedSteerSpeed *
                Time.deltaTime;

            currentRideable.MoveLaterally(
            steerAmount,
            dragAxis,
            riverRight,
            riverCenterReference,
            riverHalfWidth
            );

            UpdateAimArc();

            if (lassoCatchRing != null)
                lassoCatchRing.Hide();
        }
        else
        {
            currentRideable.SetSteerInput(0f);
            HideAimArc();
        }

        Vector3 targetPosition =
            currentRideable.GetMountPosition() + mountedOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            1f - Mathf.Exp(-mountedFollowSharpness * Time.deltaTime)
        );

        Quaternion targetRotation =
    currentRideable.GetRiderRotation(
        GetRiverForward(),
        GetRiverRight()
    );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            1f - Mathf.Exp(-12f * Time.deltaTime)
        );

        if (pointerReleasedThisFrame)
            StartJump();
    }

    private void StartJump()
    {
        if (currentRideable == null)
            return;

        HideAimArc();

        if (currentRideable != null)
            currentRideable.SetRideExpireWarningActive(false);

        currentRideable.ReleaseAfterPlayerJump(
         releasedRideableFlowSpeed,
        releasedRideableMoveOppositeRiverForward
                                                );

        currentRideable = null;

        Vector3 forward = GetRiverForward();
        Vector3 right = GetRiverRight();

        airborneVelocity =
            forward * jumpForwardSpeed +
            right * lastDragAxis * jumpSideSpeed +
            Vector3.up * jumpUpSpeed;

        PlayRiverJumpAnimation();

        airborneAnimTimer = 0f;
        switchedToAirRun = false;

        state = RiverPlayerState.Airborne;
        currentLassoCatchRadius = GetMinimumLassoCatchRadius();
    }

    private void UpdateAirborneAnimation()
    {
        if (!useRiverEscapeAnimations)
            return;

        if (switchedToAirRun)
            return;

        airborneAnimTimer += Time.deltaTime;

        if (airborneAnimTimer >= switchToRunInAirAfter)
        {
            switchedToAirRun = true;
            PlayRiverAirRunAnimation();
        }
    }

    private void UpdateLassoRing()
    {
        if (lassoCatchRing == null)
            return;

        currentBestLassoTarget = FindBestRideableTarget();

        bool hasCatchable = currentBestLassoTarget != null;

        // Ring visual can still brighten only when holding, if you want.
        bool ringActive =
            brightenRingWhenHolding
                ? pointerDown && hasCatchable
                : hasCatchable;

        lassoCatchRing.Show(
            transform,
            GetCurrentLassoCatchRadius(),
            waterY,
            ringActive
        );

        // Outline should show whenever target is inside lasso radius,
        // even if player is not holding.
        UpdateOutlinedTarget(hasCatchable ? currentBestLassoTarget : null);
    }

    private void UpdateAirborne()
    {
        UpdateCurrentLassoCatchRadius();
        UpdateLassoRing();
        UpdateAirborneAnimation();

        Vector3 right = GetRiverRight();

        if (pointerDown)
        {
            airborneVelocity +=
                right *
                dragAxis *
                airSteerAcceleration *
                Time.deltaTime;

            TryCatchRideable();
        }

        airborneVelocity += Vector3.down * gravity * Time.deltaTime;

        transform.position += airborneVelocity * Time.deltaTime;

        Vector3 flatVelocity = airborneVelocity;
        flatVelocity.y = 0f;

        if (flatVelocity.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(
                flatVelocity.normalized,
                Vector3.up
            );
        }

        if (transform.position.y < waterY - fallBelowWaterDepth)
            FailRiverEscape();
    }

    private void TryCatchRideable()
    {
        if (requireHoldToCatch && !pointerDown)
            return;

        RiverRideableObject target = FindBestRideableTarget();

        if (target == null)
            return;

        StartMountRoutine(target);
    }

    private RiverRideableObject FindBestRideableTarget()
    {
        RiverRideableObject bestTarget = null;
        float bestScore = float.MaxValue;

        Vector3 playerPosition = transform.position;
        Vector3 forward = GetRiverForward();

        // Important:
        // Lasso catch uses flat XZ distance only.
        // Player jump height should not affect catching.
        Vector3 playerFlatPosition = playerPosition;
        playerFlatPosition.y = 0f;

        float catchRadius = GetCurrentLassoCatchRadius();
        float catchRadiusSqr = catchRadius * catchRadius;

        for (int i = 0; i < RiverRideableObject.All.Count; i++)
        {
            RiverRideableObject rideable = RiverRideableObject.All[i];

            if (rideable == null || !rideable.IsAvailable)
                continue;

            Vector3 targetPosition = rideable.GetMountPosition();

            Vector3 targetFlatPosition = targetPosition;
            targetFlatPosition.y = 0f;

            Vector3 flatToTarget = targetFlatPosition - playerFlatPosition;

            float flatDistanceSqr = flatToTarget.sqrMagnitude;

            if (flatDistanceSqr > catchRadiusSqr)
                continue;

            float forwardAmount = Vector3.Dot(flatToTarget, forward);

            // Allows catching slightly behind/under the player,
            // but avoids catching things far behind.
            if (forwardAmount < -1.5f)
                continue;

            float score =
                flatDistanceSqr -
                forwardAmount * catchForwardBias;

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = rideable;
            }
        }

        return bestTarget;
    }

    private void StartMountRoutine(RiverRideableObject target)
    {
        if (target == null)
            return;

        if (mountRoutine != null)
            StopCoroutine(mountRoutine);

        mountRoutine = StartCoroutine(MountRoutine(target));
    }

    private IEnumerator MountRoutine(RiverRideableObject target)
    {
        state = RiverPlayerState.Mounting;

        Vector3 startPosition = transform.position;
        float timer = 0f;

        while (timer < mountSnapDuration)
        {
            if (target == null || !target.IsAvailable)
            {
                state = RiverPlayerState.Airborne;
                yield break;
            }

            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / mountSnapDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 targetPosition =
                target.GetMountPosition() + mountedOffset;

            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            yield return null;
        }

        MountRideableInstant(target,true);
        mountRoutine = null;
    }

    private void MountRideableInstant(RiverRideableObject target, bool playLandingSplash = false)
    {
        if (target == null)
        {
            FailRiverEscape();
            return;
        }

        PlayRiverIdleAnimation();

        if (lassoCatchRing != null)
            lassoCatchRing.Hide();

        if (currentRideable != null)
            currentRideable.SetRider(null);

        currentRideable = target;

        currentRideable.SetSplashWaterY(waterY);

        if (playLandingSplash)
            currentRideable.PlayLandingSplash();

        currentRideable.SetRider(this);

        mountedRideTimeTotal = GetFinalRideDuration(currentRideable);
        mountedRideTimeRemaining = mountedRideTimeTotal;

        rideExpireWarningShown = false;

        if (currentRideable != null)
            currentRideable.SetRideExpireWarningActive(false);

        transform.position =
            currentRideable.GetMountPosition() + mountedOffset;

        transform.rotation = Quaternion.LookRotation(
            GetRiverForward(),
            Vector3.up
        );

        ClearOutlinedTarget();

        state = RiverPlayerState.Mounted;

        if (controller != null)
            controller.TryRecenterRiverForward();
    }

    private void FailRiverEscape()
    {
        if (state == RiverPlayerState.Failed)
            return;

        if (lassoCatchRing != null)
            lassoCatchRing.Hide();

        HideAimArc();

        state = RiverPlayerState.Failed;

        if (currentRideable != null)
        {
            currentRideable.SetRider(null);
            currentRideable = null;
        }

        if (controller != null)
            controller.RegisterPlayerFailed();
    }

    private void UpdateAimArc()
    {
        if (aimArcLine == null)
            return;

        aimArcLine.enabled = true;
        aimArcLine.positionCount = arcSamples;

        Vector3 forward = GetRiverForward();
        Vector3 right = GetRiverRight();

        Vector3 startPosition = transform.position;

        Vector3 velocity =
            forward * jumpForwardSpeed +
            right * lastDragAxis * jumpSideSpeed +
            Vector3.up * jumpUpSpeed;

        for (int i = 0; i < arcSamples; i++)
        {
            float t = i * arcTimeStep;

            Vector3 point =
                startPosition +
                velocity * t +
                Vector3.down * 0.5f * gravity * t * t;

            aimArcLine.SetPosition(i, point);
        }
    }

    private void HideAimArc()
    {
        if (aimArcLine != null)
            aimArcLine.enabled = false;
    }

    private Vector3 GetRiverForward()
    {
        Vector3 forward =
            riverDirectionReference != null
                ? riverDirectionReference.forward
                : Vector3.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        return forward.normalized;
    }

    private Vector3 GetRiverRight()
    {
        Vector3 right =
            riverDirectionReference != null
                ? riverDirectionReference.right
                : Vector3.right;

        right.y = 0f;

        if (right.sqrMagnitude < 0.001f)
            right = Vector3.right;

        return right.normalized;
    }
}