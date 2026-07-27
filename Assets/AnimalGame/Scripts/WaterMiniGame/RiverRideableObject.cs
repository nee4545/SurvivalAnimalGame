using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RiverRideableObject : MonoBehaviour
{
    public static readonly List<RiverRideableObject> All =
        new List<RiverRideableObject>();

    [Header("Steering Visual Rotation")]
    public Transform visualRoot;

    [Header("Target Highlight")]
    public RiverRideableTargetOutline targetOutline;

    [Tooltip("If Visual Root is empty, rotate this object directly.")]
    public bool rotateSelfIfNoVisualRoot = true;

    public float maxSteerYawAngle = 18f;
    public float maxSteerRollAngle = 10f;
    public float steerRotationSharpness = 10f;

    [Header("Asymmetric Steering Visual")]
    public bool useAsymmetricYaw = true;

    [Tooltip("Extra multiplier for left steering yaw. 1 = same as normal.")]
    public float leftYawMultiplier = 1.25f;

    [Tooltip("Extra multiplier for right steering yaw. 1 = same as normal.")]
    public float rightYawMultiplier = 1f;

    [Header("Steering Direction Visual")]
    public bool useDirectionalSteerRotation = true;

    [Tooltip("How much the rideable points toward the side while steering.")]
    public float steerLookSideAmount = 0.35f;

    [Tooltip("How much the rider points toward the side while steering.")]
    public float riderLookSideAmount = 0.25f;

    [Header("Animation")]
    public CuteAnimalAnimHandler animHandler;
    public bool playRunAnimationDuringRiverEscape = true;

    [Header("Rider Rotation")]
    public bool rotateRiderWithSteer = true;
    public float riderYawAngle = 12f;
    public float riderRollAngle = 6f;

    private Quaternion visualStartLocalRotation;
    private float currentSteerInput;
    private float targetSteerInput;

    [Header("Mount")]
    public bool canBeMounted = true;
    public Transform mountPoint;
    public float fallbackMountHeight = 1f;

    [Header("Auto Correct Visual To Lane")]
    public bool autoCorrectVisualToRiverForward = true;

    [Tooltip("How much the animal still visually turns while holding left/right. Lower = faces lane more.")]
    [Range(0f, 1f)]
    public float heldSteerVisualAmount = 0.22f;

    [Tooltip("Extra visual kick when steering direction changes.")]
    public float steerChangeKickAmount = 0.75f;

    [Tooltip("How fast the animal corrects back to river/lane direction.")]
    public float steerKickReturnSharpness = 5f;

    [Tooltip("Maximum visual steering amount after auto-correction.")]
    public float maxAutoCorrectedVisualInput = 1f;

    [Header("Steering")]
    public bool allowMountedSteering = true;

    [Header("River Spawn Placement")]
    public float heightAboveWater = 0.15f;

    [Tooltip("Use this if the animal model faces sideways/backwards compared to Unity forward.")]
    public float spawnYawOffset = 0f;

    [Header("Water Splash")]
    public RiverRideableSplashController splashController;
    public bool useWaterSplash = true;

    private float rawSteerInput;
    private float visualSteerKick;

    private RiverEscapePlayerController currentRider;

    public bool isMovingAgainstRiver = false;

    [Header("Ride Duration")]
    public bool useRideDuration = true;
    public float rideDurationSeconds = 6f;

    [Tooltip("After the player leaves this rideable, it cannot be mounted again.")]
    public bool disableAfterPlayerLeaves = true;

    [Header("Sink On Expire")]
    public bool sinkOnRideExpire = true;
    public Transform sinkVisualRoot;
    public float sinkDepth = 1.5f;
    public float sinkDuration = 0.75f;

    [Header("Underwater Retire Motion")]
    public bool useUnderwaterRetireMotion = true;

    [Tooltip("Usually the animal model child, not the root.")]
    public Transform underwaterVisualRoot;

    [Tooltip("Positive or negative depending on your model forward direction.")]
    public float underwaterPitchAngle = 35f;

    public float underwaterPitchDuration = 0.35f;
    public float underwaterSinkDepth = 1.5f;
    public float underwaterSinkDuration = 0.8f;

    [Tooltip("How fast the retired rideable moves away underwater.")]
    public float underwaterMoveSpeed = 18f;

    [Tooltip("Destroy after it leaves the camera view.")]
    public bool destroyWhenOffCamera = true;

    [Header("Underwater Direction")]
    public bool invertUnderwaterMoveDirection = true;

    [Tooltip("Root will face underwater move direction while retiring.")]
    public bool alignRootToUnderwaterDirection = true;

    public float underwaterAlignSharpness = 10f;

    [Header("Underwater Straighten")]
    public bool straightenAfterPitch = true;
    public float underwaterStraightenDuration = 0.35f;

    [Header("Ride Expire Warning")]
    public bool useRideExpireWarning = true;

    [Tooltip("Exclamation mark particle object attached to this rideable.")]
    public GameObject rideExpireWarningObject;

    [Tooltip("Optional. If empty, particles are found automatically under Ride Expire Warning Object.")]
    public ParticleSystem[] rideExpireWarningParticles;

    [Header("Scene Authored Rideable")]
    public bool isSceneAuthoredRideable;
    public bool destroySceneAuthoredOnDespawn = true;

    [Header("Underwater Destroy Timing")]
    public bool waitForUnderwaterAnimationBeforeDestroy = true;
    public float extraDestroyDelayAfterSink = 0.35f;

    private Vector3 defaultUnderwaterLocalPosition;
    private Quaternion defaultUnderwaterLocalRotation;
    private bool cachedUnderwaterDefaults;

    private bool underwaterAnimationFinished;

    private bool isRideExpireWarningActive;

    public Camera retireCamera;
    public float offCameraViewportPadding = 0.25f;
    public float maxRetiredLifetime = 6f;

    private bool isMovingUnderwater;
    private float retiredLifetime;
    private float offCameraTimer;
    private Vector3 underwaterMoveDirection;
    private Coroutine underwaterRoutine;

    private bool isRetiring;
    private Coroutine sinkRoutine;

    private bool defaultCanBeMounted;
    private Vector3 defaultSinkLocalPosition;
    private Quaternion defaultSinkLocalRotation;
    private bool cachedSinkDefaults;

    public bool IsAvailable
    {
        get
        {
            return gameObject.activeInHierarchy &&
                   enabled &&
                   canBeMounted &&
                   !isRetiring &&
                   currentRider == null;
        }
    }

    public RiverEscapePlayerController CurrentRider
    {
        get { return currentRider; }
    }

    public bool HasRider
    {
        get { return currentRider != null; }
    }

    public bool IsRetiring
    {
        get { return isRetiring; }
    }

    private void Awake()
    {
        defaultCanBeMounted = canBeMounted;
        CacheSinkDefaults();
        CacheUnderwaterDefaults();
    }

    private bool TryReturnSceneAuthoredRideableToOriginalTile()
    {
        if (!isSceneAuthoredRideable)
            return false;

        RiverSceneJumpRideable sceneJumpRideable =
            GetComponent<RiverSceneJumpRideable>();

        if (sceneJumpRideable == null)
            return false;

        if (!sceneJumpRideable.returnToOriginalTileOnDespawn)
            return false;

        sceneJumpRideable.ReturnToOriginalTile();
        return true;
    }

    private void CacheUnderwaterDefaults()
    {
        if (cachedUnderwaterDefaults)
            return;

        Transform target = GetUnderwaterVisualTarget();

        if (target == null)
            return;

        defaultUnderwaterLocalPosition = target.localPosition;
        defaultUnderwaterLocalRotation = target.localRotation;
        cachedUnderwaterDefaults = true;
    }

    private void ResetRetireMotionStateForReuse()
    {
        isMovingUnderwater = false;
        isRetiring = false;

        retiredLifetime = 0f;
        offCameraTimer = 0f;
        underwaterAnimationFinished = false;
        underwaterMoveDirection = Vector3.zero;

        if (underwaterRoutine != null)
        {
            StopCoroutine(underwaterRoutine);
            underwaterRoutine = null;
        }

        if (sinkRoutine != null)
        {
            StopCoroutine(sinkRoutine);
            sinkRoutine = null;
        }

        Transform sinkTarget = GetSinkTarget();

        if (sinkTarget != null && cachedSinkDefaults)
        {
            sinkTarget.localPosition = defaultSinkLocalPosition;
            sinkTarget.localRotation = defaultSinkLocalRotation;
        }

        Transform underwaterTarget = GetUnderwaterVisualTarget();

        if (underwaterTarget != null && cachedUnderwaterDefaults)
        {
            underwaterTarget.localPosition = defaultUnderwaterLocalPosition;
            underwaterTarget.localRotation = defaultUnderwaterLocalRotation;
        }
    }

    public void SetRideExpireWarningActive(bool active)
    {
        if (!useRideExpireWarning)
            active = false;

        CacheRideExpireWarningParticles();

        bool objectAlreadyCorrect =
            rideExpireWarningObject == null ||
            rideExpireWarningObject.activeSelf == active;

        if (isRideExpireWarningActive == active && objectAlreadyCorrect)
            return;

        isRideExpireWarningActive = active;

        if (rideExpireWarningObject != null)
            rideExpireWarningObject.SetActive(active);

        if (rideExpireWarningParticles == null)
            return;

        for (int i = 0; i < rideExpireWarningParticles.Length; i++)
        {
            ParticleSystem particle = rideExpireWarningParticles[i];

            if (particle == null)
                continue;

            if (active)
            {
                particle.Clear(true);
                particle.Play(true);
            }
            else
            {
                particle.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
            }
        }
    }

    private void CacheRideExpireWarningParticles()
    {
        if (rideExpireWarningParticles != null &&
            rideExpireWarningParticles.Length > 0)
            return;

        if (rideExpireWarningObject != null)
        {
            rideExpireWarningParticles =
                rideExpireWarningObject.GetComponentsInChildren<ParticleSystem>(true);
        }
    }

    private Transform GetUnderwaterVisualTarget()
    {
        if (underwaterVisualRoot != null)
            return underwaterVisualRoot;

        if (sinkVisualRoot != null)
            return sinkVisualRoot;

        if (visualRoot != null)
            return visualRoot;

        // Important:
        // Do NOT return transform here.
        // Returning the root transform makes ResetForSpawnerReuse()
        // restore the animal's old localPosition after it has been reparented.
        return null;
    }

    private Transform GetUnderwaterRetireTarget()
    {
        Transform target = GetUnderwaterVisualTarget();

        if (target != null)
            return target;

        // Only use root while actually retiring/sinking.
        // Do not use root for cached reset defaults.
        return transform;
    }


    private Transform GetSinkTarget()
    {
        if (sinkVisualRoot != null)
            return sinkVisualRoot;

        if (visualRoot != null)
            return visualRoot;

        return null;
    }

    private void CacheSinkDefaults()
    {
        if (cachedSinkDefaults)
            return;

        Transform target = GetSinkTarget();

        if (target == null)
            return;

        defaultSinkLocalPosition = target.localPosition;
        defaultSinkLocalRotation = target.localRotation;
        cachedSinkDefaults = true;
    }

    public void ResetForSpawnerReuse()
    {
        CacheSinkDefaults();
        CacheUnderwaterDefaults();

        ResetRetireMotionStateForReuse();

        currentRider = null;
        canBeMounted = defaultCanBeMounted;

        SetTargetHighlighted(false);
        StopRidingSplash();
        SetRideExpireWarningActive(false);

        ResetSteeringVisualInput();
        CacheVisualRotation();
    }

    public void PrepareForPool()
    {
        currentRider = null;

        ResetRetireMotionStateForReuse();

        SetTargetHighlighted(false);
        StopRidingSplash();
        SetRideExpireWarningActive(false);
    }

    private float GetVisualYawAmount()
    {
        float input = currentSteerInput;

        float multiplier = 1f;

        if (useAsymmetricYaw)
        {
            if (input < 0f)
                multiplier = leftYawMultiplier;
            else if (input > 0f)
                multiplier = rightYawMultiplier;
        }

        float yawAmount = input * maxSteerYawAngle * multiplier;

        if (useDirectionalSteerRotation)
            yawAmount *= 0.5f;

        return yawAmount;
    }

    private void OnEnable()
    {
        if (!All.Contains(this))
            All.Add(this);

        CacheVisualRotation();
        ResetSteeringVisualInput();

   
        animHandler = GetComponent<CuteAnimalAnimHandler>();

        if (targetOutline == null)
            targetOutline = GetComponent<RiverRideableTargetOutline>();

        if (splashController == null)
            splashController = GetComponentInChildren<RiverRideableSplashController>(true);

        if (splashController != null)
        {
            splashController.playContinuousOnEnable = false;
            splashController.StopContinuousImmediate();
        }

        isRetiring = false;

        if (sinkRoutine != null)
        {
            StopCoroutine(sinkRoutine);
            sinkRoutine = null;
        }

        PlayRunAnimation();
        isRideExpireWarningActive = true;
        SetRideExpireWarningActive(false);
    }

    public void SetSplashWaterY(float waterY)
    {
        if (!useWaterSplash)
            return;

        if (splashController == null)
            splashController = GetComponentInChildren<RiverRideableSplashController>(true);

        if (splashController == null)
            return;

        splashController.waterY = waterY;
    }

    public float GetRideDuration()
    {
        return Mathf.Max(0.1f, rideDurationSeconds);
    }

    public void ReleaseAfterPlayerJump(
        float flowSpeed,
        bool moveOppositeRiverForward
    )
    {
        if (disableAfterPlayerLeaves)
            canBeMounted = false;

        SetTargetHighlighted(false);
        SetRider(null);

        BeginFlowAwayIfUnmanaged(
            flowSpeed,
            moveOppositeRiverForward
        );

        SetRideExpireWarningActive(false);
    }

    public void ExpireRideAndSink(
    float flowSpeed,
    bool moveOppositeRiverForward
)
    {
        if (isRetiring)
            return;

        isRetiring = true;
        canBeMounted = false;

        SetTargetHighlighted(false);
        SetRider(null);
        SetRideExpireWarningActive(false);

        RiverRideableAIBehavior aiBehavior =
            GetComponent<RiverRideableAIBehavior>();

        if (aiBehavior != null)
        {
            aiBehavior.StopSelfFlow();

            underwaterMoveDirection =
                aiBehavior.GetRiverMoveDirection(moveOppositeRiverForward);
        }
        else
        {
            underwaterMoveDirection = transform.forward;
        }

        if (invertUnderwaterMoveDirection)
            underwaterMoveDirection *= -1f;

        underwaterMoveDirection.y = 0f;

        if (underwaterMoveDirection.sqrMagnitude < 0.001f)
            underwaterMoveDirection = transform.forward;

        underwaterMoveDirection.Normalize();

        underwaterMoveDirection.y = 0f;

        if (underwaterMoveDirection.sqrMagnitude < 0.001f)
            underwaterMoveDirection = transform.forward;

        underwaterMoveDirection.Normalize();

        isMovingUnderwater = true;
        retiredLifetime = 0f;
        offCameraTimer = 0f;
        underwaterAnimationFinished = false;

        if (underwaterRoutine != null)
            StopCoroutine(underwaterRoutine);

        if (useUnderwaterRetireMotion)
            underwaterRoutine = StartCoroutine(UnderwaterRetireRoutine());
    }

    private IEnumerator UnderwaterRetireRoutine()
    {
        Transform target = GetUnderwaterRetireTarget();

        Vector3 startLocalPosition = target.localPosition;
        Vector3 endLocalPosition =
            startLocalPosition + Vector3.down * underwaterSinkDepth;

        Quaternion startLocalRotation = target.localRotation;

        Quaternion pitchedLocalRotation =
            startLocalRotation *
            Quaternion.Euler(underwaterPitchAngle, 0f, 0f);

        float pitchDuration = Mathf.Max(0.01f, underwaterPitchDuration);
        float sinkDuration = Mathf.Max(0.01f, underwaterSinkDuration);
        float straightenDuration = Mathf.Max(0.01f, underwaterStraightenDuration);

        float rotationDuration =
            straightenAfterPitch
                ? pitchDuration + straightenDuration
                : pitchDuration;

        float totalDuration =
            Mathf.Max(rotationDuration, sinkDuration);

        float timer = 0f;

        while (timer < totalDuration)
        {
            timer += Time.deltaTime;

            float sinkT = Mathf.Clamp01(timer / sinkDuration);
            sinkT = Mathf.SmoothStep(0f, 1f, sinkT);

            target.localPosition =
                Vector3.Lerp(
                    startLocalPosition,
                    endLocalPosition,
                    sinkT
                );

            if (timer <= pitchDuration)
            {
                float pitchT = Mathf.Clamp01(timer / pitchDuration);
                pitchT = Mathf.SmoothStep(0f, 1f, pitchT);

                target.localRotation =
                    Quaternion.Slerp(
                        startLocalRotation,
                        pitchedLocalRotation,
                        pitchT
                    );
            }
            else if (straightenAfterPitch)
            {
                float straightenT =
                    Mathf.Clamp01((timer - pitchDuration) / straightenDuration);

                straightenT = Mathf.SmoothStep(0f, 1f, straightenT);

                target.localRotation =
                    Quaternion.Slerp(
                        pitchedLocalRotation,
                        startLocalRotation,
                        straightenT
                    );
            }
            else
            {
                target.localRotation = pitchedLocalRotation;
            }

            yield return null;
        }

        target.localPosition = endLocalPosition;

        if (straightenAfterPitch)
            target.localRotation = startLocalRotation;
        else
            target.localRotation = pitchedLocalRotation;

        yield return new WaitForSeconds(extraDestroyDelayAfterSink);

        underwaterAnimationFinished = true;
    }

    private void BeginFlowAwayIfUnmanaged(
        float flowSpeed,
        bool moveOppositeRiverForward
    )
    {
        RiverRideableAIBehavior aiBehavior =
            GetComponent<RiverRideableAIBehavior>();

        if (aiBehavior == null)
            return;

        if (!aiBehavior.IsInitialized)
            return;

        aiBehavior.BeginSelfFlow(
            flowSpeed,
            moveOppositeRiverForward
        );
    }

    private IEnumerator SinkRoutine()
    {
        Transform target =
            sinkVisualRoot != null
                ? sinkVisualRoot
                : visualRoot;

        if (target == null)
            target = transform;

        Vector3 startPosition = target.position;
        Vector3 endPosition = startPosition + Vector3.down * sinkDepth;

        float timer = 0f;

        while (timer < sinkDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / sinkDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            target.position = Vector3.Lerp(
                startPosition,
                endPosition,
                t
            );

            yield return null;
        }

        target.position = endPosition;
    }

    public void PlayRidingSplash()
    {
        if (!useWaterSplash)
            return;

        if (splashController == null)
            splashController = GetComponentInChildren<RiverRideableSplashController>(true);

        if (splashController == null)
            return;

        splashController.PlayContinuous();
    }

    public void StopRidingSplash()
    {
        if (splashController == null)
            splashController = GetComponentInChildren<RiverRideableSplashController>(true);

        if (splashController == null)
            return;

        splashController.StopContinuous();
    }

    public void PlayLandingSplash()
    {
        if (!useWaterSplash)
            return;

        if (splashController == null)
            splashController = GetComponentInChildren<RiverRideableSplashController>(true);

        if (splashController == null)
            return;

        splashController.PlayLandingBurst();
    }

    public void SetTargetHighlighted(bool highlighted)
    {
        if (targetOutline == null)
            return;

        targetOutline.SetHighlighted(highlighted);
    }

    private void PlayRunAnimation()
    {
        if (!playRunAnimationDuringRiverEscape)
            return;

        if (animHandler == null)
            return;

        animHandler.SetAnimation(eCuteAnimalAnims.WALK);
    }

    private void Update()
    {
        UpdateSteeringVisualRotation();
        UpdateAnims();

        UpdateUnderwaterRetireMotion();
    }

    private void UpdateUnderwaterRetireMotion()
    {
        if (!isMovingUnderwater)
            return;

        transform.position +=
            underwaterMoveDirection * underwaterMoveSpeed * Time.deltaTime;

        if (alignRootToUnderwaterDirection)
        {
            Vector3 lookDirection = underwaterMoveDirection;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(
                        lookDirection.normalized,
                        Vector3.up
                    ) *
                    Quaternion.Euler(0f, spawnYawOffset, 0f);

                transform.rotation =
                    Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        1f - Mathf.Exp(-underwaterAlignSharpness * Time.deltaTime)
                    );
            }
        }

        retiredLifetime += Time.deltaTime;

        bool canDestroyNow =
    !waitForUnderwaterAnimationBeforeDestroy ||
    underwaterAnimationFinished;

        if (canDestroyNow && destroyWhenOffCamera && IsOutsideRetireCamera())
        {
            offCameraTimer += Time.deltaTime;

            if (offCameraTimer >= 0.25f)
            {
                if (TryReturnSceneAuthoredRideableToOriginalTile())
                    return;

                Destroy(gameObject);
                return;
            }
        }
        else
        {
            offCameraTimer = 0f;
        }

        if (canDestroyNow && retiredLifetime >= maxRetiredLifetime)
        {
            if (TryReturnSceneAuthoredRideableToOriginalTile())
                return;

            Destroy(gameObject);
        }
    }

    private bool IsOutsideRetireCamera()
    {
        Camera cam = retireCamera;

        if (cam == null)
            cam = Camera.current;

        if (cam == null)
            return false;

        Vector3 viewportPoint = cam.WorldToViewportPoint(transform.position);

        if (viewportPoint.z < 0f)
            return true;

        return viewportPoint.x < -offCameraViewportPadding ||
               viewportPoint.x > 1f + offCameraViewportPadding ||
               viewportPoint.y < -offCameraViewportPadding ||
               viewportPoint.y > 1f + offCameraViewportPadding;
    }


    void UpdateAnims()
    {
        if(isMovingAgainstRiver)
        {
            animHandler.SetAnimation(eCuteAnimalAnims.RUN);
            return;
        }


        if(!HasRider)
        {
            animHandler.SetAnimation(eCuteAnimalAnims.IDLE);
        }
        else
        {
            animHandler.SetAnimation(eCuteAnimalAnims.RUN);
        }
    }

    private void CacheVisualRotation()
    {
        Transform target = GetVisualRotationTarget();

        if (target != null)
            visualStartLocalRotation = target.localRotation;
    }

    private void ResetSteeringVisualInput()
    {
        currentSteerInput = 0f;
        targetSteerInput = 0f;
        rawSteerInput = 0f;
        visualSteerKick = 0f;
    }

    private Transform GetVisualRotationTarget()
    {
        if (visualRoot != null)
            return visualRoot;

        RiverRideableAIBehavior aiBehavior = GetComponent<RiverRideableAIBehavior>();

        if (aiBehavior != null)
        {
            // When unmounted, AI owns the root rotation.
            if (currentRider == null)
                return null;

            // When mounted, player steering is allowed to rotate the rideable.
            if (rotateSelfIfNoVisualRoot)
                return transform;

            return null;
        }

        if (rotateSelfIfNoVisualRoot)
            return transform;

        return null;
    }

    private void UpdateSteeringVisualRotation()
    {
        if (autoCorrectVisualToRiverForward)
        {
            visualSteerKick = Mathf.Lerp(
                visualSteerKick,
                0f,
                1f - Mathf.Exp(-steerKickReturnSharpness * Time.deltaTime)
            );

            targetSteerInput = Mathf.Clamp(
                rawSteerInput * heldSteerVisualAmount + visualSteerKick,
                -maxAutoCorrectedVisualInput,
                maxAutoCorrectedVisualInput
            );
        }

        currentSteerInput = Mathf.Lerp(
            currentSteerInput,
            targetSteerInput,
            1f - Mathf.Exp(-steerRotationSharpness * Time.deltaTime)
        );

        Transform target = GetVisualRotationTarget();

        if (target == null)
            return;

        Quaternion targetRotation;

        float yawAmount = GetVisualYawAmount();

        targetRotation =
            visualStartLocalRotation *
            Quaternion.Euler(
                0f,
                yawAmount,
                -currentSteerInput * maxSteerRollAngle
            );

        target.localRotation = targetRotation;
    }

    public void SetSteerInput(float input)
    {
        input = Mathf.Clamp(input, -1f, 1f);

        if (!autoCorrectVisualToRiverForward)
        {
            rawSteerInput = input;
            targetSteerInput = input;
            return;
        }

        float inputDelta = input - rawSteerInput;

        rawSteerInput = input;

        visualSteerKick += inputDelta * steerChangeKickAmount;

        visualSteerKick = Mathf.Clamp(
            visualSteerKick,
            -maxAutoCorrectedVisualInput,
            maxAutoCorrectedVisualInput
        );

        targetSteerInput = Mathf.Clamp(
            rawSteerInput * heldSteerVisualAmount + visualSteerKick,
            -maxAutoCorrectedVisualInput,
            maxAutoCorrectedVisualInput
        );
    }

    public Quaternion GetRiderRotation(Vector3 riverForward, Vector3 riverRight)
    {
        riverForward.y = 0f;
        riverRight.y = 0f;

        if (riverForward.sqrMagnitude < 0.001f)
            riverForward = Vector3.forward;

        if (riverRight.sqrMagnitude < 0.001f)
            riverRight = Vector3.right;

        riverForward.Normalize();
        riverRight.Normalize();

        Vector3 lookDirection = riverForward;

        if (useDirectionalSteerRotation)
        {
            lookDirection =
                (riverForward + riverRight * currentSteerInput * riderLookSideAmount)
                .normalized;
        }

        Quaternion baseRotation = Quaternion.LookRotation(
            lookDirection,
            Vector3.up
        );

        if (!rotateRiderWithSteer)
            return baseRotation;

        return baseRotation *
               Quaternion.Euler(
                   0f,
                   0f,
                   -currentSteerInput * riderRollAngle
               );
    }

    private void OnDisable()
    {
        All.Remove(this);

        if (currentRider != null)
            currentRider = null;
    }

    private void OnDestroy()
    {
        All.Remove(this);
    }

    public Vector3 GetMountPosition()
    {
        if (mountPoint != null)
            return mountPoint.position;

        return transform.position + Vector3.up * fallbackMountHeight;
    }

    public void SetRider(RiverEscapePlayerController rider)
    {
        currentRider = rider;

        ResetSteeringVisualInput();

        if (currentRider == null)
        {
            SetRideExpireWarningActive(false);
        }

        RiverRideableAIBehavior aiBehavior = GetComponent<RiverRideableAIBehavior>();

        if (currentRider != null)
        {
            // Player just landed/mounted.
            // Remove AI side-to-side rotation before mounted steering starts.
            if (aiBehavior != null && aiBehavior.IsInitialized)
            {
                aiBehavior.ResetSideMovementVisualYaw();
                aiBehavior.SnapRotationForMount();
            }

            // Now cache the corrected forward-facing rotation as the mounted base.
            CacheVisualRotation();

            PlayRidingSplash();
        }
        else
        {
            StopRidingSplash();

            if (aiBehavior != null && aiBehavior.IsInitialized)
            {
                aiBehavior.SyncAIToCurrentPosition();
                aiBehavior.SnapRotationToCorrectFacing();
            }

            CacheVisualRotation();
        }
    }

    public void MoveLaterally(
    float amount,
    float steerInput,
    Vector3 riverRight,
    Transform riverCenterReference,
    float riverHalfWidth
    )
    {
        if (!allowMountedSteering)
            return;

        SetSteerInput(steerInput);

        riverRight.y = 0f;

        if (riverRight.sqrMagnitude < 0.001f)
            return;

        riverRight.Normalize();

        Vector3 position = transform.position;

        if (riverCenterReference != null)
        {
            Vector3 fromCenter = position - riverCenterReference.position;
            fromCenter.y = 0f;

            float currentSide = Vector3.Dot(fromCenter, riverRight);

            float targetSide = Mathf.Clamp(
                currentSide + amount,
                -riverHalfWidth,
                riverHalfWidth
            );

            float correction = targetSide - currentSide;
            position += riverRight * correction;
        }
        else
        {
            position += riverRight * amount;
        }

        transform.position = position;
    }
}