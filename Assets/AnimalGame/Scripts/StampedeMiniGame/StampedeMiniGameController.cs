using System.Collections;
using UnityEngine;

public class StampedeMiniGameController : MonoBehaviour
{
    public static StampedeMiniGameController Instance;

    [Header("Player")]
    public CCActor playerActor;
    public StampedeLaneController laneController;

    [Header("Arena")]
    public Transform stampedeStartPoint;

    [Header("Camera")]
    public Camera mainCamera;
    public Camera stampedeCamera;
    public bool switchCameraForStampede = true;

    [Header("Mini Game")]
    public float miniGameDuration = 30f;
    public bool returnPlayerToOriginalPosition = true;

    [Header("Lives")]
    public int maxLives = 3;
    public float hitCooldown = 1f;

    [Header("Spawner")]
    public StampedeHazardSpawner hazardSpawner;
    public StampedeRocksSpawnner rocksSpawnner;

    [Header("Stampede Direction Variation")]
    public bool alternateStampedeDirection = true;

    [Tooltip("If true, the first stampede run uses inverted mode. Next run will use normal mode.")]
    public bool nextRunUsesInvertedMode = true;

    public bool currentRunUsesInvertedMode;

    [Header("Fail Timing")]
    public float failAfterLastHitDelay = 0.85f;

    private bool isEnding;
    private Coroutine delayedEndRoutine;

    private int currentLives;
    private float lastHitTime = -999f;

    [Header("Rewards")]
    public int successXPReward = 100;

    [Header("Objects To Disable During Stampede")]
    public GameObject[] objectsToDisableDuringStampede;

    [Header("Rock Hit Reaction")]
    public float rockHitWorldSlowMultiplier = 0.45f;
    public float rockHitSlowInDuration = 0.08f;
    public float rockHitSlowHoldDuration = 0.12f;
    public float rockHitSlowRecoverDuration = 0.65f;

    [Header("Stampede UI")]
    public StampedeModeUI stampedeModeUI;

    [Header("World Scroller")]
    public StampedeWorldScroller worldScroller;

    private bool[] objectsOriginalActiveStates;

    [Header("Debug")]
    public bool debugLogs = true;

    private bool isRunning;
    private Coroutine routine;
    private float remainingTime;

    private Vector3 originalPlayerPosition;
    private Quaternion originalPlayerRotation;

    private bool hasCachedOriginalState;
    private bool playerWasMovedToStampede;
    private bool rewardGivenForThisRun;

    public bool IsRunning => isRunning;

    public int CurrentLives => currentLives;
    public float RemainingTime => remainingTime;

    private void Awake()
    {
        Instance = this;
    }

    public void StartStampedeMiniGame()
    {
        if (isRunning)
            return;

        if (playerActor == null)
        {
            Debug.LogWarning("[Stampede] Missing playerActor.");
            return;
        }

        if (laneController == null)
        {
            Debug.LogWarning("[Stampede] Missing laneController.");
            return;
        }

        if (stampedeStartPoint == null)
        {
            Debug.LogWarning("[Stampede] Missing stampedeStartPoint.");
            return;
        }

        routine = StartCoroutine(RunStampedeRoutine());
    }


    private void PrepareStampedeDirectionVariation()
    {
        currentRunUsesInvertedMode = nextRunUsesInvertedMode;

        ApplyStampedeDirectionMode(currentRunUsesInvertedMode);

        if (alternateStampedeDirection)
            nextRunUsesInvertedMode = !nextRunUsesInvertedMode;
    }

    private void ApplyStampedeDirectionMode(bool invertedMode)
    {
        if (laneController != null)
            laneController.faceAwayFromStampede = invertedMode;

        if (hazardSpawner != null)
            hazardSpawner.hazardsMoveTowardPlayer = invertedMode;

        if (worldScroller != null)
            worldScroller.invertScrollDirection = invertedMode;

        if (debugLogs)
        {
            Debug.Log(
                invertedMode
                    ? "[Stampede] Direction Mode: INVERTED"
                    : "[Stampede] Direction Mode: NORMAL"
            );
        }
    }

    private IEnumerator RunStampedeRoutine()
    {
        isRunning = true;

        isEnding = false;
        rewardGivenForThisRun = false;
        lastHitTime = -999f;

        CacheOriginalState();

        PrepareStampedeDirectionVariation();

        EnterStampedeMode();

        currentLives = maxLives;
        remainingTime = miniGameDuration;

        if (stampedeModeUI != null)
        {
            stampedeModeUI.Show(miniGameDuration);
            stampedeModeUI.SetLives(currentLives);
        }

        if (hazardSpawner != null)
            hazardSpawner.BeginSpawning(this, laneController);

        if (rocksSpawnner != null)
            rocksSpawnner.BeginSpawning(this, laneController);

        remainingTime = miniGameDuration;

        while (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;

            if (stampedeModeUI != null)
                stampedeModeUI.UpdateTimer(remainingTime);

            yield return null;
        }

        BeginDelayedEnd(currentLives > 0);
    }

    private void CacheOriginalState()
    {
        originalPlayerPosition = playerActor.transform.position;
        originalPlayerRotation = playerActor.transform.rotation;
        hasCachedOriginalState = true;
    }

    private void EnterStampedeMode()
    {
        if (debugLogs)
            Debug.Log("[Stampede] Entered stampede mode.");

        DisableStampedeBlockedObjects();

        // Move player to arena start.
        playerActor.transform.SetPositionAndRotation(
            stampedeStartPoint.position,
            stampedeStartPoint.rotation
        );
        playerWasMovedToStampede = true;

        // This is safer than disabling CCActor completely.
        // Your CCActor already returns early when this is true.
        playerActor.isSlowMotionHuntActive = true;
        playerActor.isInParabola = true;

        // Stop active attack state visuals if needed.
        playerActor.currentTarget = null;
        playerActor.isAttackingLoop = false;

        laneController.Begin(playerActor.transform, stampedeStartPoint);

        if (worldScroller != null)
            worldScroller.Begin();

        if (switchCameraForStampede)
        {
            if (mainCamera != null)
                mainCamera.gameObject.SetActive(false);

            if (stampedeCamera != null)
                stampedeCamera.gameObject.SetActive(true);
        }
    }

    public void RegisterPlayerHit(Vector3 hitDirection)
    {
        if (!TryConsumeStampedeLife())
            return;

        if (laneController != null)
            laneController.PlayHitReaction(hitDirection);

        if (currentLives <= 0)
            BeginDelayedEnd(false);
    }

    public void RegisterRockHazardHit(Vector3 hitDirection)
    {
        if (!TryConsumeStampedeLife())
            return;

        if (laneController != null)
            laneController.PlayRockStumbleReaction(hitDirection);

        if (currentLives <= 0)
            BeginDelayedEnd(false);
    }

    private bool TryConsumeStampedeLife()
    {
        if (!isRunning || isEnding)
            return false;

        if (Time.time < lastHitTime + hitCooldown)
            return false;

        lastHitTime = Time.time;

        currentLives--;

        if (stampedeModeUI != null)
            stampedeModeUI.SetLives(currentLives);

        if (debugLogs)
            Debug.Log("[Stampede] Player hit. Lives left: " + currentLives);

        return true;
    }

    private void BeginDelayedEnd(bool success)
    {
        if (isEnding)
            return;

        isEnding = true;

        // Stop new hazards immediately, but let the player hit reaction finish.
        if (hazardSpawner != null)
            hazardSpawner.StopSpawningAndClear();

        if (rocksSpawnner != null)
            rocksSpawnner.StopSpawningAndClear();

        if (delayedEndRoutine != null)
            StopCoroutine(delayedEndRoutine);

        delayedEndRoutine = StartCoroutine(DelayedEndRoutine(success));
    }

    private IEnumerator DelayedEndRoutine(bool success)
    {
        yield return new WaitForSecondsRealtime(failAfterLastHitDelay);

        delayedEndRoutine = null;
        EndStampedeMiniGame(success);
    }

    public void EndStampedeMiniGame(bool success)
    {
        // Do not rely only on isRunning. If a previous cleanup partly failed,
        // the player may still be in stampede mode even when the state flags are messy.
        if (!isRunning && !isEnding && !playerWasMovedToStampede)
            return;

        bool shouldGiveReward = success && !rewardGivenForThisRun;

        if (delayedEndRoutine != null)
        {
            StopCoroutine(delayedEndRoutine);
            delayedEndRoutine = null;
        }

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        // Freeze the stampede systems first so nothing writes to the player after restore.
        SafeStopStampedeSystems();

        // Most important part: always restore player control + position, even if
        // one of the optional cleanup systems above/below has a problem.
        RestorePlayerAfterStampede();

        RestoreCamerasAfterStampede();
        RestoreStampedeBlockedObjects();

        if (stampedeModeUI != null)
            stampedeModeUI.Hide();

        isRunning = false;
        isEnding = false;
        playerWasMovedToStampede = false;
        hasCachedOriginalState = false;

        if (shouldGiveReward)
        {
            rewardGivenForThisRun = true;
            GiveRewards();
        }

        if (debugLogs)
            Debug.Log(success ? "[Stampede] Completed." : "[Stampede] Failed.");
    }

    private void SafeStopStampedeSystems()
    {
        if (hazardSpawner != null)
        {
            try
            {
                hazardSpawner.StopSpawningAndClear();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e, hazardSpawner);
            }
        }

        if (laneController != null)
        {
            try
            {
                laneController.End();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e, laneController);
            }
        }

        if (worldScroller != null)
        {
            try
            {
                worldScroller.Stop();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e, worldScroller);
            }
        }
    }

    private void RestorePlayerAfterStampede()
    {
        if (playerActor == null)
            return;

        CharacterController controller = playerActor.GetComponent<CharacterController>();
        bool controllerWasEnabled = controller != null && controller.enabled;

        if (controller != null)
            controller.enabled = false;

        try
        {
            if (returnPlayerToOriginalPosition && hasCachedOriginalState)
            {
                playerActor.transform.SetPositionAndRotation(
                    originalPlayerPosition,
                    originalPlayerRotation
                );
            }

            playerActor.isSlowMotionHuntActive = false;
            playerActor.isInParabola = false;
            playerActor.currentTarget = null;
            playerActor.isAttackingLoop = false;
            playerActor.inputVec = Vector2.zero;
            playerActor.moveDirection = Vector3.zero;
        }
        finally
        {
            if (controller != null)
                controller.enabled = controllerWasEnabled;
        }

        Physics.SyncTransforms();
    }

    private void RestoreCamerasAfterStampede()
    {
        if (!switchCameraForStampede)
            return;

        if (stampedeCamera != null)
            stampedeCamera.gameObject.SetActive(false);

        if (mainCamera != null)
            mainCamera.gameObject.SetActive(true);
    }

    private void DisableStampedeBlockedObjects()
    {
        if (objectsToDisableDuringStampede == null)
            return;

        objectsOriginalActiveStates = new bool[objectsToDisableDuringStampede.Length];

        for (int i = 0; i < objectsToDisableDuringStampede.Length; i++)
        {
            GameObject obj = objectsToDisableDuringStampede[i];

            if (obj == null)
                continue;

            objectsOriginalActiveStates[i] = obj.activeSelf;
            obj.SetActive(false);
        }
    }

    private void RestoreStampedeBlockedObjects()
    {
        if (objectsToDisableDuringStampede == null || objectsOriginalActiveStates == null)
            return;

        for (int i = 0; i < objectsToDisableDuringStampede.Length; i++)
        {
            GameObject obj = objectsToDisableDuringStampede[i];

            if (obj == null)
                continue;

            if (i >= objectsOriginalActiveStates.Length)
                continue;

            obj.SetActive(objectsOriginalActiveStates[i]);
        }
    }

    private void GiveRewards()
    {
        if (playerActor != null)
        {
            playerActor.AddXP(successXPReward);

            // Coin reward can be connected after we confirm your final coin method name.
            // Example later:
            // playerActor.AddCoins(successCoinReward);
        }
    }
}