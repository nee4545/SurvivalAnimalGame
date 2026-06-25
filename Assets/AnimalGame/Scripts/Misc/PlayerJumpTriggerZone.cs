using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayerJumpTriggerZone : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional exact takeoff point. If empty, the player's current position is used.")]
    public Transform jumpStartPoint;

    [Tooltip("The exact point where the player lands.")]
    public Transform landingPoint;

    [Header("Jump")]
    public float jumpHeight = 4f;
    public float jumpDuration = 1.2f;

    [Tooltip("Moves the player to Jump Start Point before beginning the jump.")]
    public bool snapPlayerToJumpStart = true;

    [Tooltip("Use the Landing Point rotation when the jump finishes.")]
    public bool useLandingRotation = true;

    [Header("Backflip")]
    public float backflipDegrees = -360f;

    [Tooltip(
        "Usually X for a backflip. " +
        "Try Z if your animal flips sideways."
    )]
    public Vector3 backflipAxis = Vector3.right;

    [Header("Trigger")]
    public string playerTag = "Player";
    public bool oneShot;
    public bool useUnscaledTime;

    [Header("Gizmos")]
    public bool showTrajectoryGizmo = true;

    [Range(5, 100)]
    public int gizmoSegments = 30;

    public float landingMarkerRadius = 0.6f;
    public Color trajectoryColor = Color.yellow;
    public Color landingColor = Color.green;
    public Color takeoffColor = Color.cyan;

    private bool isJumping;
    private bool hasTriggered;

    private Tween jumpTween;

    private CCActor activePlayer;
    private CharacterController activeController;
    private bool controllerWasEnabled;

    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();

        if (trigger != null)
            trigger.isTrigger = true;
    }

    private void OnValidate()
    {
        Collider trigger = GetComponent<Collider>();

        if (trigger != null)
            trigger.isTrigger = true;

        jumpHeight = Mathf.Max(0f, jumpHeight);
        jumpDuration = Mathf.Max(0.05f, jumpDuration);
        gizmoSegments = Mathf.Max(5, gizmoSegments);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isJumping)
            return;

        if (oneShot && hasTriggered)
            return;

        if (!IsPlayerCollider(other))
            return;

        CCActor playerActor = other.GetComponentInParent<CCActor>();

        if (playerActor == null)
            return;

        if (playerActor.isDead ||
            playerActor.isInParabola ||
            playerActor.isSlowMotionHuntActive)
        {
            return;
        }

        StartJump(playerActor);
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other.CompareTag(playerTag))
            return true;

        Transform root = other.transform.root;

        return root != null && root.CompareTag(playerTag);
    }

    private void StartJump(CCActor playerActor)
    {
        if (landingPoint == null)
        {
            Debug.LogWarning(
                $"[{name}] No Landing Point has been assigned.",
                this
            );

            return;
        }

        isJumping = true;
        hasTriggered = true;

        activePlayer = playerActor;
        activeController =
            playerActor.controller != null
                ? playerActor.controller
                : playerActor.GetComponent<CharacterController>();

        Transform playerTransform = playerActor.transform;

        Vector3 startPosition =
            snapPlayerToJumpStart && jumpStartPoint != null
                ? jumpStartPoint.position
                : playerTransform.position;

        Vector3 endPosition = landingPoint.position;

        Quaternion startRotation = playerTransform.rotation;

        Quaternion endRotation =
            useLandingRotation
                ? landingPoint.rotation
                : startRotation;

        // Stop normal player systems.
        playerActor.isInParabola = true;
        playerActor.inputVec = Vector2.zero;
        playerActor.moveDirection = Vector3.zero;
        playerActor.verticalVelocity = 0f;
        playerActor.isRunning = false;
        playerActor.isAttackingLoop = false;

        playerActor.animHandler?.SetAnimation(
            eCuteAnimalAnims.JUMP
        );

        // CharacterController can fight direct Transform movement,
        // so disable it during the tween.
        if (activeController != null)
        {
            controllerWasEnabled = activeController.enabled;

            if (activeController.enabled)
                activeController.enabled = false;
        }

        playerTransform.SetPositionAndRotation(
            startPosition,
            startRotation
        );

        jumpTween?.Kill();

        float progress = 0f;

        jumpTween = DOTween.To(
                () => progress,
                value =>
                {
                    progress = value;

                    UpdateJumpTransform(
                        playerTransform,
                        startPosition,
                        endPosition,
                        startRotation,
                        endRotation,
                        progress
                    );
                },
                1f,
                jumpDuration
            )
            .SetEase(Ease.Linear)
            .SetUpdate(useUnscaledTime)
            .OnComplete(() =>
            {
                CompleteJump(
                    playerActor,
                    endPosition,
                    endRotation
                );
            });
    }

    private void UpdateJumpTransform(
        Transform playerTransform,
        Vector3 startPosition,
        Vector3 endPosition,
        Quaternion startRotation,
        Quaternion endRotation,
        float progress)
    {
        float t = Mathf.Clamp01(progress);

        Vector3 position = EvaluateParabola(
            startPosition,
            endPosition,
            t
        );

        Quaternion facingRotation = Quaternion.Slerp(
            startRotation,
            endRotation,
            t
        );

        Vector3 axis =
            backflipAxis.sqrMagnitude > 0.001f
                ? backflipAxis.normalized
                : Vector3.right;

        Quaternion flipRotation = Quaternion.AngleAxis(
            backflipDegrees * t,
            axis
        );

        playerTransform.position = position;
        playerTransform.rotation =
            facingRotation * flipRotation;
    }

    private Vector3 EvaluateParabola(
        Vector3 start,
        Vector3 end,
        float t)
    {
        Vector3 position = Vector3.Lerp(start, end, t);

        // 0 at takeoff and landing, 1 at the middle.
        float parabola = 4f * t * (1f - t);

        position.y += jumpHeight * parabola;

        return position;
    }

    private void CompleteJump(
        CCActor playerActor,
        Vector3 endPosition,
        Quaternion endRotation)
    {
        if (playerActor == null)
        {
            ClearRuntimeState();
            return;
        }

        playerActor.transform.SetPositionAndRotation(
            endPosition,
            endRotation
        );

        if (activeController != null && controllerWasEnabled)
            activeController.enabled = true;

        playerActor.inputVec = Vector2.zero;
        playerActor.moveDirection = Vector3.zero;
        playerActor.verticalVelocity = 0f;
        playerActor.isRunning = false;
        playerActor.isInParabola = false;

        playerActor.animHandler?.SetAnimation(
            eCuteAnimalAnims.IDLE
        );

        ClearRuntimeState();
    }

    private void ClearRuntimeState()
    {
        jumpTween = null;
        activePlayer = null;
        activeController = null;
        controllerWasEnabled = false;
        isJumping = false;
    }

    private void OnDisable()
    {
        if (jumpTween != null && jumpTween.IsActive())
            jumpTween.Kill();

        // Safety in case this trigger is disabled mid-jump.
        if (activePlayer != null)
        {
            if (activeController != null && controllerWasEnabled)
                activeController.enabled = true;

            activePlayer.inputVec = Vector2.zero;
            activePlayer.moveDirection = Vector3.zero;
            activePlayer.verticalVelocity = 0f;
            activePlayer.isRunning = false;
            activePlayer.isInParabola = false;
        }

        ClearRuntimeState();
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    private void OnDrawGizmos()
    {
        if (!showTrajectoryGizmo || landingPoint == null)
            return;

        Vector3 start =
            jumpStartPoint != null
                ? jumpStartPoint.position
                : transform.position;

        Vector3 end = landingPoint.position;

        Gizmos.color = trajectoryColor;

        Vector3 previousPoint = start;

        for (int i = 1; i <= gizmoSegments; i++)
        {
            float t = (float)i / gizmoSegments;

            Vector3 nextPoint = EvaluateParabola(
                start,
                end,
                t
            );

            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }

        Gizmos.color = takeoffColor;
        Gizmos.DrawWireSphere(
            start,
            landingMarkerRadius * 0.7f
        );

        Gizmos.color = landingColor;
        Gizmos.DrawWireSphere(
            end,
            landingMarkerRadius
        );

        Gizmos.DrawLine(
            end,
            end + landingPoint.forward * 1.5f
        );
    }
}