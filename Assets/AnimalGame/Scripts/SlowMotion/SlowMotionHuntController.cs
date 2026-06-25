using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Terresquall;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class SlowMotionHuntController : MonoBehaviour
{
    [Header("References")]
    public CCActor actor;
    public SlowMotionHuntUI huntUI;

    [Header("Input")]
    public InputActionReference slowHuntAction;
    public InputActionReference tapAction;

    [Header("Activation")]
    public bool requireTriggerZone = true;
    public bool requireRunning = true;
    public float cooldown = 5f;

    [Header("Enemy Search")]
    public LayerMask enemyLayer;
    public float defaultEnemyScanRange = 14f;
    public float maxLeapDistance = 16f;

    [Tooltip("Enemies behind the player are ignored unless this is disabled.")]
    public bool preferForwardDirection = true;

    [Range(-1f, 1f)]
    public float minimumForwardDot = 0.1f;

    [Header("Cluster Targeting")]
    public float clusterRadius = 5f;
    public int minimumEnemiesForCluster = 2;
    [Header("Jump Distance Preference")]
    public float minimumJumpDistance = 6f;
    public float idealJumpDistance = 11f;

    [Tooltip("Penalty applied when a cluster is too close.")]
    public float tooClosePenalty = 80f;

    [Tooltip("Penalty applied when a cluster is away from the ideal jump distance.")]
    public float idealDistancePenalty = 12f;

    private readonly List<Health> visibleEnemyHealths = new List<Health>(32);

    [Tooltip("How strongly the system prefers larger enemy groups.")]
    public float clusterCountWeight = 100f;

    [Tooltip("How strongly the system prefers clusters in front of the player.")]
    public float forwardWeight = 35f;

    [Tooltip("How strongly the system prefers closer clusters.")]
    public float distancePenalty = 3f;

    [Header("Slow Motion")]
    [Range(0.05f, 1f)] public float slowTimeScale = 0.18f;
    public float modeDuration = 2.2f;
    public float lockedMoveSpeed = 3.5f;

    [Header("Hunt Camera Preview")]
    public bool useHuntCameraPreview = true;
    public bool disableCameraSystemDuringPreview = true;

    public float cameraLookAtJumpDuration = 0.35f;
    public float cameraHoldOnJumpPoint = 0.25f;
    public float cameraReturnToPlayerDuration = 0.45f;

    public Vector3 jumpPointLookOffset = new Vector3(0f, 1.2f, 0f);
    public Vector3 playerLookOffset = new Vector3(0f, 1.5f, 0f);

    public Ease cameraLookEase = Ease.OutSine;
    public Ease cameraReturnEase = Ease.InOutSine;

    private Tween huntCameraTween;
    private bool cameraSystemWasEnabled;

    [Header("Impact Leap")]
    public float leapDuration = 0.48f;
    public float leapHeight = 3.6f;
    public float impactRadius = 4.2f;
    public float criticalDamageMultiplier = 2.5f;
    public float knockbackStrength = 1f;

    [Header("Impact Knockback")]
    public float aoeKnockbackDistance = 3.5f;
    public float aoeKnockbackHeight = 1.2f;
    public float aoeKnockbackDuration = 0.35f;

    [Header("Landing Grounding")]
    public LayerMask groundLayer;
    public float groundRayHeight = 20f;
    public float groundRayDistance = 50f;

    [Header("Cluster Debug")]
    public bool debugClusterTargeting = true;

    [Header("Hunt Camera")]
    public Camera mainCamera;

    [Tooltip("Optional. Assign your main camera's CameraSystem script here.")]
    public CameraSystem mainCameraSystem;
    private AudioListener mainAudioListener;

    [Header("Hunt VFX")]
    public GameObject preJumpDustVFX;
    public GameObject impactBloodVFX;

    [Tooltip("Where dust should spawn relative to the player.")]
    public Vector3 dustSpawnOffset = new Vector3(0f, 0.05f, 0f);

    [Tooltip("Where blood splatter should spawn relative to the impact point.")]
    public Vector3 bloodSpawnOffset = new Vector3(0f, 0.15f, 0f);

    [Tooltip("How long before destroying spawned VFX objects.")]
    public float vfxDestroyDelay = 3f;

    [Tooltip("Small delay after dust burst before the jump begins.")]
    public float preJumpVFXDelay = 0.12f;

    [Header("Auto Hunt Trigger")]
    public bool enableAutoHuntTrigger = true;

    [Tooltip("Random auto hunt check interval minimum.")]
    public float autoCheckIntervalMin = 10f;

    [Tooltip("Random auto hunt check interval maximum.")]
    public float autoCheckIntervalMax = 15f;

    [Tooltip("Auto hunt triggers only if this many animals are inside the target area.")]
    public int autoMinimumAnimalsInZone = 10;

    [Tooltip("If enabled, player must be running for auto hunt to trigger.")]
    public bool autoRequireRunning = false;

    [Tooltip("Local offset from player. Z moves the sphere forward from player.")]
    public Vector3 autoTargetAreaLocalOffset = new Vector3(0f, 0f, 10f);

    [Tooltip("Radius of the auto hunt targetable area.")]
    public float autoTargetAreaRadius = 8f;

    [Tooltip("If true, auto hunt uses this sphere for cluster targeting.")]
    public bool autoHuntUsesAutoTargetArea = true;

    [Header("Auto Hunt Gizmo")]
    public Color autoTargetAreaGizmoColor = new Color(0f, 0.8f, 1f, 0.22f);
    public Color autoTargetAreaWireColor = new Color(0f, 0.9f, 1f, 1f);

    private float nextAutoHuntCheckTime;
    private bool usingAutoHuntTargetArea;

    [Header("Mobile Controls")]
    public VirtualJoystick virtualJoystick;

    [Header("Hunt Popups")]
    public string criticalPopupText = "CRITICAL !";
    public string missPopupText = "MISS !";

    public Vector3 criticalPopupOffset = new Vector3(0f, 2f, 0f);
    public Vector3 missPopupOffset = new Vector3(0f, 2f, 0f);


    [Header("Miss")]
    public float missRecoveryDuration = 0.35f;

    private readonly Collider[] enemyBuffer = new Collider[64];
    private readonly List<Transform> visibleEnemies = new List<Transform>(32);
    private readonly List<Transform> bestCluster = new List<Transform>(16);
    private readonly HashSet<Health> damagedThisImpact = new HashSet<Health>();
    private readonly List<Transform> clusterCandidates = new List<Transform>(64);
    private readonly List<Transform> tempCluster = new List<Transform>(64);

    private SlowMotionHuntTriggerZone currentZone;
    private bool isActive;
    private bool tapRequested;
    private float nextReadyTime;

    private Vector3 lockedMoveDirection;
    private Vector3 chosenClusterCenter;
    private float originalTimeScale;
    private float originalFixedDeltaTime;

    private void Awake()
    {
        if (!actor)
            actor = GetComponent<CCActor>();

        if (actor != null && enemyLayer.value == 0)
            enemyLayer = actor.enemyLayer;

        if (!mainCamera)
            mainCamera = Camera.main;

        if (mainCamera && !mainCameraSystem)
            mainCameraSystem = mainCamera.GetComponent<CameraSystem>();

        if (mainCamera)
        {
            mainAudioListener = mainCamera.GetComponent<AudioListener>();
        }

        ScheduleNextAutoHuntCheck();
    }

    private void OnEnable()
    {
        slowHuntAction?.action.Enable();
        tapAction?.action.Enable();
    }

    private void OnDisable()
    {
        slowHuntAction?.action.Disable();
        tapAction?.action.Disable();

        if (mainCameraSystem)
            mainCameraSystem.SetHuntZoom(false);

        SetJoystickVisible(true);

        RestoreTime();
    }

    public bool IsHuntGoingOn()
    {
        return isActive;
    }

    private IEnumerator PlayHuntCameraPreview()
    {
        if (!useHuntCameraPreview || mainCamera == null)
            yield break;

        Transform cam = mainCamera.transform;

        if (mainCameraSystem && disableCameraSystemDuringPreview)
        {
            cameraSystemWasEnabled = mainCameraSystem.enabled;
            mainCameraSystem.enabled = false;
        }

        huntCameraTween?.Kill();

        Vector3 jumpLookPoint = chosenClusterCenter + jumpPointLookOffset;

        Quaternion lookAtJumpRotation = Quaternion.LookRotation(
            jumpLookPoint - cam.position,
            Vector3.up
        );

        huntCameraTween = cam
            .DORotateQuaternion(lookAtJumpRotation, cameraLookAtJumpDuration)
            .SetEase(cameraLookEase)
            .SetUpdate(true);

        yield return huntCameraTween.WaitForCompletion();

        if (cameraHoldOnJumpPoint > 0f)
            yield return new WaitForSecondsRealtime(cameraHoldOnJumpPoint);

        Vector3 playerLookPoint = transform.position + playerLookOffset;

        Quaternion lookAtPlayerRotation = Quaternion.LookRotation(
            playerLookPoint - cam.position,
            Vector3.up
        );

        huntCameraTween = cam
            .DORotateQuaternion(lookAtPlayerRotation, cameraReturnToPlayerDuration)
            .SetEase(cameraReturnEase)
            .SetUpdate(true);

        yield return huntCameraTween.WaitForCompletion();

        if (mainCameraSystem && disableCameraSystemDuringPreview)
            mainCameraSystem.enabled = cameraSystemWasEnabled;
    }

    private void Update()
    {
        if (!actor || actor.isDead)
            return;

        if (!isActive)
        {

            //UpdateAutoHuntTrigger();
            if(currentZone != null)
                TryStartSlowMotionHunt();

            //if (slowHuntAction != null && slowHuntAction.action.triggered)
            //    TryStartSlowMotionHunt();

            return;
        }

        if (tapAction != null && tapAction.action.triggered)
            tapRequested = true;
    }

    private void SetJoystickVisible(bool visible)
    {
        if (virtualJoystick)
            virtualJoystick.SetJoystickVisibleForHunt(visible);
    }

    public void RequestTapFromButton()
    {
        tapRequested = true;
    }


    private Vector3 AutoTargetAreaWorldCenter
    {
        get
        {
            return transform.TransformPoint(autoTargetAreaLocalOffset);
        }
    }

    private void ScheduleNextAutoHuntCheck()
    {
        float min = Mathf.Max(0.1f, autoCheckIntervalMin);
        float max = Mathf.Max(min, autoCheckIntervalMax);

        nextAutoHuntCheckTime = Time.time + Random.Range(min, max);
    }

    private void UpdateAutoHuntTrigger()
    {
        if (!enableAutoHuntTrigger)
            return;

        if (isActive || actor == null || actor.isDead || actor.isInParabola)
            return;

        if (Time.time < nextAutoHuntCheckTime)
            return;

        ScheduleNextAutoHuntCheck();

        if (autoRequireRunning && !actor.isRunning)
            return;

        int animalsInZone = CountValidAnimalsInAutoTargetArea();

        if (animalsInZone >= autoMinimumAnimalsInZone)
        {
            TryStartAutoSlowMotionHunt();
        }
    }

    private int CountValidAnimalsInAutoTargetArea()
    {
        int validCount = 0;

        Vector3 center = AutoTargetAreaWorldCenter;

        int count = Physics.OverlapSphereNonAlloc(
            center,
            autoTargetAreaRadius,
            enemyBuffer,
            enemyLayer
        );

        // Prevent duplicate counts from animals with multiple colliders.
        HashSet<Health> counted = new HashSet<Health>();

        for (int i = 0; i < count; i++)
        {
            Collider col = enemyBuffer[i];
            if (!col)
                continue;

            Health hp = col.GetComponentInParent<Health>();

            if (!hp || hp.IsDead)
                continue;

            if (counted.Contains(hp))
                continue;

            counted.Add(hp);
            validCount++;
        }

        return validCount;
    }

    private void TryStartAutoSlowMotionHunt()
    {
        if (Time.time < nextReadyTime)
            return;

        if (isActive || actor == null || actor.isInParabola)
            return;

        usingAutoHuntTargetArea = autoHuntUsesAutoTargetArea;

        if (!FindBestEnemyCluster(out chosenClusterCenter))
        {
            usingAutoHuntTargetArea = false;
            return;
        }

        Vector3 dir = actor.moveDirection;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.05f)
            dir = transform.forward;

        lockedMoveDirection = dir.normalized;

        StartCoroutine(SlowMotionHuntRoutine());
    }


    public void EnterHuntZone(SlowMotionHuntTriggerZone zone)
    {
        currentZone = zone;
    }

    public void ExitHuntZone(SlowMotionHuntTriggerZone zone)
    {
        if (currentZone == zone)
            currentZone = null;
    }


    private void SpawnVFX(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!prefab)
            return;

        GameObject instance = Instantiate(prefab, position, rotation);

        if (vfxDestroyDelay > 0f)
            Destroy(instance, vfxDestroyDelay);
    }

    public void TryStartSlowMotionHunt()
    {
        Debug.Log("[SlowHunt] TryStartSlowMotionHunt called");

        if (Time.time < nextReadyTime)
        {
            Debug.Log("[SlowHunt] Failed: Cooldown active");
            return;
        }

        if (isActive)
        {
            Debug.Log("[SlowHunt] Failed: Already active");
            return;
        }

        if (actor == null)
        {
            Debug.Log("[SlowHunt] Failed: Actor missing");
            return;
        }

        if (actor.isInParabola)
        {
            Debug.Log("[SlowHunt] Failed: Actor already in parabola");
            return;
        }

        if (requireTriggerZone && currentZone == null)
        {
            Debug.Log("[SlowHunt] Failed: Requires trigger zone but currentZone is null");
            return;
        }

        if (currentZone != null && !currentZone.allowSlowMotionHunt)
        {
            Debug.Log("[SlowHunt] Failed: Current zone does not allow slow hunt");
            return;
        }

        if (requireRunning && !actor.isRunning)
        {
            Debug.Log("[SlowHunt] Failed: Actor is not running");
            return;
        }

        if (!FindBestEnemyCluster(out chosenClusterCenter))
        {
            Debug.Log("[SlowHunt] Failed: No valid enemy cluster found");
            return;
        }

        Debug.Log("[SlowHunt] Success: Starting slow motion hunt");

        Vector3 dir = actor.moveDirection;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.05f)
            dir = transform.forward;

        lockedMoveDirection = dir.normalized;

        StartCoroutine(SlowMotionHuntRoutine());
    }

    private IEnumerator SlowMotionHuntRoutine()
    {
        isActive = true;
        tapRequested = false;

        SetJoystickVisible(false);

        actor.isSlowMotionHuntActive = true;
        actor.isAttackingLoop = false;
        actor.currentTarget = null;

        originalTimeScale = Time.timeScale;
        originalFixedDeltaTime = Time.fixedDeltaTime;

        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = originalFixedDeltaTime * slowTimeScale;

        if (mainCameraSystem)
            mainCameraSystem.SetHuntZoom(true);

        yield return StartCoroutine(PlayHuntCameraPreview());

        huntUI?.Show();
        actor.animHandler?.SetAnimation(eCuteAnimalAnims.RUN);

        float timer = modeDuration;
        bool resolved = false;

        while (timer > 0f && !resolved)
        {
            timer -= Time.unscaledDeltaTime;

            MovePlayerInLockedDirection();
            FaceMoveDirection();
            huntUI?.Tick();

            if (tapRequested)
            {
                bool success = huntUI != null && huntUI.IsArrowInGreenZone();

                if (success)
                    yield return StartCoroutine(DoClusterCriticalLeap());
                else
                    yield return StartCoroutine(DoMissRecovery());

                resolved = true;
            }

            yield return null;
        }

        EndSlowMotionHunt();
    }

    private bool FindBestEnemyCluster(out Vector3 clusterCenter)
    {
        clusterCenter = transform.position;

        visibleEnemyHealths.Clear();
        clusterCandidates.Clear();
        bestCluster.Clear();

        float leapLimit = currentZone != null ? currentZone.zoneMaxLeapDistance : maxLeapDistance;

        Vector3 searchCenter = transform.position;
        float searchRadius = defaultEnemyScanRange;

        if (usingAutoHuntTargetArea)
        {
            searchCenter = AutoTargetAreaWorldCenter;
            searchRadius = autoTargetAreaRadius;
            leapLimit = maxLeapDistance;
        }
        else if (currentZone != null && currentZone.useHuntTargetArea)
        {
            searchCenter = currentZone.TargetAreaWorldCenter;
            searchRadius = currentZone.TargetAreaWorldRadius;
        }
        else
        {
            searchCenter = transform.position;
            searchRadius = defaultEnemyScanRange;
        }

        int count = Physics.OverlapSphereNonAlloc(
            searchCenter,
            searchRadius,
            enemyBuffer,
            enemyLayer
        );

        // Step 1: collect unique valid animals
        for (int i = 0; i < count; i++)
        {
            Collider col = enemyBuffer[i];
            if (!col)
                continue;

            Health hp = col.GetComponentInParent<Health>();

            if (!hp || hp.IsDead)
                continue;

            if (visibleEnemyHealths.Contains(hp))
                continue;

            Transform enemyRoot = hp.transform;

            if (usingAutoHuntTargetArea)
            {
                Vector3 center = AutoTargetAreaWorldCenter;
                float radius = autoTargetAreaRadius;

                if ((enemyRoot.position - center).sqrMagnitude > radius * radius)
                    continue;
            }
            else if (currentZone != null && currentZone.useHuntTargetArea)
            {
                if (!currentZone.IsPointInsideTargetArea(enemyRoot.position))
                    continue;
            }

            Vector3 toEnemy = enemyRoot.position - transform.position;
            toEnemy.y = 0f;

            float distance = toEnemy.magnitude;

            if (distance > leapLimit)
                continue;

            // Do not choose animals too close to the player.
            if (distance < minimumJumpDistance)
                continue;

            if (preferForwardDirection)
            {
                if (toEnemy.sqrMagnitude < 0.01f)
                    continue;

                float dot = Vector3.Dot(transform.forward, toEnemy.normalized);

                if (dot < minimumForwardDot)
                    continue;
            }

            visibleEnemyHealths.Add(hp);
            clusterCandidates.Add(enemyRoot);
        }

        if (clusterCandidates.Count == 0)
        {
            if (debugClusterTargeting)
                Debug.Log("[SlowHunt] No cluster candidates found.");

            return false;
        }

        // Step 2: find the best dense cluster
        float bestScore = float.NegativeInfinity;
        Vector3 bestCenter = transform.position;

        for (int i = 0; i < clusterCandidates.Count; i++)
        {
            Transform seed = clusterCandidates[i];

            tempCluster.Clear();

            // Collect everyone near this seed.
            for (int j = 0; j < clusterCandidates.Count; j++)
            {
                Transform other = clusterCandidates[j];

                float distToSeed = Vector3.Distance(seed.position, other.position);

                if (distToSeed <= clusterRadius)
                    tempCluster.Add(other);
            }

            if (tempCluster.Count < minimumEnemiesForCluster)
                continue;

            // Step 3: calculate the TRUE center of the cluster.
            Vector3 center = Vector3.zero;

            for (int j = 0; j < tempCluster.Count; j++)
                center += tempCluster[j].position;

            center /= tempCluster.Count;

            Vector3 toCenter = center - transform.position;
            toCenter.y = 0f;

            float distanceToCenter = toCenter.magnitude;

            if (distanceToCenter < minimumJumpDistance)
                continue;

            if (distanceToCenter > leapLimit)
                continue;

            float forwardDot = toCenter.sqrMagnitude > 0.01f
                ? Vector3.Dot(transform.forward, toCenter.normalized)
                : 1f;

            if (preferForwardDirection && forwardDot < minimumForwardDot)
                continue;

            float distanceFromIdeal = Mathf.Abs(distanceToCenter - idealJumpDistance);

            // This rewards:
            // 1. Bigger groups
            // 2. Groups closer to the ideal leap distance
            // 3. Groups mostly in front of player
            float score =
                tempCluster.Count * clusterCountWeight +
                forwardDot * forwardWeight -
                distanceFromIdeal * idealDistancePenalty;

            if (score > bestScore)
            {
                bestScore = score;
                bestCenter = center;

                bestCluster.Clear();

                for (int j = 0; j < tempCluster.Count; j++)
                    bestCluster.Add(tempCluster[j]);
            }
        }

        // Step 4: fallback if no cluster found
        if (bestCluster.Count == 0)
        {
            Transform bestSingle = null;
            float bestSingleScore = float.NegativeInfinity;

            for (int i = 0; i < clusterCandidates.Count; i++)
            {
                Transform enemy = clusterCandidates[i];

                Vector3 toEnemy = enemy.position - transform.position;
                toEnemy.y = 0f;

                float distance = toEnemy.magnitude;

                if (distance < minimumJumpDistance || distance > leapLimit)
                    continue;

                float forwardDot = toEnemy.sqrMagnitude > 0.01f
                    ? Vector3.Dot(transform.forward, toEnemy.normalized)
                    : 1f;

                if (preferForwardDirection && forwardDot < minimumForwardDot)
                    continue;

                float distanceFromIdeal = Mathf.Abs(distance - idealJumpDistance);

                float score =
                    forwardDot * forwardWeight -
                    distanceFromIdeal * idealDistancePenalty;

                if (score > bestSingleScore)
                {
                    bestSingleScore = score;
                    bestSingle = enemy;
                }
            }

            if (!bestSingle)
            {
                if (debugClusterTargeting)
                    Debug.Log("[SlowHunt] No fallback target found.");

                return false;
            }

            bestCenter = bestSingle.position;
            bestCluster.Add(bestSingle);
        }

        // Step 5: ground the landing point using terrain/ground, not NavMesh.
        clusterCenter = GetGroundedLandingPoint(bestCenter);

        if (debugClusterTargeting)
        {
            Debug.Log(
                $"[SlowHunt] Selected cluster. Members: {bestCluster.Count}, Center: {clusterCenter}, Score: {bestScore}"
            );
        }

        return true;
    }

    private Vector3 GetGroundedLandingPoint(Vector3 rawPoint)
    {
        Vector3 rayStart = rawPoint + Vector3.up * groundRayHeight;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundRayDistance, groundLayer))
        {
            return hit.point;
        }

        // Fallback: keep original point if ground ray fails.
        return rawPoint;
    }

    private bool IsEnemyValid(Transform enemy)
    {
        if (!enemy)
            return false;

        Health hp = enemy.GetComponent<Health>();
        if (!hp || hp.IsDead)
            return false;

        return true;
    }

    private void MovePlayerInLockedDirection()
    {
        if (!actor.controller)
            return;

        Vector3 motion = lockedMoveDirection * lockedMoveSpeed;
        motion.y = actor.verticalVelocity;

        actor.controller.Move(motion * Time.unscaledDeltaTime);
    }

    private void FaceMoveDirection()
    {
        if (lockedMoveDirection.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(lockedMoveDirection);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            actor.rotationSpeed * Time.unscaledDeltaTime
        );
    }

    private IEnumerator DoClusterCriticalLeap()
    {
        RestoreTime();

        actor.isInParabola = true;

        // Dust burst before launch
        Vector3 dustPos = transform.position + dustSpawnOffset;
        SpawnVFX(preJumpDustVFX, dustPos, Quaternion.identity);


        actor.animHandler?.SetAnimation(eCuteAnimalAnims.ATTACK);

        // Tiny anticipation delay before the leap.
        // Uses real time so it still feels consistent around slow-mo transitions.
        if (preJumpVFXDelay > 0f)
            yield return new WaitForSecondsRealtime(preJumpVFXDelay);


        Vector3 start = transform.position;
        Vector3 end = chosenClusterCenter;

        float elapsed = 0f;

        while (elapsed < leapDuration)
        {
            float t = Mathf.Clamp01(elapsed / leapDuration);

            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y = Mathf.Lerp(start.y, end.y, t) + leapHeight * Mathf.Sin(Mathf.PI * t);

            if (actor.controller)
                actor.controller.enabled = false;

            transform.position = pos;

            if (actor.controller)
                actor.controller.enabled = true;

            Vector3 faceDir = end - transform.position;
            faceDir.y = 0f;

            if (faceDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(faceDir.normalized);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (actor.controller)
            actor.controller.enabled = false;

        transform.position = end;

        if (actor.controller)
            actor.controller.enabled = true;

        // Big blood impact at landing point
        Vector3 bloodPos = end + bloodSpawnOffset;
        Quaternion bloodRot = Quaternion.LookRotation(transform.forward, Vector3.up);
        SpawnVFX(impactBloodVFX, bloodPos, bloodRot);


        DealAreaCriticalDamage(end);

        if (DamagePopupSpawner.Instance != null)
        {
            DamagePopupSpawner.Instance.ShowTextPopup(
                end + criticalPopupOffset,
                criticalPopupText
            );
        }


        actor.isInParabola = false;
    }

    private void DealAreaCriticalDamage(Vector3 impactCenter)
    {
        damagedThisImpact.Clear();

        int damage = Mathf.RoundToInt(actor.attackDamage * criticalDamageMultiplier);

        int count = Physics.OverlapSphereNonAlloc(
            impactCenter,
            impactRadius,
            enemyBuffer,
            enemyLayer
        );

        for (int i = 0; i < count; i++)
        {
            Collider col = enemyBuffer[i];
            if (!col) continue;

            Health hp = col.GetComponentInParent<Health>();
            if (!hp || hp.IsDead)
                continue;

            if (damagedThisImpact.Contains(hp))
                continue;

            damagedThisImpact.Add(hp);

            hp.TakeDamage(damage, actor.transform);

            CuteAnimalAI ai = hp.GetComponent<CuteAnimalAI>();
            if (ai == null)
                ai = hp.GetComponentInParent<CuteAnimalAI>();

            if (ai != null)
            {
                Vector3 pushDir = ai.transform.position - impactCenter;
                pushDir.y = 0f;

                if (pushDir.sqrMagnitude < 0.01f)
                    pushDir = ai.transform.position - transform.position;

                pushDir.y = 0f;

                if (pushDir.sqrMagnitude < 0.01f)
                    pushDir = transform.forward;

                pushDir.Normalize();

                ai.StartParabolicAerialKnockback(
                    pushDir,
                    aoeKnockbackDistance,
                    aoeKnockbackHeight,
                    aoeKnockbackDuration
                );
            }
        }
    }

    private IEnumerator DoMissRecovery()
    {
        RestoreTime();

        actor.animHandler?.SetAnimation(eCuteAnimalAnims.IDLE);

        if (DamagePopupSpawner.Instance != null)
        {
            DamagePopupSpawner.Instance.ShowTextPopup(
                transform.position + missPopupOffset,
                missPopupText
            );
        }

        float timer = missRecoveryDuration;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }
    }

    private void EndSlowMotionHunt()
    {
        RestoreTime();
        huntCameraTween?.Kill();

        if (mainCameraSystem && disableCameraSystemDuringPreview)
            mainCameraSystem.enabled = true;


        huntUI?.Hide();

        if (mainCameraSystem)
            mainCameraSystem.SetHuntZoom(false);

        isActive = false;
        tapRequested = false;
        usingAutoHuntTargetArea = false;

        if (currentZone)
            currentZone = null;

        actor.isSlowMotionHuntActive = false;
        actor.isInParabola = false;
        actor.currentTarget = null;

        SetJoystickVisible(true);

        nextReadyTime = Time.time + cooldown;
    }

    private void RestoreTime()
    {
        Time.timeScale = originalTimeScale > 0f ? originalTimeScale : 1f;

        if (originalFixedDeltaTime > 0f)
            Time.fixedDeltaTime = originalFixedDeltaTime;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, defaultEnemyScanRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(chosenClusterCenter, impactRadius);

        if (!enableAutoHuntTrigger)
            return;

        Vector3 center = transform.TransformPoint(autoTargetAreaLocalOffset);
        float radius = autoTargetAreaRadius;

        Gizmos.color = autoTargetAreaGizmoColor;
        Gizmos.DrawSphere(center, radius);

        Gizmos.color = autoTargetAreaWireColor;
        Gizmos.DrawWireSphere(center, radius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, center);
    }
#endif
}