using UnityEngine;

public class RiverEscapeMiniGameController : MonoBehaviour
{
    [Header("Player")]
    public Transform player;
    public RiverEscapePlayerController riverPlayerController;

    [Header("River World")]
    public RiverWorldScroller riverWorldScroller;

    [Header("River Start")]
    public Transform riverStartPoint;
    public RiverRideableObject startingRideable;

    [Header("Cameras")]
    public Camera normalCamera;
    public Camera riverCamera;
    public RiverEscapeCameraController riverCameraController;

    [Header("Disable During River Escape")]
    public GameObject[] objectsToDisableDuringRiver;
    public MonoBehaviour[] behavioursToDisableDuringRiver;

    [Header("End")]
    public float failReturnDelay = 0.75f;

    [Header("Debug Exit")]
    public bool allowEscapeKeyToExit = true;
    public KeyCode debugExitKey = KeyCode.Escape;

    [Header("Rideable Spawning")]
    public RiverRideableSpawner rideableSpawner;

    [Header("Counter Flow Rideable Spawning")]
    public RiverCounterFlowRideableSpawner counterFlowRideableSpawner;

    [Header("Debug")]
    public bool debugLogs;

    [Header("River Player Scale")]
    public bool scalePlayerDuringRiverEscape = true;

    [Tooltip("The transform to scale. Usually player root, or player visual child if you do not want collider size to change.")]
    public Transform playerScaleTarget;

    public Vector3 riverPlayerScaleMultiplier = new Vector3(1.25f, 1.25f, 1.25f);

    private Vector3 originalPlayerScale;
    private bool cachedOriginalPlayerScale;

    [Header("Respawn")]
    public bool respawnOnExistingRideable = true;
    public float respawnDelay = 0.5f;

    [Header("River Forward Recenter")]
    public bool useForwardRecenter = true;

    [Tooltip("Usually the player transform.")]
    public Transform recenterTarget;

    [Tooltip("Objects that should move back with the player. Add ActiveRideables parent, CounterFlow parent, starting rideable parent if needed.")]
    public Transform[] recenterRoots;

    [Tooltip("How far forward the player can move before we recentre.")]
    public float recenterThreshold = 8f;

    [Header("River Direction")]
    public Transform riverDirectionReference;

    private float initialTargetForward;

    private bool isRunning;
    private Vector3 originalPlayerPosition;
    private Quaternion originalPlayerRotation;

    private bool[] disabledObjectOriginalStates;
    private bool[] disabledBehaviourOriginalStates;

    public void StartRiverEscapeMiniGame()
    {
        if (isRunning)
            return;

        if (player == null)
        {
            Debug.LogWarning("[RiverEscape] Missing player.");
            return;
        }

        if (riverPlayerController == null)
        {
            Debug.LogWarning("[RiverEscape] Missing riverPlayerController.");
            return;
        }

        if (riverStartPoint == null)
        {
            Debug.LogWarning("[RiverEscape] Missing riverStartPoint.");
            return;
        }

        if (startingRideable == null)
        {
            Debug.LogWarning("[RiverEscape] Missing startingRideable.");
            return;
        }

        isRunning = true;

        CacheOriginalState();
        EnterRiverEscapeMode();

        if (debugLogs)
            Debug.Log("[RiverEscape] Started.");
    }

    public void CacheRiverForwardAnchor()
    {
        if (recenterTarget == null && riverPlayerController != null)
            recenterTarget = riverPlayerController.transform;

        initialTargetForward = GetForwardProjection(
            recenterTarget != null ? recenterTarget.position : Vector3.zero
        );
    }

    private void CacheOriginalPlayerScale()
    {
        if (cachedOriginalPlayerScale)
            return;

        if (playerScaleTarget == null && riverPlayerController != null)
            playerScaleTarget = riverPlayerController.transform;

        if (playerScaleTarget == null)
            return;

        originalPlayerScale = playerScaleTarget.localScale;
        cachedOriginalPlayerScale = true;
    }

    private void ApplyRiverPlayerScale()
    {
        if (!scalePlayerDuringRiverEscape)
            return;

        CacheOriginalPlayerScale();

        if (playerScaleTarget == null)
            return;

        playerScaleTarget.localScale = new Vector3(
            originalPlayerScale.x * riverPlayerScaleMultiplier.x,
            originalPlayerScale.y * riverPlayerScaleMultiplier.y,
            originalPlayerScale.z * riverPlayerScaleMultiplier.z
        );
    }

    private void RestoreOriginalPlayerScale()
    {
        if (!cachedOriginalPlayerScale)
            return;

        if (playerScaleTarget == null)
            return;

        playerScaleTarget.localScale = originalPlayerScale;
    }

    public void TryRecenterRiverForward()
    {
        if (!useForwardRecenter)
            return;

        if (recenterTarget == null)
            return;

        Vector3 forward = GetRiverForward();

        float currentForward =
            GetForwardProjection(recenterTarget.position);

        float forwardDelta =
            currentForward - initialTargetForward;

        if (Mathf.Abs(forwardDelta) < recenterThreshold)
            return;

        Vector3 shift = -forward * forwardDelta;

        // Move player back to anchor.
        recenterTarget.position += shift;

        // Move active rideables / dynamic objects back with player.
        if (recenterRoots != null)
        {
            for (int i = 0; i < recenterRoots.Length; i++)
            {
                if (recenterRoots[i] == null)
                    continue;

                recenterRoots[i].position += shift;
            }
        }

        // Move/sync river camera with the same shift.
        if (riverCameraController != null)
        {
            riverCameraController.ApplyExternalRecenterShift(shift);
        }
        else if (riverCamera != null)
        {
            riverCamera.transform.position += shift;
        }
    }

    private float GetForwardProjection(Vector3 position)
    {
        Vector3 forward = GetRiverForward();
        return Vector3.Dot(position, forward);
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

    public void NotifyRideableExpired()
    {
        if (!isRunning)
            return;

        if (riverWorldScroller != null)
            riverWorldScroller.StopScrolling();

        if (debugLogs)
            Debug.Log("[RiverEscape] Rideable expired. River scroller stopped.");
    }

    public void RegisterPlayerFailed()
    {
        if (!isRunning)
            return;

        CancelInvoke();

        if (respawnOnExistingRideable)
            Invoke(nameof(RespawnPlayerOnExistingRideable), respawnDelay);
        else
            Invoke(nameof(FailAndReturn), failReturnDelay);
    }

    private void RespawnPlayerOnExistingRideable()
    {
        if (!isRunning)
            return;

        RiverRideableObject respawnRideable = null;

        if (rideableSpawner != null)
        {
            respawnRideable =
                rideableSpawner.GetRandomAvailableRideableForRespawn();
        }

        if (respawnRideable == null)
        {
            FailAndReturn();
            return;
        }

        if (riverWorldScroller != null)
            riverWorldScroller.StartScrolling();

        riverPlayerController.RespawnOnRideable(respawnRideable);
    }

    private void FailAndReturn()
    {
        EndRiverEscapeMiniGame(false);
    }

    public void EndRiverEscapeMiniGame(bool success)
    {
        if (!isRunning)
            return;

        isRunning = false;

        if (riverWorldScroller != null)
            riverWorldScroller.StopScrolling();

        if (riverPlayerController != null)
            riverPlayerController.EndRiverEscape();

        RestoreOriginalPlayerScale();
        RestorePlayerToOriginalWorld();
        RestoreDisabledObjects();
        RestoreCameras();

        if (rideableSpawner != null)
            rideableSpawner.StopAndClear();

        if (counterFlowRideableSpawner != null)
            counterFlowRideableSpawner.StopAndClear();

        if (debugLogs)
            Debug.Log("[RiverEscape] Ended. Success: " + success);
    }

    private void Update()
    {
        if (!isRunning)
            return;

        if (!allowEscapeKeyToExit)
            return;

        if (Input.GetKeyDown(debugExitKey))
        {
            EndRiverEscapeMiniGame(false);
        }
    }

    private void CacheOriginalState()
    {
        originalPlayerPosition = player.position;
        originalPlayerRotation = player.rotation;

        disabledObjectOriginalStates =
            new bool[objectsToDisableDuringRiver.Length];

        for (int i = 0; i < objectsToDisableDuringRiver.Length; i++)
        {
            if (objectsToDisableDuringRiver[i] == null)
                continue;

            disabledObjectOriginalStates[i] =
                objectsToDisableDuringRiver[i].activeSelf;
        }

        disabledBehaviourOriginalStates =
            new bool[behavioursToDisableDuringRiver.Length];

        for (int i = 0; i < behavioursToDisableDuringRiver.Length; i++)
        {
            if (behavioursToDisableDuringRiver[i] == null)
                continue;

            disabledBehaviourOriginalStates[i] =
                behavioursToDisableDuringRiver[i].enabled;
        }
    }

    private void EnterRiverEscapeMode()
    {
        DisableNormalObjects();
        SwitchToRiverCamera();
        TeleportPlayerToRiverStart();
        ApplyRiverPlayerScale();
        CacheRiverForwardAnchor();

        if (riverWorldScroller != null)
            riverWorldScroller.StartScrolling();

        riverPlayerController.BeginRiverEscape(
            this,
            startingRideable
        );

        if (rideableSpawner != null)
            rideableSpawner.StartSpawning();

        if (counterFlowRideableSpawner != null)
            counterFlowRideableSpawner.StartSpawning();

    }

    private void DisableNormalObjects()
    {
        for (int i = 0; i < objectsToDisableDuringRiver.Length; i++)
        {
            if (objectsToDisableDuringRiver[i] != null)
                objectsToDisableDuringRiver[i].SetActive(false);
        }

        for (int i = 0; i < behavioursToDisableDuringRiver.Length; i++)
        {
            if (behavioursToDisableDuringRiver[i] != null)
                behavioursToDisableDuringRiver[i].enabled = false;
        }
    }

    private void SwitchToRiverCamera()
    {
        if (normalCamera != null)
            normalCamera.gameObject.SetActive(false);

        if (riverCamera != null)
            riverCamera.gameObject.SetActive(true);
    }

    private void RestoreCameras()
    {
        if (riverCamera != null)
            riverCamera.gameObject.SetActive(false);

        if (normalCamera != null)
            normalCamera.gameObject.SetActive(true);
    }

    private void TeleportPlayerToRiverStart()
    {
        CharacterController characterController =
            player.GetComponent<CharacterController>();

        bool wasEnabled =
            characterController != null &&
            characterController.enabled;

        if (characterController != null)
            characterController.enabled = false;

        player.SetPositionAndRotation(
            riverStartPoint.position,
            riverStartPoint.rotation
        );

        Physics.SyncTransforms();

        if (characterController != null)
            characterController.enabled = wasEnabled;
    }

    private void RestorePlayerToOriginalWorld()
    {
        CharacterController characterController =
            player.GetComponent<CharacterController>();

        bool wasEnabled =
            characterController != null &&
            characterController.enabled;

        if (characterController != null)
            characterController.enabled = false;

        player.SetPositionAndRotation(
            originalPlayerPosition,
            originalPlayerRotation
        );

        Physics.SyncTransforms();

        if (characterController != null)
            characterController.enabled = wasEnabled;
    }

    private void RestoreDisabledObjects()
    {
        for (int i = 0; i < objectsToDisableDuringRiver.Length; i++)
        {
            if (objectsToDisableDuringRiver[i] != null)
                objectsToDisableDuringRiver[i].SetActive(
                    disabledObjectOriginalStates[i]
                );
        }

        for (int i = 0; i < behavioursToDisableDuringRiver.Length; i++)
        {
            if (behavioursToDisableDuringRiver[i] != null)
                behavioursToDisableDuringRiver[i].enabled =
                    disabledBehaviourOriginalStates[i];
        }
    }
}