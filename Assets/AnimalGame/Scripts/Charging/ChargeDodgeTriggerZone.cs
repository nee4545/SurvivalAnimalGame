using System.Collections;
using Terresquall;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ChargeDodgeTriggerZone : MonoBehaviour
{
    [Header("References")]
    public GameObject chargingAIPrefab;
    public Transform aiSpawnPoint;
    public SlowMotionHuntUI speedometerUI;
    public CameraSystem mainCameraSystem;

    [Header("Charge Contact Safety")]
    public bool forceMissIfAIReachesPlayer = true;
    public float aiPlayerContactDistance = 2.2f;
    public bool forceResolveIfAIPassesPlayer = true;

    [Header("Charge Impact Timing")]
    public float resolveDistanceFromPlayer = 3f;
    public bool noTapCountsAsMiss = true;
    public float maxChargeResolveTime = 5f;

    [Header("Trigger")]
    public string playerTag = "Player";
    public bool triggerOnlyOnce = true;
    public float startDelay = 0.25f;
    public bool useStartDistanceCheck = false;
    public float startDistanceFromAISpawn = 18f;

    [Header("Charge Sequence")]
    public float slowTimeScale = 0.2f;
    public float timingWindowDuration = 2f;
    public float aiChargeSpeed = 22f;
    public float aiChargePastPlayerDistance = 10f;

    [Header("Charge Camera Polish")]
    public bool lookAtChargingAIDuringUI = true;
    public float cameraReturnDistanceFromPlayer = 5f;

    [Header("Player Successful Dodge - Jump Over AI")]
    public float dodgeForwardOverAnimalDistance = 5.5f;
    public float dodgeOverAnimalHeight = 4.2f;
    public bool facePlayerTowardAIOnStart = true;
    public float playerFaceAISpeed = 12f;
    public float dodgeDuration = 0.55f;
    public int dodgeBackflipRotations = 1;

    [Header("Player Hit Reaction")]
    public int damageOnHit = 25;
    public float hitKnockbackDistance = 10f;
    public float hitKnockbackHeight = 5f;
    public float hitKnockbackDuration = 0.75f;
    public int hitBackflipRotations = 2;

    [Header("After Sequence")]
    public bool enableAINormalBehaviourAfterCharge = true;
    public float aiNormalBehaviourDelay = 0.4f;

    [Header("Mobile Controls")]
    public VirtualJoystick virtualJoystick;

    [Header("Dodge Result Popups")]
    public string successPopupText = "Super Dodge";
    public string missPopupText = "Missed!";
    public Vector3 resultPopupOffset = new Vector3(0f, 2.5f, 0f);

    [Header("Debug Gizmo")]
    public Color triggerGizmoColor = new Color(1f, 0.2f, 0f, 0.18f);
    public Color chargeLineColor = Color.red;

    private bool hasTriggered;
    private bool sequenceRunning;
    private bool tapRequested;
    private CCActor playerActor;
    private GameObject spawnedAI;
    private CuteAnimalAI spawnedCuteAI;

    private float originalTimeScale;
    private float originalFixedDeltaTime;

    private Vector3 cachedChargeDirection;
    private Vector3 cachedChargeEndPoint;
    private Vector3 cachedPlayerPositionAtChargeStart;

    private bool canReceiveTap;
    private bool isSubscribedToSpeedometer;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnlyOnce)
            return;

        CCActor actor = other.GetComponentInParent<CCActor>();

        if (!actor)
            return;

        playerActor = actor;

        if (!useStartDistanceCheck)
            StartChargeSequence();
    }

    private void OnTriggerStay(Collider other)
    {
        if (hasTriggered && triggerOnlyOnce)
            return;

        if (!useStartDistanceCheck)
            return;

        CCActor actor = other.GetComponentInParent<CCActor>();

        if (!actor)
            return;

        playerActor = actor;

        if (!aiSpawnPoint)
            return;

        float dist = Vector3.Distance(playerActor.transform.position, aiSpawnPoint.position);

        if (dist <= startDistanceFromAISpawn)
            StartChargeSequence();
    }

    public void RequestTapFromButton()
    {
        if (!sequenceRunning)
            return;

        if (!canReceiveTap)
            return;

        tapRequested = true;
    }

    private void StartChargeSequence()
    {
        if (sequenceRunning)
            return;

        if (!playerActor || !chargingAIPrefab || !aiSpawnPoint)
            return;

        hasTriggered = true;
        StartCoroutine(ChargeSequenceRoutine());
    }

    private IEnumerator ChargeSequenceRoutine()
    {
        sequenceRunning = true;
        tapRequested = false;
        canReceiveTap = false;

        yield return new WaitForSeconds(startDelay);

        SpawnChargeAI();

        if (!spawnedAI)
        {
            sequenceRunning = false;
            yield break;
        }

        CacheChargePath();

        if (facePlayerTowardAIOnStart)
            FacePlayerTowardAIInstant();

        playerActor.isInParabola = true;

        originalTimeScale = Time.timeScale;
        originalFixedDeltaTime = Time.fixedDeltaTime;

        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = originalFixedDeltaTime * slowTimeScale;

        if (mainCameraSystem)
        {
            mainCameraSystem.SetHuntZoom(true);

            if (lookAtChargingAIDuringUI && spawnedAI)
                mainCameraSystem.SetTemporaryLookTarget(spawnedAI.transform);
        }

        ShowChargeSpeedometer();

        canReceiveTap = true;

        Vector3 chargeDirection = cachedChargeDirection;
        FaceAIToDirection(chargeDirection);

        Coroutine chargeCo = StartCoroutine(MoveAICharge(chargeDirection));

        bool timingResolved = false;
        bool success = false;

        float timingTimer = timingWindowDuration;

        while (timingTimer > 0f && !timingResolved)
        {
            timingTimer -= Time.unscaledDeltaTime;

            speedometerUI?.Tick();

            if (forceMissIfAIReachesPlayer && HasAIReachedOrPassedPlayer())
            {
                success = false;
                timingResolved = true;
                canReceiveTap = false;

                HideChargeSpeedometer();
                break;
            }

            if (tapRequested)
            {
                success = speedometerUI != null && speedometerUI.IsArrowInGreenZone();
                timingResolved = true;
            }

            yield return null;
        }

        canReceiveTap = false;

        if (!timingResolved && noTapCountsAsMiss)
        {
            success = false;
            timingResolved = true;
        }

        HideChargeSpeedometer();

        // Important: once the player has tapped / timing has resolved,
        // return to normal time immediately.
        RestoreTime();

        float resolveTimer = maxChargeResolveTime;

        while (resolveTimer > 0f)
        {
            resolveTimer -= Time.unscaledDeltaTime;

            if (!spawnedAI || !playerActor)
                break;

            if (HasAIReachedOrPassedPlayer())
                break;

            float distanceToPlayer = Vector3.Distance(
                spawnedAI.transform.position,
                playerActor.transform.position
            );

            if (distanceToPlayer <= cameraReturnDistanceFromPlayer)
            {
                if (mainCameraSystem)
                {
                    mainCameraSystem.ClearTemporaryLookTarget();
                    mainCameraSystem.SetHuntZoom(false);
                }
            }

            if (distanceToPlayer <= resolveDistanceFromPlayer)
                break;

            yield return null;
        }

        RestoreTime();
        ShowResultPopup(success);

        if (mainCameraSystem)
        {
            mainCameraSystem.ClearTemporaryLookTarget();
            mainCameraSystem.SetHuntZoom(false);
        }

        if (success)
            yield return StartCoroutine(PlayerDodgeBackflip(chargeDirection));
        else
            yield return StartCoroutine(PlayerHitBackflip(chargeDirection));

        if (chargeCo != null)
            StopCoroutine(chargeCo);

        yield return StartCoroutine(MoveAIChargeToCachedEndPoint(chargeDirection));

        if (mainCameraSystem)
        {
            mainCameraSystem.ClearTemporaryLookTarget();
            mainCameraSystem.SetHuntZoom(false);
        }

        playerActor.isInParabola = false;

        yield return new WaitForSeconds(aiNormalBehaviourDelay);

        EnableNormalAI();

        HideChargeSpeedometer();
        tapRequested = false;
        canReceiveTap = false;

        sequenceRunning = false;
    }

    private void ShowResultPopup(bool success)
    {
        if (!playerActor)
            return;

        if (DamagePopupSpawner.Instance == null)
            return;

        string popupText = success ? successPopupText : missPopupText;

        DamagePopupSpawner.Instance.ShowTextPopup(
            playerActor.transform.position + resultPopupOffset,
            popupText
        );
    }

    private void SetJoystickVisible(bool visible)
    {
        if (virtualJoystick)
            virtualJoystick.SetJoystickVisibleForHunt(visible);
    }

    private void ShowChargeSpeedometer()
    {
        if (!speedometerUI)
            return;

        SetJoystickVisible(false);

        speedometerUI.Show();

        if (!isSubscribedToSpeedometer)
        {
            speedometerUI.SubscribeTap(RequestTapFromButton);
            isSubscribedToSpeedometer = true;
        }
    }

    private void HideChargeSpeedometer()
    {
        if (!speedometerUI)
            return;

        SetJoystickVisible(true);

        if (isSubscribedToSpeedometer)
        {
            speedometerUI.UnsubscribeTap(RequestTapFromButton);
            isSubscribedToSpeedometer = false;
        }

        speedometerUI.Hide();
    }

    private bool HasAIReachedOrPassedPlayer()
    {
        if (!spawnedAI || !playerActor)
            return false;

        float distanceToPlayer = Vector3.Distance(
            spawnedAI.transform.position,
            playerActor.transform.position
        );

        if (distanceToPlayer <= aiPlayerContactDistance)
            return true;

        if (forceResolveIfAIPassesPlayer)
        {
            Vector3 fromOriginalPlayerToAI =
                spawnedAI.transform.position - cachedPlayerPositionAtChargeStart;

            fromOriginalPlayerToAI.y = 0f;

            float passedAmount = Vector3.Dot(
                fromOriginalPlayerToAI,
                cachedChargeDirection
            );

            if (passedAmount > 0f)
                return true;
        }

        return false;
    }

    private void FacePlayerTowardAIInstant()
    {
        if (!playerActor || !spawnedAI)
            return;

        Vector3 dir = spawnedAI.transform.position - playerActor.transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f)
            return;

        playerActor.transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    private void SpawnChargeAI()
    {
        spawnedAI = Instantiate(
            chargingAIPrefab,
            aiSpawnPoint.position,
            aiSpawnPoint.rotation
        );

        if (spawnedAI && playerActor)
        {
            Vector3 dirToPlayer = playerActor.transform.position - spawnedAI.transform.position;
            dirToPlayer.y = 0f;

            if (dirToPlayer.sqrMagnitude > 0.01f)
                spawnedAI.transform.rotation = Quaternion.LookRotation(dirToPlayer.normalized);
        }

        spawnedCuteAI = spawnedAI.GetComponent<CuteAnimalAI>();

        if (spawnedCuteAI)
            spawnedCuteAI.enabled = false;
    }

    private void CacheChargePath()
    {
        cachedPlayerPositionAtChargeStart = playerActor.transform.position;

        cachedChargeDirection = cachedPlayerPositionAtChargeStart - aiSpawnPoint.position;
        cachedChargeDirection.y = 0f;

        if (cachedChargeDirection.sqrMagnitude < 0.01f)
            cachedChargeDirection = aiSpawnPoint.forward;

        cachedChargeDirection.Normalize();

        cachedChargeEndPoint =
            cachedPlayerPositionAtChargeStart +
            cachedChargeDirection * aiChargePastPlayerDistance;
    }

    private void FaceAIToDirection(Vector3 direction)
    {
        if (!spawnedAI)
            return;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        spawnedAI.transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    private IEnumerator MoveAICharge(Vector3 direction)
    {
        direction.y = 0f;
        direction.Normalize();

        while (sequenceRunning && spawnedAI)
        {
            spawnedAI.transform.position += direction * aiChargeSpeed * Time.unscaledDeltaTime;
            spawnedAI.transform.rotation = Quaternion.LookRotation(direction);
            yield return null;
        }
    }

    private IEnumerator MoveAIChargeToCachedEndPoint(Vector3 direction)
    {
        if (!spawnedAI)
            yield break;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            direction = spawnedAI.transform.forward;

        direction.Normalize();

        Vector3 start = spawnedAI.transform.position;
        Vector3 end = cachedChargeEndPoint;

        float distance = Vector3.Distance(start, end);
        float duration = Mathf.Max(0.1f, distance / aiChargeSpeed);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);

            spawnedAI.transform.position = Vector3.Lerp(start, end, t);
            spawnedAI.transform.rotation = Quaternion.LookRotation(direction);

            elapsed += Time.deltaTime;
            yield return null;
        }

        spawnedAI.transform.position = end;
        spawnedAI.transform.rotation = Quaternion.LookRotation(direction);
    }

    private IEnumerator PlayerDodgeBackflip(Vector3 chargeDirection)
    {
        Transform player = playerActor.transform;
        CharacterController controller = playerActor.GetComponent<CharacterController>();

        if (controller)
            controller.enabled = false;

        chargeDirection.y = 0f;

        if (chargeDirection.sqrMagnitude < 0.01f)
            chargeDirection = spawnedAI ? spawnedAI.transform.forward : player.forward;

        chargeDirection.Normalize();

        Vector3 jumpDir = -chargeDirection;
        jumpDir.y = 0f;
        jumpDir.Normalize();

        Vector3 start = player.position;
        Vector3 end = start + jumpDir * dodgeForwardOverAnimalDistance;

        Quaternion startRot = player.rotation;

        if (spawnedAI)
        {
            Vector3 faceDir = spawnedAI.transform.position - player.position;
            faceDir.y = 0f;

            if (faceDir.sqrMagnitude > 0.01f)
            {
                startRot = Quaternion.LookRotation(faceDir.normalized);
                player.rotation = startRot;
            }
        }

        float elapsed = 0f;

        while (elapsed < dodgeDuration)
        {
            float t = Mathf.Clamp01(elapsed / dodgeDuration);

            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y = Mathf.Lerp(start.y, end.y, t) + dodgeOverAnimalHeight * Mathf.Sin(Mathf.PI * t);

            player.position = pos;

            float flipAngle = 360f * dodgeBackflipRotations * t;
            player.rotation = startRot * Quaternion.Euler(-flipAngle, 0f, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        player.position = end;
        player.rotation = startRot;

        if (controller)
            controller.enabled = true;
    }

    private IEnumerator PlayerHitBackflip(Vector3 chargeDirection)
    {
        Transform player = playerActor.transform;
        CharacterController controller = playerActor.GetComponent<CharacterController>();
        Health playerHealth = playerActor.GetComponent<Health>();

        if (playerHealth)
            playerHealth.TakeDamage(damageOnHit);

        if (controller)
            controller.enabled = false;

        Vector3 dir = chargeDirection;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f)
            dir = -player.forward;

        dir.Normalize();

        Vector3 start = player.position;
        Vector3 end = start + dir * hitKnockbackDistance;

        Quaternion startRot = player.rotation;

        float elapsed = 0f;

        while (elapsed < hitKnockbackDuration)
        {
            float t = Mathf.Clamp01(elapsed / hitKnockbackDuration);

            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y = Mathf.Lerp(start.y, end.y, t) + hitKnockbackHeight * Mathf.Sin(Mathf.PI * t);

            player.position = pos;

            float flipAngle = 360f * hitBackflipRotations * t;
            player.rotation = startRot * Quaternion.Euler(-flipAngle, 0f, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        player.position = end;
        player.rotation = startRot;

        if (controller)
            controller.enabled = true;
    }

    private void EnableNormalAI()
    {
        if (!enableAINormalBehaviourAfterCharge)
            return;

        if (spawnedCuteAI)
            spawnedCuteAI.enabled = true;
    }

    private void RestoreTime()
    {
        Time.timeScale = originalTimeScale > 0f ? originalTimeScale : 1f;

        if (originalFixedDeltaTime > 0f)
            Time.fixedDeltaTime = originalFixedDeltaTime;
    }

    private void OnDisable()
    {
        HideChargeSpeedometer();

        tapRequested = false;
        canReceiveTap = false;
        sequenceRunning = false;

        RestoreTime();

        if (mainCameraSystem)
        {
            mainCameraSystem.ClearTemporaryLookTarget();
            mainCameraSystem.SetHuntZoom(false);
        }

        if (playerActor)
            playerActor.isInParabola = false;

        SetJoystickVisible(true);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();

        if (col)
        {
            Gizmos.color = triggerGizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
                Gizmos.DrawCube(box.center, box.size);
            else if (col is SphereCollider sphere)
                Gizmos.DrawSphere(sphere.center, Vector3.one.x * sphere.radius);

            Gizmos.matrix = Matrix4x4.identity;
        }

        if (aiSpawnPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(aiSpawnPoint.position, 0.6f);

            Gizmos.color = chargeLineColor;
            Vector3 previewDir = aiSpawnPoint.forward;
            Vector3 previewEnd = aiSpawnPoint.position + previewDir * aiChargePastPlayerDistance;

            Gizmos.DrawLine(aiSpawnPoint.position, previewEnd);
            Gizmos.DrawWireSphere(previewEnd, 0.6f);
        }
    }
#endif
}