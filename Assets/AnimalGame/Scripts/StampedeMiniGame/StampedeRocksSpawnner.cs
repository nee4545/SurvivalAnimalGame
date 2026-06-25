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

    public void StopSpawningAndClear()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        for (int i = activeHazards.Count - 1; i >= 0; i--)
        {
            if (activeHazards[i] != null)
                Destroy(activeHazards[i]);
        }

        activeHazards.Clear();

        if (debugLogs)
            Debug.Log("[StampedeHazardSpawner] Stopped and cleared.");
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
            spawnDirection * spawnDistanceFromPlayer;

        spawnPosition.y += rockYOffset;

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
}
