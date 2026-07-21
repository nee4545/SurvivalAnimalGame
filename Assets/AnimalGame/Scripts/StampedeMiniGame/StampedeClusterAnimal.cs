using DG.Tweening;
using UnityEngine;

public class StampedeClusterAnimal : MonoBehaviour
{
    private enum ClusterState
    {
        Waiting,
        Spreading,
        Splashed
    }

    [Header("References")]
    public CuteAnimalAnimHandler animHandler;

    [Header("Movement With World")]
    public bool moveWithStampedeWorld = true;
    public float moveSpeed = 15f;
    public Vector3 moveDirection;

    [Header("Player Spread")]
    public float playerSpreadDistance = 5f;
    public float spreadSideDistanceMin = 2.2f;
    public float spreadSideDistanceMax = 4.2f;
    public float spreadForwardDistanceMin = 0.5f;
    public float spreadForwardDistanceMax = 1.8f;
    public float spreadJumpPower = 0.45f;
    public float spreadDuration = 0.45f;
    public Ease spreadEase = Ease.OutBack;

    [Header("AI Splash Camera Direction")]
    public Camera splashCameraOverride;

    [Tooltip("How much the AI splash uses screen-left/screen-right direction.")]
    public float cameraSidewaysWeight = 1.0f;

    [Tooltip("How much the AI splash pushes toward the camera/screen.")]
    public float cameraTowardScreenWeight = 0.65f;

    [Tooltip("Small amount of random direction so every animal does not fly identically.")]
    public float cameraSplashRandomWeight = 0.25f;

    [Tooltip("If true, each animal randomly chooses left or right when splashed.")]
    public bool randomizeSplashSide = true;

    [Header("AI Splash")]
    public LayerMask stampedeHazardMask = ~0;
    public float aiSplashDetectRadius = 1.3f;
    public float splashDistanceMin = 4f;
    public float splashDistanceMax = 7f;
    public float splashJumpPower = 1.6f;
    public float splashDuration = 0.55f;
    public float splashSpinDegrees = 720f;
    public Ease splashEase = Ease.OutQuad;

    [Header("Cleanup")]
    public float destroyAfterSpread = 1.2f;
    public float destroyAfterSplash = 1.1f;
    public float autoDestroyDistanceFromPlayer = 45f;

    private Transform player;
    private StampedeLaneController laneController;
    private ClusterState state;

    private Vector3 basePosition;
    private Vector3 reactionOffset;
    private float reactionHeightOffset;
    private Sequence reactionSequence;

    private static readonly Collider[] hitBuffer = new Collider[12];

    public void Init(
    Transform playerTransform,
    StampedeLaneController lane,
    Vector3 worldMoveDirection,
    float worldMoveSpeed,
    Camera splashCamera
)
    {
        player = playerTransform;
        laneController = lane;
        splashCameraOverride = splashCamera;

        moveDirection = worldMoveDirection;
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude < 0.001f && laneController != null)
            moveDirection = -laneController.GetForwardDirection();

        if (moveDirection.sqrMagnitude < 0.001f)
            moveDirection = Vector3.back;

        moveDirection.Normalize();

        moveSpeed = Mathf.Abs(worldMoveSpeed);
        state = ClusterState.Waiting;

        basePosition = transform.position;
        reactionOffset = Vector3.zero;
        reactionHeightOffset = 0f;

        if (reactionSequence != null)
        {
            reactionSequence.Kill();
            reactionSequence = null;
        }

        transform.DOKill();

        if (animHandler == null)
            animHandler = GetComponentInChildren<CuteAnimalAnimHandler>();

        animHandler?.SetAnimation(eCuteAnimalAnims.IDLE);

        StampedePropSpawnReservation.RegisterActiveProp(transform);
    }

    private void Update()
    {
        MoveWithWorld();

        if (state != ClusterState.Waiting)
            return;

        CheckPlayerApproach();
        CheckAIHit();

        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);

            if (distance >= autoDestroyDistanceFromPlayer)
                Destroy(gameObject);
        }
    }

    private Camera GetSplashCamera()
    {
        if (splashCameraOverride != null)
            return splashCameraOverride;

        return Camera.main;
    }

    private Vector3 GetCameraRightOnGround()
    {
        Camera cam = GetSplashCamera();

        if (cam == null)
            return GetLaneRight();

        Vector3 right = cam.transform.right;
        right.y = 0f;

        if (right.sqrMagnitude < 0.001f)
            return GetLaneRight();

        return right.normalized;
    }

    private Vector3 GetTowardCameraOnGround()
    {
        Camera cam = GetSplashCamera();

        if (cam == null)
            return -moveDirection.normalized;

        Vector3 towardCamera = cam.transform.position - basePosition;
        towardCamera.y = 0f;

        if (towardCamera.sqrMagnitude < 0.001f)
            return -moveDirection.normalized;

        return towardCamera.normalized;
    }

    private Vector3 GetCameraSplashDirection(Transform hazard)
    {
        Vector3 cameraRight = GetCameraRightOnGround();
        Vector3 towardCamera = GetTowardCameraOnGround();

        int side = Random.value > 0.5f ? 1 : -1;

        if (!randomizeSplashSide && hazard != null)
        {
            Vector3 fromHazard = basePosition - hazard.position;
            fromHazard.y = 0f;

            float dot = Vector3.Dot(fromHazard, cameraRight);
            side = dot >= 0f ? 1 : -1;
        }

        Vector3 random = Random.insideUnitSphere;
        random.y = 0f;

        if (random.sqrMagnitude > 0.001f)
            random.Normalize();

        Vector3 direction =
            cameraRight * side * cameraSidewaysWeight +
            towardCamera * cameraTowardScreenWeight +
            random * cameraSplashRandomWeight;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            direction = cameraRight * side;

        return direction.normalized;
    }

    private void MoveWithWorld()
    {
        if (moveWithStampedeWorld)
            basePosition += moveDirection * moveSpeed * Time.deltaTime;

        Vector3 finalPosition =
            basePosition +
            reactionOffset +
            Vector3.up * reactionHeightOffset;

        transform.position = finalPosition;
    }


    private Vector3 GetLaneForward()
    {
        Vector3 forward = laneController != null
            ? laneController.GetForwardDirection()
            : Vector3.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        return forward.normalized;
    }

    private Vector3 GetLaneRight()
    {
        Vector3 forward = GetLaneForward();

        Vector3 right = Vector3.Cross(Vector3.up, forward);
        right.y = 0f;

        if (right.sqrMagnitude < 0.001f)
            right = transform.right;

        return right.normalized;
    }

    private int GetSideAwayFromPlayer()
    {
        if (player == null)
            return Random.value > 0.5f ? 1 : -1;

        Vector3 laneRight = GetLaneRight();

        Vector3 fromPlayer = basePosition - player.position;
        fromPlayer.y = 0f;

        float dot = Vector3.Dot(fromPlayer, laneRight);

        if (Mathf.Abs(dot) < 0.1f)
            return Random.value > 0.5f ? 1 : -1;

        return dot >= 0f ? 1 : -1;
    }

    private void FaceDirection(Vector3 direction, float duration)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        transform
            .DORotateQuaternion(
                Quaternion.LookRotation(direction.normalized, Vector3.up),
                duration
            );
    }

    private void CheckPlayerApproach()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= playerSpreadDistance)
            SpreadAwayFromPlayer();
    }

    private void CheckAIHit()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            aiSplashDetectRadius,
            hitBuffer,
            stampedeHazardMask,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < count; i++)
        {
            Collider hit = hitBuffer[i];

            if (hit == null)
                continue;

            StampedeHazardAI hazardAI = hit.GetComponentInParent<StampedeHazardAI>();

            if (hazardAI != null)
            {
                SplashAwayFromAI(hazardAI.transform);
                return;
            }
        }
    }

    private void SpreadAwayFromPlayer()
    {
        if (state != ClusterState.Waiting)
            return;

        state = ClusterState.Spreading;

        transform.DOKill();

        if (reactionSequence != null)
        {
            reactionSequence.Kill();
            reactionSequence = null;
        }

        animHandler?.SetAnimation(eCuteAnimalAnims.RUN);

        Vector3 laneRight = GetLaneRight();

        int side = GetSideAwayFromPlayer();

        Vector3 sideDirection = laneRight * side;

        // Small forward push only. The world movement already carries them along the lane.
        Vector3 forwardDirection = moveDirection.normalized;

        Vector3 targetOffset =
            sideDirection * Random.Range(spreadSideDistanceMin, spreadSideDistanceMax) +
            forwardDirection * Random.Range(spreadForwardDistanceMin, spreadForwardDistanceMax);

        float jumpT = 0f;

        reactionSequence = DOTween.Sequence();

        reactionSequence.Join(
            DOTween.To(
                () => reactionOffset,
                value => reactionOffset = value,
                targetOffset,
                spreadDuration
            ).SetEase(spreadEase)
        );

        reactionSequence.Join(
            DOTween.To(
                () => jumpT,
                value =>
                {
                    jumpT = value;
                    reactionHeightOffset = Mathf.Sin(jumpT * Mathf.PI) * spreadJumpPower;
                },
                1f,
                spreadDuration
            ).SetEase(Ease.Linear)
        );

        FaceDirection(sideDirection + forwardDirection * 0.35f, spreadDuration * 0.35f);

        reactionSequence.OnComplete(() =>
        {
            reactionHeightOffset = 0f;
            Destroy(gameObject, destroyAfterSpread);
        });
    }

    private void SplashAwayFromAI(Transform hazard)
    {
        if (state == ClusterState.Splashed)
            return;

        state = ClusterState.Splashed;

        transform.DOKill();

        if (reactionSequence != null)
        {
            reactionSequence.Kill();
            reactionSequence = null;
        }

        animHandler?.SetAnimation(eCuteAnimalAnims.DAMAGE);

        Vector3 splashDirection = GetCameraSplashDirection(hazard);

        Vector3 targetOffset =
            splashDirection * Random.Range(splashDistanceMin, splashDistanceMax);

        float jumpT = 0f;

        reactionSequence = DOTween.Sequence();

        reactionSequence.Join(
            DOTween.To(
                () => reactionOffset,
                value => reactionOffset = value,
                targetOffset,
                splashDuration
            ).SetEase(splashEase)
        );

        reactionSequence.Join(
            DOTween.To(
                () => jumpT,
                value =>
                {
                    jumpT = value;
                    reactionHeightOffset = Mathf.Sin(jumpT * Mathf.PI) * splashJumpPower;
                },
                1f,
                splashDuration
            ).SetEase(Ease.Linear)
        );

        reactionSequence.Join(
            transform.DORotate(
                transform.eulerAngles +
                new Vector3(
                    Random.Range(-splashSpinDegrees, splashSpinDegrees),
                    Random.Range(-splashSpinDegrees, splashSpinDegrees),
                    Random.Range(-splashSpinDegrees, splashSpinDegrees)
                ),
                splashDuration,
                RotateMode.FastBeyond360
            )
        );

        reactionSequence.OnComplete(() =>
        {
            reactionHeightOffset = 0f;
            Destroy(gameObject, destroyAfterSplash);
        });
    }

    private void OnDestroy()
    {
        StampedePropSpawnReservation.UnregisterActiveProp(transform);

        if (reactionSequence != null)
        {
            reactionSequence.Kill();
            reactionSequence = null;
        }

        transform.DOKill();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerSpreadDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aiSplashDetectRadius);
    }
}