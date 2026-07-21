using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StampedeRocksSpawnner : MonoBehaviour
{
    [Header("Rock Hazard")]
    public GameObject rockHazardPrefab;

    [Header("References")]
    public StampedeWorldScroller worldScroller;

    [Header("Spawn Timing")]
    public float spawnIntervalMin = 1.2f;
    public float spawnIntervalMax = 2.2f;

    [Header("Spawn Position")]
    public float spawnDistanceFromPlayer = 35f;
    public float rockYOffset = 0f;

    [Header("Lane Pattern")]
    public bool allowSameLaneTwice = false;

    [Range(0f, 1f)]
    public float doubleRockChance = 0.15f;

    [Header("Direction Variation")]
    public bool hazardMovesTowardPlayer = true;

    [Header("Direction Specific Spawn Distance")]
    public bool useDirectionSpecificSpawnDistance = true;
    public float normalSpawnDistanceFromPlayer = 32f;
    public float invertedSpawnDistanceFromPlayer = 45f;

    [Header("Prop Spawn Reservation")]
    public bool usePropSpawnReservation = true;
    public float propReservationForwardSeparation = 10f;
    public float propReservationSideSeparation = 3f;
    public float propReservationLifetime = 1.5f;

    [Header("Debug")]
    public bool debugLogs;

    private StampedeMiniGameController controller;
    private StampedeLaneController laneController;
    private Transform player;

    private readonly List<GameObject> activeHazards = new();
    private Coroutine spawnRoutine;
    private int lastLane = -1;

    public void BeginSpawning(
        StampedeMiniGameController miniGameController,
        StampedeLaneController lane
    )
    {
        if (!enabled)
            return;

        controller = miniGameController;
        laneController = lane;

        if (controller != null && controller.playerActor != null)
            player = controller.playerActor.transform;

        if (worldScroller == null && controller != null)
            worldScroller = controller.worldScroller;

        StopSpawningAndClear();

        spawnRoutine = StartCoroutine(SpawnRoutine());

        if (debugLogs)
            Debug.Log("[StampedeHazardSpawner] Started.");
    }

    private float GetCurrentSpawnDistance()
    {
        if (!useDirectionSpecificSpawnDistance)
            return spawnDistanceFromPlayer;

        if (laneController != null && laneController.faceAwayFromStampede)
            return invertedSpawnDistanceFromPlayer;

        return normalSpawnDistanceFromPlayer;
    }

    public void StopSpawningOnly()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        if (debugLogs)
            Debug.Log("[StampedeRocksSpawnner] Stopped spawning only.");
    }

    public void ClearSpawnedRocks()
    {
        for (int i = activeHazards.Count - 1; i >= 0; i--)
        {
            if (activeHazards[i] != null)
                Destroy(activeHazards[i]);
        }

        activeHazards.Clear();

        if (debugLogs)
            Debug.Log("[StampedeRocksSpawnner] Cleared rocks.");
    }

    public void StopSpawningAndClear()
    {
        StopSpawningOnly();
        ClearSpawnedRocks();
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            float wait = Random.Range(spawnIntervalMin, spawnIntervalMax);
            yield return new WaitForSeconds(wait);

            SpawnRockOnRandomLane();

            if (Random.value <= doubleRockChance)
                SpawnRockOnRandomLane();
        }
    }

    public void ApplyRunConfig(StampedeRunConfig config)
    {
        if (config == null)
            return;

        enabled = config.enableRockHazards;

        rockHazardPrefab = config.rockHazardPrefab;

        spawnIntervalMin = config.rockSpawnIntervalMin;
        spawnIntervalMax = config.rockSpawnIntervalMax;

        useDirectionSpecificSpawnDistance = true;
        normalSpawnDistanceFromPlayer = config.rockNormalSpawnDistanceFromPlayer;
        invertedSpawnDistanceFromPlayer = config.rockInvertedSpawnDistanceFromPlayer;

        doubleRockChance = config.doubleRockChance;

        StopSpawningAndClear();
    }

    private void SpawnRockOnRandomLane()
    {
        if (rockHazardPrefab == null)
            return;

        if (controller == null || !controller.IsRunning)
            return;

        if (laneController == null || player == null)
            return;

        int lane = GetRandomLane();

        Vector3 moveDirection = GetRockMoveDirection();
        Vector3 spawnDirection = -moveDirection;

        Vector3 lanePosition = laneController.GetLaneWorldPosition(lane);

        Vector3 spawnPosition =
    lanePosition +
    spawnDirection * GetCurrentSpawnDistance();

        spawnPosition.y += rockYOffset;

        if (usePropSpawnReservation)
        {
            Vector3 forward = laneController.GetForwardDirection();
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;

            forward.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, forward);
            right.y = 0f;

            if (right.sqrMagnitude < 0.001f)
                right = Vector3.right;

            right.Normalize();

            bool reserved = StampedePropSpawnReservation.TryReserve(
                spawnPosition,
                forward,
                right,
                propReservationForwardSeparation,
                propReservationSideSeparation,
                propReservationLifetime
            );

            if (!reserved)
                return;
        }

        GameObject rock = Instantiate(
            rockHazardPrefab,
            spawnPosition,
            Quaternion.identity
        );

        StampedeRockHazard rockHazard =
            rock.GetComponent<StampedeRockHazard>();

        if (rockHazard != null)
        {
            rockHazard.Init(
                controller,
                player,
                moveDirection,
                GetRockMoveSpeed()
            );
        }

        StampedePropSpawnReservation.RegisterActiveProp(rock.transform);

        activeHazards.Add(rock);

        if (debugLogs)
            Debug.Log("[StampedeHazardSpawner] Spawned rock on lane: " + lane);
    }

    private int GetRandomLane()
    {
        int laneCount = Mathf.Max(1, laneController.laneCount);

        if (laneCount <= 1)
            return 0;

        int lane = Random.Range(0, laneCount);

        if (!allowSameLaneTwice)
        {
            int safety = 0;

            while (lane == lastLane && safety < 10)
            {
                lane = Random.Range(0, laneCount);
                safety++;
            }
        }

        lastLane = lane;
        return lane;
    }

    private Vector3 GetRockMoveDirection()
    {
        if (worldScroller != null)
            return worldScroller.GetCurrentScrollMoveDirection();

        Vector3 forward = laneController.GetForwardDirection();
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        return hazardMovesTowardPlayer ? -forward.normalized : forward.normalized;
    }

    private float GetRockMoveSpeed()
    {
        if (worldScroller != null)
            return worldScroller.GetCurrentScrollSpeed();

        return 15f;
    }

    private void OnDestroy()
    {
        StampedePropSpawnReservation.UnregisterActiveProp(transform);
        transform.DOKill();
    }
}
