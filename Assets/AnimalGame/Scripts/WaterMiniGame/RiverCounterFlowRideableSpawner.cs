using System.Collections.Generic;
using UnityEngine;

public class RiverCounterFlowRideableSpawner : MonoBehaviour
{
    [System.Serializable]
    public class CounterFlowSpawnEntry
    {
        public RiverRideableObject prefab;
        public float weight = 1f;

        [Header("Behavior Override")]
        public bool overrideBehavior = true;
        public RiverRideableAIBehavior.RideableBehaviorType behaviorType =
            RiverRideableAIBehavior.RideableBehaviorType.Straight;
    }

    [Header("References")]
    public Transform playerReference;
    public Transform riverCenterReference;
    public Transform riverDirectionReference;
    public Transform waterSurfaceReference;

    [Header("Prefabs")]
    public CounterFlowSpawnEntry[] counterFlowPrefabs;

    [Header("River Direction")]
    [Tooltip("Use the same value as RiverWorldScroller > Move Opposite River Forward.")]
    public bool riverTilesMoveOppositeRiverForward = true;

    [Header("Spawn Area")]
    public float riverHalfWidth = 5f;
    public float edgePadding = 0.8f;

    [Tooltip("How far behind the player these objects spawn.")]
    public float spawnBehindDistance = 22f;

    [Tooltip("Extra random behind distance.")]
    public float spawnBehindRandom = 10f;

    [Tooltip("Destroy after object moves this far past the player.")]
    public float destroyPastPlayerDistance = 35f;

    [Tooltip("Safety cleanup if object somehow stays too far behind.")]
    public float destroyTooFarBehindDistance = 40f;

    [Header("Spawn Timing")]
    public float minSpawnDelay = 2.5f;
    public float maxSpawnDelay = 5f;
    public int maxActive = 4;

    [Header("Movement")]
    [Tooltip("World speed of counter-flow rideables.")]
    public float counterFlowSpeed = 6f;

    [Header("Water Height")]
    public bool useWaterSurfaceReference = true;
    public float waterY = 0f;

    [Tooltip("Keeps counter-flow rideables locked to water height while moving.")]
    public bool keepRideablesOnWaterY = true;

    [Tooltip("If ON, objects move opposite to the river tile movement.")]
    public bool moveOppositeToRiverTiles = true;

    [Header("Facing")]
    [Tooltip("For counter-flow animals, this usually should be ON.")]
    public bool faceMovementDirection = true;

    [Header("Parenting")]
    public Transform activeParent;

    [Header("Debug")]
    public bool debugLogs;

    private readonly List<RiverRideableObject> activeObjects = new List<RiverRideableObject>();

    private bool isRunning;
    private float spawnTimer;

    private void Awake()
    {
        if (activeParent == null)
            activeParent = transform;
    }

    private void Update()
    {
        if (!isRunning)
            return;

        TickActiveObjects();
        CleanupOldObjects();
        TickSpawner();
    }

    public void StartSpawning()
    {
        isRunning = true;
        spawnTimer = Random.Range(minSpawnDelay, maxSpawnDelay);
    }

    public void StopSpawning()
    {
        isRunning = false;
    }

    public void StopAndClear()
    {
        isRunning = false;

        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            RiverRideableObject obj = activeObjects[i];

            if (obj == null)
                continue;

            if (obj.HasRider)
                continue;

            Destroy(obj.gameObject);
        }

        activeObjects.Clear();
    }

    private float GetReferenceForwardAmount()
    {
        if (playerReference == null)
            return 0f;

        Vector3 forward = GetRiverForward();

        if (riverCenterReference != null)
        {
            Vector3 fromCenter = playerReference.position - riverCenterReference.position;
            return Vector3.Dot(fromCenter, forward);
        }

        return Vector3.Dot(playerReference.position, forward);
    }

    private Vector3 GetPositionOnRiver(float forwardAmount, float lateralOffset)
    {
        Vector3 forward = GetRiverForward();
        Vector3 right = GetRiverRight(forward);

        Vector3 center =
            riverCenterReference != null
                ? riverCenterReference.position
                : Vector3.zero;

        Vector3 position =
            center +
            forward * forwardAmount +
            right * lateralOffset;

        return position;
    }

    private void TickSpawner()
    {
        if (activeObjects.Count >= maxActive)
            return;

        spawnTimer -= Time.deltaTime;

        if (spawnTimer > 0f)
            return;

        SpawnCounterFlowObject();

        spawnTimer = Random.Range(minSpawnDelay, maxSpawnDelay);
    }

    private void SpawnCounterFlowObject()
    {
        CounterFlowSpawnEntry entry = PickSpawnEntry();

        if (entry == null || entry.prefab == null)
            return;

        RiverRideableObject rideable = Instantiate(entry.prefab, activeParent);

        Vector3 forward = GetRiverForward();
        Vector3 right = GetRiverRight(forward);

        Vector3 tileMoveDirection = GetRiverTileMoveDirection();
        Vector3 counterFlowDirection = moveOppositeToRiverTiles
            ? -tileMoveDirection
            : tileMoveDirection;

        float behindDistance =
            spawnBehindDistance + Random.Range(0f, spawnBehindRandom);

        float lateralLimit = Mathf.Max(0.5f, riverHalfWidth - edgePadding);
        float lateralOffset = Random.Range(-lateralLimit, lateralLimit);

        float referenceForward = GetReferenceForwardAmount();

        float counterDirectionAlongRiver =
            Vector3.Dot(counterFlowDirection.normalized, forward.normalized);

        if (Mathf.Abs(counterDirectionAlongRiver) < 0.001f)
            counterDirectionAlongRiver = 1f;

        float spawnForward =
            referenceForward -
            counterDirectionAlongRiver * behindDistance;

        Vector3 spawnPosition =
            GetPositionOnRiver(
                spawnForward,
                lateralOffset
            );

        spawnPosition.y = GetWaterY() + rideable.heightAboveWater;

        rideable.transform.position = spawnPosition;

        Quaternion spawnRotation = Quaternion.LookRotation(
            faceMovementDirection ? counterFlowDirection : forward,
            Vector3.up
        );

        spawnRotation *= Quaternion.Euler(0f, rideable.spawnYawOffset, 0f);

        rideable.transform.rotation = spawnRotation;

        RiverRideableAIBehavior behavior =
            rideable.GetComponent<RiverRideableAIBehavior>();

        if (behavior == null)
            behavior = rideable.gameObject.AddComponent<RiverRideableAIBehavior>();

        if (entry.overrideBehavior)
            behavior.behaviorType = entry.behaviorType;

        // Counter-flow objects move opposite the tile movement.
        bool counterFlowMoveOppositeRiverForward =
            GetMoveOppositeRiverForwardForDirection(counterFlowDirection, forward);

        behavior.SetMoveDirectionMode(counterFlowMoveOppositeRiverForward);

        // For counter-flow, facing movement direction usually looks correct.
        behavior.faceOppositeScrollDirection = !faceMovementDirection;
        behavior.yawOffset = rideable.spawnYawOffset;

        behavior.Initialize(
            riverCenterReference,
            riverDirectionReference,
            riverHalfWidth - edgePadding,
            lateralOffset
        );

        behavior.SetManagedBySpawner(true);

        behavior.SyncAIToCurrentPosition();
        behavior.SnapRotationToCorrectFacing();

        rideable.SetRider(null);

        rideable.isMovingAgainstRiver = true;

        activeObjects.Add(rideable);

        if (debugLogs)
            Debug.Log("[CounterFlowSpawner] Spawned " + rideable.name);
    }

    private void ApplyWaterY(RiverRideableObject rideable)
    {
        if (rideable == null)
            return;

        Vector3 position = rideable.transform.position;
        position.y = GetWaterY() + rideable.heightAboveWater;
        rideable.transform.position = position;

        rideable.SetSplashWaterY(GetWaterY());
    }

    private void TickActiveObjects()
    {
        Vector3 forward = GetRiverForward();
        Vector3 tileMoveDirection = GetRiverTileMoveDirection();
        Vector3 counterFlowDirection = moveOppositeToRiverTiles
            ? -tileMoveDirection
            : tileMoveDirection;

        bool counterFlowMoveOppositeRiverForward =
            GetMoveOppositeRiverForwardForDirection(counterFlowDirection, forward);

        for (int i = 0; i < activeObjects.Count; i++)
        {
            RiverRideableObject rideable = activeObjects[i];

            if (rideable == null)
                continue;

            if (rideable.IsRetiring)
                continue;

            if (rideable.HasRider)
                continue;

            RiverRideableAIBehavior behavior =
                rideable.GetComponent<RiverRideableAIBehavior>();

            if (behavior != null)
            {
                behavior.ManagedTick(
                    counterFlowSpeed,
                    counterFlowMoveOppositeRiverForward
                );
            }
            else
            {
                rideable.transform.position +=
                    counterFlowDirection * counterFlowSpeed * Time.deltaTime;
            }

            if (keepRideablesOnWaterY)
                ApplyWaterY(rideable);
        }
    }

    private void CleanupOldObjects()
    {
        Vector3 tileMoveDirection = GetRiverTileMoveDirection();
        Vector3 counterFlowDirection = moveOppositeToRiverTiles
            ? -tileMoveDirection
            : tileMoveDirection;

        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            RiverRideableObject rideable = activeObjects[i];

            if (rideable == null)
            {
                activeObjects.RemoveAt(i);
                continue;
            }

            if (rideable.IsRetiring)
                continue;

            if (rideable.HasRider)
                continue;

            Vector3 fromPlayer = rideable.transform.position - playerReference.position;

            float distanceAlongCounterFlow =
                Vector3.Dot(fromPlayer, counterFlowDirection);

            if (distanceAlongCounterFlow > destroyPastPlayerDistance ||
                distanceAlongCounterFlow < -destroyTooFarBehindDistance)
            {
                activeObjects.RemoveAt(i);
                Destroy(rideable.gameObject);
            }
        }
    }

    private CounterFlowSpawnEntry PickSpawnEntry()
    {
        if (counterFlowPrefabs == null || counterFlowPrefabs.Length == 0)
            return null;

        float totalWeight = 0f;

        for (int i = 0; i < counterFlowPrefabs.Length; i++)
        {
            CounterFlowSpawnEntry entry = counterFlowPrefabs[i];

            if (entry == null || entry.prefab == null)
                continue;

            totalWeight += Mathf.Max(0f, entry.weight);
        }

        if (totalWeight <= 0f)
            return counterFlowPrefabs[0];

        float randomValue = Random.Range(0f, totalWeight);
        float runningWeight = 0f;

        for (int i = 0; i < counterFlowPrefabs.Length; i++)
        {
            CounterFlowSpawnEntry entry = counterFlowPrefabs[i];

            if (entry == null || entry.prefab == null)
                continue;

            runningWeight += Mathf.Max(0f, entry.weight);

            if (randomValue <= runningWeight)
                return entry;
        }

        return counterFlowPrefabs[0];
    }

    private Vector3 GetRiverTileMoveDirection()
    {
        Vector3 forward = GetRiverForward();

        return riverTilesMoveOppositeRiverForward
            ? -forward
            : forward;
    }

    private bool GetMoveOppositeRiverForwardForDirection(
        Vector3 wantedDirection,
        Vector3 riverForward
    )
    {
        float dot = Vector3.Dot(wantedDirection.normalized, riverForward.normalized);

        // If wanted direction is opposite river forward,
        // then moveOppositeRiverForward should be true.
        return dot < 0f;
    }

    private float GetWaterY()
    {
        if (useWaterSurfaceReference && waterSurfaceReference != null)
            return waterSurfaceReference.position.y;

        return waterY;
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

    private Vector3 GetRiverRight(Vector3 forward)
    {
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        if (right.sqrMagnitude <= 0.001f)
            right = Vector3.right;

        return right;
    }
}