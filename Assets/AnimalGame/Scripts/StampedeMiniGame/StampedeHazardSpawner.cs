using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StampedeHazardSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnPhase
    {
        public string phaseName = "Phase";

        [Header("Spawn Rate")]
        public float minSpawnInterval = 0.9f;
        public float maxSpawnInterval = 1.6f;

        [Header("Movement Speed")]
        public float minMoveSpeed = 8f;
        public float maxMoveSpeed = 12f;

        [Header("Amount Per Wave")]
        public int minHazardsPerSpawn = 1;
        public int maxHazardsPerSpawn = 1;

        public SpawnPhase() { }

        public SpawnPhase(
            string name,
            float minInterval,
            float maxInterval,
            float minSpeed,
            float maxSpeed,
            int minCount,
            int maxCount
        )
        {
            phaseName = name;
            minSpawnInterval = minInterval;
            maxSpawnInterval = maxInterval;
            minMoveSpeed = minSpeed;
            maxMoveSpeed = maxSpeed;
            minHazardsPerSpawn = minCount;
            maxHazardsPerSpawn = maxCount;
        }
    }

    [Header("Hazard Prefabs")]
    public StampedeHazardAI[] hazardPrefabs;

    [Header("Lane Source")]
    public StampedeLaneController laneController;

    [Header("Spawn Position")]
    public float spawnDistanceFromLanes = 18f;
    public float spawnHeightOffset = 0f;

    [Header("Fallback Spawn Timing")]
    public float startDelay = 1f;
    public float minSpawnInterval = 0.9f;
    public float maxSpawnInterval = 1.6f;

    [Header("Fallback Hazard Movement")]
    public float minMoveSpeed = 8f;
    public float maxMoveSpeed = 12f;
    public bool hazardsMoveTowardPlayer = true;

    [Header("Phased Spawn Tuning")]
    public bool usePhasedSpawnTuning = true;

    [Tooltip("0% to 30% of the stampede duration.")]
    public SpawnPhase openingPhase = new SpawnPhase(
        "Opening 30%",
        1.2f,
        1.8f,
        7f,
        9f,
        1,
        1
    );

    [Tooltip("30% to 70% of the stampede duration.")]
    public SpawnPhase middlePhase = new SpawnPhase(
        "Middle 40%",
        0.85f,
        1.35f,
        9f,
        12f,
        1,
        2
    );

    [Tooltip("70% to 100% of the stampede duration.")]
    public SpawnPhase finalePhase = new SpawnPhase(
        "Final 30%",
        0.55f,
        1.0f,
        12f,
        16f,
        2,
        2
    );

    [Header("Lane Selection")]
    public bool allowSameLaneBackToBack = false;

    [Header("Pooling")]
    public int prewarmCount = 12;

    [Header("Debug")]
    public bool debugLogs;

    private readonly List<StampedeHazardAI> spawned = new List<StampedeHazardAI>();
    private readonly Queue<StampedeHazardAI> pool = new Queue<StampedeHazardAI>();

    private StampedeMiniGameController controller;

    private Coroutine spawnRoutine;
    private bool isSpawning;

    private int lastSpawnedLane = -1;
    private float spawnStartTime;

    public void BeginSpawning(
        StampedeMiniGameController miniGameController,
        StampedeLaneController playerLaneController
    )
    {
        controller = miniGameController;

        if (laneController == null)
            laneController = playerLaneController;

        if (laneController == null)
        {
            Debug.LogWarning("[StampedeHazardSpawner] Missing laneController.");
            return;
        }

        if (hazardPrefabs == null || hazardPrefabs.Length == 0)
        {
            Debug.LogWarning("[StampedeHazardSpawner] No hazard prefabs assigned.");
            return;
        }

        PrewarmPool();

        isSpawning = true;
        lastSpawnedLane = -1;
        spawnStartTime = Time.time;

        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        spawnRoutine = StartCoroutine(SpawnRoutine());

        if (debugLogs)
            Debug.Log("[StampedeHazardSpawner] Started.");
    }

    public void StopSpawningAndClear()
    {
        isSpawning = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            if (spawned[i] != null)
                spawned[i].gameObject.SetActive(false);
        }

        spawned.Clear();

        if (debugLogs)
            Debug.Log("[StampedeHazardSpawner] Stopped.");
    }

    private IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(startDelay);

        while (isSpawning)
        {
            SpawnPhase phase = GetCurrentPhase();
            SpawnHazardWave(phase);

            float wait = GetCurrentSpawnWait(phase);
            yield return new WaitForSeconds(wait);
        }
    }

    private SpawnPhase GetCurrentPhase()
    {
        if (!usePhasedSpawnTuning)
            return null;

        if (controller == null || controller.miniGameDuration <= 0f)
            return null;

        float elapsed = Time.time - spawnStartTime;
        float progress = Mathf.Clamp01(elapsed / controller.miniGameDuration);

        if (progress < 0.30f)
            return openingPhase;

        if (progress < 0.70f)
            return middlePhase;

        return finalePhase;
    }

    private float GetCurrentSpawnWait(SpawnPhase phase)
    {
        if (phase == null)
            return RandomRangeSafe(minSpawnInterval, maxSpawnInterval);

        return RandomRangeSafe(phase.minSpawnInterval, phase.maxSpawnInterval);
    }

    private void SpawnHazardWave(SpawnPhase phase)
    {
        if (laneController == null)
            return;

        int minCount = phase != null ? phase.minHazardsPerSpawn : 1;
        int maxCount = phase != null ? phase.maxHazardsPerSpawn : 1;

        minCount = Mathf.Max(1, minCount);
        maxCount = Mathf.Max(minCount, maxCount);

        int hazardCount = Random.Range(minCount, maxCount + 1);

        if (!allowSameLaneBackToBack)
            hazardCount = Mathf.Min(hazardCount, Mathf.Max(1, laneController.laneCount));

        List<int> usedLanesThisWave = new List<int>();

        for (int i = 0; i < hazardCount; i++)
        {
            int lane = GetRandomLane(usedLanesThisWave);
            SpawnHazard(phase, lane);
            usedLanesThisWave.Add(lane);
        }
    }

    private void SpawnHazard(SpawnPhase phase, int lane)
    {
        if (laneController == null)
            return;

        StampedeHazardAI hazard = GetHazardFromPool();

        if (hazard == null)
            return;

        Vector3 spawnPos = GetSpawnPositionForLane(lane);
        Vector3 runDirection = GetRunDirection();

        Quaternion spawnRot = Quaternion.LookRotation(runDirection, Vector3.up);
        float speed = GetCurrentMoveSpeed(phase);

        hazard.Init(
            controller,
            laneController,
            spawnPos,
            spawnRot,
            runDirection,
            speed
        );

        if (!spawned.Contains(hazard))
            spawned.Add(hazard);

        lastSpawnedLane = lane;

        if (debugLogs)
        {
            string phaseName = phase != null ? phase.phaseName : "Fallback";
            Debug.Log("[StampedeHazardSpawner] Spawned lane: " + lane + " | " + phaseName + " | Speed: " + speed);
        }
    }

    private float GetCurrentMoveSpeed(SpawnPhase phase)
    {
        if (phase == null)
            return RandomRangeSafe(minMoveSpeed, maxMoveSpeed);

        return RandomRangeSafe(phase.minMoveSpeed, phase.maxMoveSpeed);
    }

    private int GetRandomLane(List<int> usedLanesThisWave)
    {
        int laneCount = laneController.laneCount;

        if (laneCount <= 1)
            return 0;

        List<int> candidates = new List<int>();

        for (int i = 0; i < laneCount; i++)
        {
            if (!allowSameLaneBackToBack && i == lastSpawnedLane)
                continue;

            if (usedLanesThisWave != null && usedLanesThisWave.Contains(i))
                continue;

            candidates.Add(i);
        }

        if (candidates.Count > 0)
            return candidates[Random.Range(0, candidates.Count)];

        return Random.Range(0, laneCount);
    }

    private Vector3 GetSpawnPositionForLane(int lane)
    {
        Vector3 lanePosition = laneController.GetLaneWorldPosition(lane);
        Vector3 forward = laneController.GetForwardDirection();

        Vector3 spawnDirection = hazardsMoveTowardPlayer ? forward : -forward;

        Vector3 spawnPosition = lanePosition + spawnDirection * spawnDistanceFromLanes;
        spawnPosition.y += spawnHeightOffset;

        return spawnPosition;
    }

    private Vector3 GetRunDirection()
    {
        Vector3 forward = laneController.GetForwardDirection();

        if (hazardsMoveTowardPlayer)
            return -forward;

        return forward;
    }

    private float RandomRangeSafe(float min, float max)
    {
        if (max < min)
        {
            float temp = min;
            min = max;
            max = temp;
        }

        return Random.Range(min, max);
    }

    private StampedeHazardAI GetHazardFromPool()
    {
        while (pool.Count > 0)
        {
            StampedeHazardAI hazard = pool.Dequeue();

            if (hazard != null && !hazard.gameObject.activeSelf)
                return hazard;
        }

        StampedeHazardAI prefab = hazardPrefabs[Random.Range(0, hazardPrefabs.Length)];

        if (prefab == null)
            return null;

        StampedeHazardAI instance = Instantiate(prefab, transform);
        instance.gameObject.SetActive(false);

        return instance;
    }

    private void PrewarmPool()
    {
        if (pool.Count > 0)
            return;

        for (int i = 0; i < prewarmCount; i++)
        {
            StampedeHazardAI prefab = hazardPrefabs[Random.Range(0, hazardPrefabs.Length)];

            if (prefab == null)
                continue;

            StampedeHazardAI instance = Instantiate(prefab, transform);
            instance.gameObject.SetActive(false);
            pool.Enqueue(instance);
        }
    }
}
