using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StampedeAnimalClusterSpawner : MonoBehaviour
{
    [Header("Cluster Animal Prefabs")]
    public StampedeClusterAnimal[] clusterAnimalPrefabs;

    [Header("References")]
    public StampedeLaneController laneController;
    public StampedeWorldScroller worldScroller;

    [Header("Spawn Timing")]
    public float startDelay = 1f;
    public float minSpawnInterval = 1.8f;
    public float maxSpawnInterval = 3.2f;

    [Header("Cluster Shape")]
    public int minAnimalsPerCluster = 3;
    public int maxAnimalsPerCluster = 6;
    public float clusterWidth = 2.2f;
    public float clusterDepth = 2.8f;

    [Header("Spawn Position")]
    public float spawnDistanceFromPlayer = 30f;
    public float spawnHeightOffset = 0f;

    [Header("Lane Selection")]
    public bool allowMultiLaneCluster = false;
    public int maxLanesPerCluster = 1;

    [Header("Movement Fallback")]
    public float fallbackMoveSpeed = 15f;
    public bool movesTowardPlayer = true;

    [Header("Direction Specific Spawn Distance")]
    public bool useDirectionSpecificSpawnDistance = true;
    public float normalSpawnDistanceFromPlayer = 28f;
    public float invertedSpawnDistanceFromPlayer = 45f;

    [Header("Prop Spawn Reservation")]
    public bool usePropSpawnReservation = true;
    public float propReservationForwardSeparation = 10f;
    public float propReservationLifetime = 1.5f;
    public float propReservationSideSeparation = 3.5f;

    [Header("Camera")]
    public Camera splashCameraOverride;

    [Header("Cleanup")]
    public bool clearOnStop = true;

    [Header("Debug")]
    public bool debugLogs;

    private StampedeMiniGameController controller;
    private Transform player;
    private Coroutine spawnRoutine;
    private bool isSpawning;

    private readonly List<GameObject> spawnedClusters = new();

    public void BeginSpawning(
        StampedeMiniGameController miniGameController,
        StampedeLaneController playerLaneController
    )
    {

        if (!enabled)
            return;

        controller = miniGameController;
        laneController = playerLaneController;

        if (controller != null && controller.playerActor != null)
            player = controller.playerActor.transform;

        if (laneController == null)
        {
            Debug.LogWarning("[StampedeAnimalClusterSpawner] Missing laneController.");
            return;
        }

        if (player == null)
        {
            Debug.LogWarning("[StampedeAnimalClusterSpawner] Missing player.");
            return;
        }

        if (clusterAnimalPrefabs == null || clusterAnimalPrefabs.Length == 0)
        {
            Debug.LogWarning("[StampedeAnimalClusterSpawner] No cluster animal prefabs assigned.");
            return;
        }

        isSpawning = true;

        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        spawnRoutine = StartCoroutine(SpawnRoutine());

        if (debugLogs)
            Debug.Log("[StampedeAnimalClusterSpawner] Started.");
    }

    private float GetCurrentSpawnDistance()
    {
        if (!useDirectionSpecificSpawnDistance)
            return spawnDistanceFromPlayer;

        if (laneController != null && laneController.faceAwayFromStampede)
            return invertedSpawnDistanceFromPlayer;

        return normalSpawnDistanceFromPlayer;
    }

    private Camera GetSplashCamera()
    {
        if (splashCameraOverride != null)
            return splashCameraOverride;

        if (controller != null && controller.stampedeCamera != null)
            return controller.stampedeCamera;

        if (laneController != null && laneController.stampedeCamera != null)
            return laneController.stampedeCamera;

        return Camera.main;
    }

    public void StopSpawningOnly()
    {
        isSpawning = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        if (debugLogs)
            Debug.Log("[StampedeAnimalClusterSpawner] Stopped spawning only.");
    }

    public void ClearSpawnedClusters()
    {
        for (int i = spawnedClusters.Count - 1; i >= 0; i--)
        {
            if (spawnedClusters[i] != null)
                Destroy(spawnedClusters[i]);
        }

        spawnedClusters.Clear();

        if (debugLogs)
            Debug.Log("[StampedeAnimalClusterSpawner] Cleared clusters.");
    }

    public void StopSpawningAndClear()
    {
        StopSpawningOnly();

        if (clearOnStop)
            ClearSpawnedClusters();
    }

    private IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(startDelay);

        while (isSpawning)
        {
            SpawnCluster();

            float wait = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(wait);
        }
    }

    private void SpawnCluster()
    {
        int laneCount = Mathf.Max(1, laneController.laneCount);

        int lanesToUse = allowMultiLaneCluster
            ? Mathf.Clamp(maxLanesPerCluster, 1, laneCount)
            : 1;

        List<int> lanes = PickLanes(laneCount, lanesToUse);

        GameObject root = new GameObject("Stampede Animal Cluster");
        spawnedClusters.Add(root);

        for (int i = 0; i < lanes.Count; i++)
        {
            SpawnAnimalsForLane(root.transform, lanes[i]);
        }
    }

    public void ApplyRunConfig(StampedeRunConfig config)
    {
        if (config == null)
            return;

        enabled = config.enableAnimalClusters;

        clusterAnimalPrefabs = config.clusterAnimalPrefabs;

        minSpawnInterval = config.clusterSpawnIntervalMin;
        maxSpawnInterval = config.clusterSpawnIntervalMax;

        useDirectionSpecificSpawnDistance = true;
        normalSpawnDistanceFromPlayer = config.clusterNormalSpawnDistanceFromPlayer;
        invertedSpawnDistanceFromPlayer = config.clusterInvertedSpawnDistanceFromPlayer;

        minAnimalsPerCluster = config.minAnimalsPerCluster;
        maxAnimalsPerCluster = config.maxAnimalsPerCluster;

        clusterWidth = config.clusterWidth;
        clusterDepth = config.clusterDepth;

        allowMultiLaneCluster = config.allowMultiLaneCluster;
        maxLanesPerCluster = config.maxLanesPerCluster;

        StopSpawningAndClear();
    }

    private void SpawnAnimalsForLane(Transform root, int lane)
    {
        int count = Random.Range(minAnimalsPerCluster, maxAnimalsPerCluster + 1);

        Vector3 lanePosition = laneController.GetLaneWorldPosition(lane);

        Vector3 forward = laneController.GetForwardDirection();
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        // Get the real world-scroller movement direction.
        // This already accounts for inverted / non-inverted mode.
        Vector3 moveDirection = GetMoveDirection();
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude < 0.001f)
            moveDirection = -forward;

        moveDirection.Normalize();

        float moveSpeed = GetMoveSpeed();

        // Spawn opposite the movement direction.
        // If objects move toward the player/camera, they must spawn ahead of that movement path.
        Vector3 spawnDirection = -moveDirection;

        Vector3 clusterCenter =
     lanePosition +
     spawnDirection * GetCurrentSpawnDistance();

        clusterCenter.y += spawnHeightOffset;

        if (usePropSpawnReservation)
        {
            Vector3 forwardForReservation = laneController.GetForwardDirection();
            forwardForReservation.y = 0f;

            if (forwardForReservation.sqrMagnitude < 0.001f)
                forwardForReservation = Vector3.forward;

            forwardForReservation.Normalize();

            Vector3 rightForReservation = Vector3.Cross(Vector3.up, forwardForReservation);
            rightForReservation.y = 0f;

            if (rightForReservation.sqrMagnitude < 0.001f)
                rightForReservation = Vector3.right;

            rightForReservation.Normalize();

            float forwardClearance =
                propReservationForwardSeparation + clusterDepth * 0.5f;

            float sideClearance =
                propReservationSideSeparation + clusterWidth * 0.5f;

            bool reserved = StampedePropSpawnReservation.TryReserve(
                clusterCenter,
                forwardForReservation,
                rightForReservation,
                forwardClearance,
                sideClearance,
                propReservationLifetime
            );

            if (!reserved)
                return;
        }


        for (int i = 0; i < count; i++)
        {
            StampedeClusterAnimal prefab =
                clusterAnimalPrefabs[Random.Range(0, clusterAnimalPrefabs.Length)];

            if (prefab == null)
                continue;

            Vector3 localOffset =
                right * Random.Range(-clusterWidth * 0.5f, clusterWidth * 0.5f) +
                forward * Random.Range(-clusterDepth * 0.5f, clusterDepth * 0.5f);

            Vector3 spawnPosition = clusterCenter + localOffset;

            Quaternion rotation = Quaternion.LookRotation(
            -moveDirection.normalized,
            Vector3.up
            );

            StampedeClusterAnimal animal = Instantiate(
                prefab,
                spawnPosition,
                rotation,
                root
            );

            animal.Init(
            player,
            laneController,
            moveDirection,
            moveSpeed,
            GetSplashCamera()
            );
        }

        if (debugLogs)
            Debug.Log("[StampedeAnimalClusterSpawner] Spawned cluster in lane: " + lane);
    }

    private List<int> PickLanes(int laneCount, int amount)
    {
        List<int> all = new List<int>();

        for (int i = 0; i < laneCount; i++)
            all.Add(i);

        List<int> picked = new List<int>();

        for (int i = 0; i < amount && all.Count > 0; i++)
        {
            int index = Random.Range(0, all.Count);
            picked.Add(all[index]);
            all.RemoveAt(index);
        }

        return picked;
    }

    private Vector3 GetMoveDirection()
    {
        if (worldScroller != null)
            return worldScroller.GetCurrentScrollMoveDirection();

        Vector3 forward = laneController.GetForwardDirection();
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        forward.Normalize();

        return movesTowardPlayer ? -forward : forward;
    }

    private float GetMoveSpeed()
    {
        if (worldScroller != null)
            return worldScroller.GetCurrentScrollSpeed();

        return fallbackMoveSpeed;
    }
}