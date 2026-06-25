using UnityEngine;

public class StampedeLaneController : MonoBehaviour
{
    [Header("Lane Setup")]
    public int laneCount = 3;
    public float laneWidth = 3f;
    public float laneMoveSpeed = 8f;
    public float laneArriveDistance = 0.05f;

    [Header("Direction Reference")]
    public Transform directionReference;

    [Header("Rotation")]
    public float rotationSpeed = 12f;
    public float sideTurnAmount = 1f;
    public float forwardBlendWhileTurning = 0.35f;

    [Header("Jump")]
    public float jumpHeight = 2.2f;
    public float jumpDuration = 0.65f;
    public float jumpForwardDistance = 1.4f;
    public float faceForwardAfterJumpDuration = 0.2f;

    [Header("Animation")]
    public CuteAnimalAnimHandler animHandler;

    [Header("Stampede Visual Direction")]
    public bool faceAwayFromStampede = true;

    [Tooltip("When Face Away From Stampede is ON, move the player this far forward along the lane.")]
    public float faceAwayForwardLaneOffset = 0f;

    [Header("Keyboard Test Input")]
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Hit Reaction")]
    public float hitReactionLockTime = 0.45f;

    [Header("Cinematic Hit Reaction")]
    public Camera stampedeCamera;
    public float hitCinematicDuration = 0.75f;
    public float hitMoveTowardCameraDistance = 3.5f;
    public float hitJumpHeight = 2.8f;
    public float hitSpinDegrees = 360f;
    public float hitReturnToLaneDuration = 0.18f;

    [Header("Rock Stumble Reaction")]
    public float rockStumbleBackDistance = 2.2f;
    public float rockStumblePushDuration = 0.12f;
    public float rockStumbleHoldDuration = 0.08f;
    public float rockStumbleRecoverDuration = 0.55f;
    public float rockStumbleInputLockTime = 0.35f;

    private bool isRockStumbleReacting;
    private float rockStumbleTimer;
    private float rockStumbleOffset;
    private float rockStumbleInputLockTimer;

    private bool isCinematicHitReacting;
    private float cinematicHitTimer;
    private Vector3 cinematicHitStartPosition;
    private Vector3 cinematicHitPeakPosition;
    private Vector3 cinematicHitReturnLanePosition;
    private Quaternion cinematicHitStartRotation;

    [Header("Debug")]
    public bool debugLogs;

    private Transform player;
    private Transform laneCenter;

    private bool active;
    private int currentLane;
    private int targetLane;

    private Vector3 baseCenterPosition;
    private Vector3 laneMovePosition;

    private bool isJumping;
    private float jumpTimer;

    private bool isRecoveringJumpRotation;
    private float jumpRotationRecoverTimer;

    private bool isChangingLane;
    private int lastMoveDirection;

    private bool isHitReacting;
    private float hitReactionTimer;

    public bool IsActive => active;
    public bool IsJumping => isJumping;
    public int CurrentLane => currentLane;

    public void Begin(Transform playerTransform, Transform laneCenterTransform)
    {
        player = playerTransform;
        laneCenter = laneCenterTransform;

        if (player == null || laneCenter == null)
            return;

        if (animHandler == null)
            animHandler = player.GetComponentInChildren<CuteAnimalAnimHandler>();

        active = true;

        baseCenterPosition = laneCenter.position;

        currentLane = laneCount / 2;
        targetLane = currentLane;

        isJumping = false;
        jumpTimer = 0f;

        isRecoveringJumpRotation = false;
        jumpRotationRecoverTimer = 0f;

        isChangingLane = false;
        lastMoveDirection = 0;

        isHitReacting = false;
        hitReactionTimer = 0f;

        SnapPlayerToCurrentLane();
        PlayRunAnimation();

        if (debugLogs)
            Debug.Log("[StampedeLane] Started.");
    }

    public void End()
    {
        // Stop Update from writing to player.position before doing anything else.
        active = false;

        isJumping = false;
        jumpTimer = 0f;

        isRecoveringJumpRotation = false;
        jumpRotationRecoverTimer = 0f;

        isChangingLane = false;
        lastMoveDirection = 0;

        isHitReacting = false;
        isCinematicHitReacting = false;
        hitReactionTimer = 0f;
        cinematicHitTimer = 0f;

        isRockStumbleReacting = false;
        rockStumbleTimer = 0f;
        rockStumbleOffset = 0f;
        rockStumbleInputLockTimer = 0f;

        PlayIdleAnimation();

        player = null;
        laneCenter = null;

        if (debugLogs)
            Debug.Log("[StampedeLane] Ended.");
    }
    private void Update()
    {
        if (!active || player == null)
            return;

        UpdateHitReaction();

        if (isHitReacting)
            return;

        if (!isRockStumbleReacting || rockStumbleInputLockTimer <= 0f)
            ReadKeyboardInput();

        UpdatePlayerPosition();
        UpdatePlayerRotation();
    }

    private void ReadKeyboardInput()
    {
        if (Input.GetKeyDown(leftKey))
            MoveLane(-1);

        if (Input.GetKeyDown(rightKey))
            MoveLane(1);

        if (Input.GetKeyDown(jumpKey))
            TryJump();
    }

    public void MoveLane(int direction)
    {
        if (!active)
            return;

        if (isRockStumbleReacting && rockStumbleInputLockTimer > 0f)
            return;

        if (isHitReacting)
            return;

        if (isChangingLane)
            return;

        int newLane = targetLane + direction;
        newLane = Mathf.Clamp(newLane, 0, laneCount - 1);

        if (newLane == targetLane)
            return;

        targetLane = newLane;
        isChangingLane = true;
        lastMoveDirection = direction;

        if (!isJumping)
            PlayRunAnimation();

        if (debugLogs)
            Debug.Log("[StampedeLane] Moving to lane: " + targetLane);
    }

    public void TryJump()
    {
        if (!active)
            return;

        if (isRockStumbleReacting && rockStumbleInputLockTimer > 0f)
            return;

        if (isHitReacting)
            return;

        if (isJumping)
            return;

        isJumping = true;
        jumpTimer = 0f;

        isRecoveringJumpRotation = false;
        jumpRotationRecoverTimer = 0f;

        PlayJumpAnimation();

        if (debugLogs)
            Debug.Log("[StampedeLane] Jump.");
    }

    public void PlayRockStumbleReaction(Vector3 hitDirection)
    {
        if (!active || player == null)
            return;

        isRockStumbleReacting = true;

        rockStumbleTimer = 0f;
        rockStumbleOffset = 0f;
        rockStumbleInputLockTimer = rockStumbleInputLockTime;

        isJumping = false;
        jumpTimer = 0f;

        isRecoveringJumpRotation = false;
        jumpRotationRecoverTimer = 0f;

        int nearestLane = GetClosestLaneIndex(player.position);
        currentLane = nearestLane;
        targetLane = nearestLane;

        isChangingLane = false;
        lastMoveDirection = 0;

        Vector3 lanePos = GetLanePosition(currentLane);
        lanePos.y = baseCenterPosition.y;
        laneMovePosition = lanePos;

        PlayDamageAnimation();

        if (debugLogs)
            Debug.Log("[StampedeLane] Rock stumble reaction.");
    }

    private void UpdateRockStumbleReaction()
    {
        if (!isRockStumbleReacting)
            return;

        rockStumbleTimer += Time.deltaTime;

        if (rockStumbleInputLockTimer > 0f)
            rockStumbleInputLockTimer -= Time.deltaTime;

        float pushEnd = rockStumblePushDuration;
        float holdEnd = pushEnd + rockStumbleHoldDuration;
        float totalDuration =
            rockStumblePushDuration +
            rockStumbleHoldDuration +
            rockStumbleRecoverDuration;

        if (rockStumbleTimer <= pushEnd)
        {
            float t = Mathf.Clamp01(rockStumbleTimer / rockStumblePushDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            rockStumbleOffset = Mathf.Lerp(0f, rockStumbleBackDistance, t);
        }
        else if (rockStumbleTimer <= holdEnd)
        {
            rockStumbleOffset = rockStumbleBackDistance;
        }
        else
        {
            float recoverT =
                (rockStumbleTimer - holdEnd) /
                Mathf.Max(0.01f, rockStumbleRecoverDuration);

            recoverT = Mathf.Clamp01(recoverT);
            recoverT = Mathf.SmoothStep(0f, 1f, recoverT);

            rockStumbleOffset = Mathf.Lerp(rockStumbleBackDistance, 0f, recoverT);
        }

        if (rockStumbleTimer >= totalDuration)
        {
            FinishRockStumbleReaction();
        }
    }

    private void FinishRockStumbleReaction()
    {
        isRockStumbleReacting = false;
        rockStumbleTimer = 0f;
        rockStumbleOffset = 0f;
        rockStumbleInputLockTimer = 0f;

        PlayRunAnimation();
    }

    private Vector3 GetRockStumbleOffset()
    {
        if (!isRockStumbleReacting)
            return Vector3.zero;

        Vector3 backDirection = -GetPlayerVisualForwardDirection();
        backDirection.y = 0f;

        if (backDirection.sqrMagnitude < 0.001f)
            return Vector3.zero;

        return backDirection.normalized * rockStumbleOffset;
    }

    public void PlayHitReaction(Vector3 hitDirection)
    {
        if (!active || player == null)
            return;

        isHitReacting = true;
        isCinematicHitReacting = true;

        hitReactionTimer = hitReactionLockTime;
        cinematicHitTimer = 0f;

        isJumping = false;
        jumpTimer = 0f;

        isRecoveringJumpRotation = false;
        jumpRotationRecoverTimer = 0f;

        // Lock player to closest current lane so lane movement does not continue during hit.
        int nearestLane = GetClosestLaneIndex(player.position);
        currentLane = nearestLane;
        targetLane = nearestLane;

        isChangingLane = false;
        lastMoveDirection = 0;

        cinematicHitStartPosition = player.position;

        cinematicHitReturnLanePosition = GetLanePosition(currentLane);
        cinematicHitReturnLanePosition.y = baseCenterPosition.y;

        Vector3 cameraDirection = GetCameraDirectionForHit();

        cinematicHitPeakPosition =
            cinematicHitStartPosition +
            cameraDirection * hitMoveTowardCameraDistance;

        cinematicHitPeakPosition.y =
            baseCenterPosition.y + hitJumpHeight;

        cinematicHitStartRotation = player.rotation;

        PlayDamageAnimation();

        if (debugLogs)
            Debug.Log("[StampedeLane] Cinematic hit reaction.");
    }

    private int GetClosestLaneIndex(Vector3 worldPosition)
    {
        int closestLane = 0;
        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < laneCount; i++)
        {
            Vector3 lanePos = GetLanePosition(i);

            Vector3 a = new Vector3(worldPosition.x, 0f, worldPosition.z);
            Vector3 b = new Vector3(lanePos.x, 0f, lanePos.z);

            float distance = Vector3.SqrMagnitude(a - b);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestLane = i;
            }
        }

        return closestLane;
    }

    private void UpdateHitReaction()
    {
        if (!isHitReacting)
            return;

        hitReactionTimer -= Time.deltaTime;

        if (isCinematicHitReacting)
            UpdateCinematicHitReaction();

        if (hitReactionTimer <= 0f)
        {
            FinishHitReaction();
        }
    }

    private void UpdateCinematicHitReaction()
    {
        if (player == null)
            return;

        cinematicHitTimer += Time.deltaTime;

        float t = Mathf.Clamp01(cinematicHitTimer / hitCinematicDuration);

        Vector3 targetPosition;

        if (t < 0.65f)
        {
            float outT = t / 0.65f;
            outT = Mathf.SmoothStep(0f, 1f, outT);

            targetPosition = Vector3.Lerp(
                cinematicHitStartPosition,
                cinematicHitPeakPosition,
                outT
            );
        }
        else
        {
            float backT = (t - 0.65f) / 0.35f;
            backT = Mathf.SmoothStep(0f, 1f, backT);

            targetPosition = Vector3.Lerp(
                cinematicHitPeakPosition,
                cinematicHitReturnLanePosition,
                backT
            );
        }

        player.position = targetPosition;

        float spin = hitSpinDegrees * t;
        Quaternion spinRotation =
            cinematicHitStartRotation *
            Quaternion.Euler(spin, 0f, 0f);

        player.rotation = spinRotation;
    }

    private void FinishHitReaction()
    {
        isHitReacting = false;
        isCinematicHitReacting = false;
        hitReactionTimer = 0f;
        cinematicHitTimer = 0f;

        Vector3 lanePos = GetLanePosition(currentLane);
        lanePos.y = baseCenterPosition.y;

        player.position = lanePos;
        player.rotation = Quaternion.LookRotation(GetPlayerVisualForwardDirection(), Vector3.up);

        if (isChangingLane)
            PlayRunAnimation();
        else
            PlayRunAnimation();
    }

    private Vector3 GetCameraDirectionForHit()
    {
        Camera cam = stampedeCamera != null ? stampedeCamera : Camera.main;

        if (cam == null)
            return -GetForwardDirection();

        Vector3 dir = cam.transform.position - player.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            dir = -GetForwardDirection();

        return dir.normalized;
    }

    private void UpdatePlayerPosition()
    {
        Vector3 targetPos = GetLanePosition(targetLane);
        targetPos.y = baseCenterPosition.y;

        laneMovePosition = Vector3.MoveTowards(
            laneMovePosition,
            targetPos,
            laneMoveSpeed * Time.deltaTime
        );

        Vector3 jumpOffset = GetJumpOffset();

        Vector3 stumbleOffset = GetRockStumbleOffset();

        Vector3 finalPosition =
            laneMovePosition +
            jumpOffset +
            stumbleOffset;

        finalPosition.y = baseCenterPosition.y + jumpOffset.y;

        player.position = finalPosition;

        float distance = Vector3.Distance(
            new Vector3(laneMovePosition.x, 0f, laneMovePosition.z),
            new Vector3(targetPos.x, 0f, targetPos.z)
        );

        if (isChangingLane && distance <= laneArriveDistance)
        {
            currentLane = targetLane;
            isChangingLane = false;
            lastMoveDirection = 0;

            if (!isJumping && !isHitReacting)
                PlayRunAnimation();
        }
    }

    private void UpdatePlayerRotation()
    {
        Quaternion targetRotation = GetTargetRotation();

        player.rotation = Quaternion.Slerp(
            player.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void UpdateJumpRotationRecovery()
    {
        if (!isRecoveringJumpRotation)
            return;

        jumpRotationRecoverTimer -= Time.deltaTime;

        if (jumpRotationRecoverTimer <= 0f)
        {
            isRecoveringJumpRotation = false;

            if (player != null)
                player.rotation = Quaternion.LookRotation(GetForwardDirection(), Vector3.up);
        }
    }

    private Quaternion GetTargetRotation()
    {
        Vector3 forward = GetPlayerVisualForwardDirection();
        Vector3 right = GetLaneRightDirection();

        Quaternion forwardRotation = Quaternion.LookRotation(forward, Vector3.up);
        Quaternion backRotation = Quaternion.LookRotation(-forward, Vector3.up);

        if (isHitReacting)
            return forwardRotation;

        if (isJumping)
        {
            float jumpProgress = jumpTimer / jumpDuration;

            if (jumpProgress <= 0.5f)
            {
                float turnT = jumpProgress / 0.5f;
                turnT = Mathf.SmoothStep(0f, 1f, turnT);

                // During forward jump, turn 180 degrees.
                return Quaternion.Slerp(forwardRotation, backRotation, turnT);
            }

            // While returning back to lane position, keep facing backward.
            return backRotation;
        }

        if (isChangingLane && lastMoveDirection != 0)
        {
            Vector3 lookDirection = (
                right * lastMoveDirection * sideTurnAmount +
                forward * forwardBlendWhileTurning
            ).normalized;

            if (lookDirection.sqrMagnitude < 0.001f)
                lookDirection = forward;

            return Quaternion.LookRotation(lookDirection, Vector3.up);
        }

        // After landing, smoothly face forward again.
        if (isRecoveringJumpRotation)
            return forwardRotation;

        return forwardRotation;
    }

    private Vector3 GetLanePosition(int lane)
    {
        float middleIndex = (laneCount - 1) * 0.5f;
        float laneOffset = (lane - middleIndex) * laneWidth;

        Vector3 laneRight = GetLaneRightDirection();

        Vector3 pos = baseCenterPosition + laneRight * laneOffset;

        if (!faceAwayFromStampede)
        {
            pos += GetForwardDirection() * faceAwayForwardLaneOffset;
        }

        return pos;
    }

    public Vector3 GetLaneWorldPosition(int lane)
    {
        lane = Mathf.Clamp(lane, 0, laneCount - 1);
        return GetLanePosition(lane);
    }

    private Vector3 GetLaneRightDirection()
    {
        Transform reference = directionReference != null ? directionReference : laneCenter;

        Vector3 right = reference.right;
        right.y = 0f;

        if (right.sqrMagnitude < 0.001f)
            right = Vector3.right;

        return right.normalized;
    }

    public Vector3 GetForwardDirection()
    {
        Transform reference = directionReference != null ? directionReference : laneCenter;

        Vector3 forward = reference.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        return forward.normalized;
    }

    private Vector3 GetPlayerVisualForwardDirection()
    {
        Vector3 forward = GetForwardDirection();

        if (faceAwayFromStampede)
            return -forward;

        return forward;
    }

    private Vector3 GetJumpOffset()
    {
        if (!isJumping)
            return Vector3.zero;

        jumpTimer += Time.deltaTime;

        float t = jumpTimer / jumpDuration;

        if (t >= 1f)
        {
            isJumping = false;
            jumpTimer = 0f;

            isRecoveringJumpRotation = true;
            jumpRotationRecoverTimer = faceForwardAfterJumpDuration;

            if (isChangingLane)
                PlayRunAnimation();
            else if (!isHitReacting)
                PlayRunAnimation();

            return Vector3.zero;
        }

        float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;

        float forwardAmount;

        if (t <= 0.5f)
        {
            // Jump forward.
            forwardAmount = Mathf.Lerp(0f, jumpForwardDistance, t / 0.5f);
        }
        else
        {
            // Walk/return back to original lane position.
            forwardAmount = Mathf.Lerp(jumpForwardDistance, 0f, (t - 0.5f) / 0.5f);
        }

        Vector3 forwardOffset = GetForwardDirection() * forwardAmount;

        return new Vector3(forwardOffset.x, height, forwardOffset.z);
    }

    private void SnapPlayerToCurrentLane()
    {
        Vector3 pos = GetLanePosition(currentLane);
        pos.y = baseCenterPosition.y;

        laneMovePosition = pos;

        player.position = pos;
        player.rotation = Quaternion.LookRotation(GetPlayerVisualForwardDirection(), Vector3.up);
    }

    private void PlayIdleAnimation()
    {
        if (animHandler == null)
            return;

        animHandler.SetAnimation(eCuteAnimalAnims.IDLE);
    }

    private void PlayRunAnimation()
    {
        if (animHandler == null)
            return;

        animHandler.SetAnimation(eCuteAnimalAnims.RUN);
    }

    private void PlayJumpAnimation()
    {
        if (animHandler == null)
            return;

        animHandler.SetAnimation(eCuteAnimalAnims.JUMP);
    }

    private void PlayDamageAnimation()
    {
        if (animHandler == null)
            return;

        animHandler.SetAnimation(eCuteAnimalAnims.RUN);
    }
}