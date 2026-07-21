using System.Collections.Generic;
using UnityEngine;

public class RiverRideableSpawner : MonoBehaviour
{
    [System.Serializable]
    public class RideableSpawnEntry
    {
        public RiverRideableObject prefab;
        public float weight = 1f;

        [Header("Optional Behavior Override")]
        public bool overrideBehavior;
        public RiverRideableAIBehavior.RideableBehaviorType behaviorType =
            RiverRideableAIBehavior.RideableBehaviorType.Straight;
    }

    [Header("References")]
    public Transform spawnReference;
    public Transform riverCenterReference;
    public Transform riverDirectionReference;

    [Header("Prefabs")]
    public RideableSpawnEntry[] rideablePrefabs;

    [Header("River Bounds")]
    public float riverHalfWidth = 5f;
    public float edgePadding = 0.8f;

    [Header("Spawn Distances")]
    public float spawnAheadDistance = 35f;
    public float despawnBehindDistance = 15f;

    [Header("Spawn Gap")]
    public float minForwardGap = 7f;
    public float maxForwardGap = 13f;

    [Header("Movement")]
    public float riverCurrentSpeed = 12f;
    public bool moveOppositeRiverForward = true;

    [Header("Limits")]
    public int maxActiveRideables = 12;
    public int initialSpawnCount = 5;

    [Header("Parenting")]
    public Transform activeParent;

    [Header("Water Height")]
    public Transform waterSurfaceReference;
    public float waterY = 0f;
    public bool useWaterSurfaceReference = true;

    [Header("Facing")]
    public bool faceSameDirectionAsPlayer = true;
    public Transform playerFacingReference;

    [Header("Despawn Visibility")]
    public bool useCameraBasedDespawn = true;
    public Camera despawnCamera;
    public float despawnViewportPadding = 0.25f;

    [Header("Scene Authored Rideables")]
    public Transform sceneRideableRuntimeParent;

    [Tooltip("Prevents scene jump rideables from being returned to the tile immediately after landing.")]
    public float sceneRideableDespawnGraceSeconds = 3f;

    [Header("Debug")]
    public bool debugLogs;

    private readonly List<RiverRideableObject> activeRideables = new List<RiverRideableObject>();
    private readonly Dictionary<RiverRideableObject, Queue<RiverRideableObject>> pool =
        new Dictionary<RiverRideableObject, Queue<RiverRideableObject>>();

    private readonly Dictionary<RiverRideableObject, bool> sceneRideableMoveOverrides =
    new Dictionary<RiverRideableObject, bool>();

    private readonly List<RiverRideableObject> respawnCandidates =
    new List<RiverRideableObject>();

    private readonly Dictionary<RiverRideableObject, float> sceneRideableDespawnProtectedUntil =
    new Dictionary<RiverRideableObject, float>();

    private bool isRunning;

    private void Awake()
    {
        if (spawnReference == null)
            spawnReference = transform;

        if (activeParent == null)
            activeParent = transform;
    }

    private float GetWaterY()
    {
        if (useWaterSurfaceReference && waterSurfaceReference != null)
            return waterSurfaceReference.position.y;

        return waterY;
    }

    public float GetWaterYForExternalUse()
    {
        return GetWaterY();
    }

    private bool IsSceneRideableDespawnProtected(RiverRideableObject rideable)
    {
        if (rideable == null)
            return false;

        if (!sceneRideableDespawnProtectedUntil.TryGetValue(rideable, out float protectedUntil))
            return false;

        if (Time.time < protectedUntil)
            return true;

        sceneRideableDespawnProtectedUntil.Remove(rideable);
        return false;
    }

    public Vector3 GetRiverForwardForExternalUse()
    {
        return GetRiverForward();
    }

    public bool GetMoveOppositeRiverForwardForExternalUse()
    {
        return moveOppositeRiverForward;
    }

    public void UnregisterSceneRideable(RiverRideableObject rideable)
    {
        if (rideable == null)
            return;

        activeRideables.Remove(rideable);
        sceneRideableMoveOverrides.Remove(rideable);
        sceneRideableDespawnProtectedUntil.Remove(rideable);
    }

    private float GetSafeHalfWidthForSceneRideable(Vector3 worldPosition)
    {
        Vector3 forward = GetRiverForward();
        Vector3 right = GetRiverRight(forward);

        Vector3 center =
            riverCenterReference != null
                ? riverCenterReference.position
                : Vector3.zero;

        float lateralDistance =
            Mathf.Abs(Vector3.Dot(worldPosition - center, right));

        return Mathf.Max(
            riverHalfWidth,
            lateralDistance + edgePadding + 0.5f
        );
    }

    private bool IsOutsideDespawnCamera(RiverRideableObject rideable)
    {
        if (!useCameraBasedDespawn)
            return true;

        Camera cam = despawnCamera;

        if (cam == null)
            cam = Camera.current;

        if (cam == null)
        {
            if (debugLogs)
                Debug.LogWarning("[RiverRideableSpawner] Missing despawn camera.");

            return false;
        }

        Vector3 checkPosition = rideable.transform.position;

        Transform mountPoint = rideable.mountPoint;

        if (mountPoint != null)
            checkPosition = mountPoint.position;

        Vector3 viewportPoint = cam.WorldToViewportPoint(checkPosition);

        if (viewportPoint.z < 0f)
            return true;

        return viewportPoint.x < -despawnViewportPadding ||
               viewportPoint.x > 1f + despawnViewportPadding ||
               viewportPoint.y < -despawnViewportPadding ||
               viewportPoint.y > 1f + despawnViewportPadding;
    }


    public RiverRideableObject GetRandomAvailableRideableForRespawn(
    float minForwardDistance = 3f,
    float maxForwardDistance = 35f
)
    {
        respawnCandidates.Clear();

        float referenceProjection = GetForwardProjection(spawnReference.position);

        for (int i = 0; i < activeRideables.Count; i++)
        {
            RiverRideableObject rideable = activeRideables[i];

            if (rideable == null)
                continue;

            if (!rideable.IsAvailable)
                continue;

            if (rideable.IsRetiring)
                continue;

            float distance =
                GetForwardProjection(rideable.transform.position) -
                referenceProjection;

            if (distance < minForwardDistance)
                continue;

            if (distance > maxForwardDistance)
                continue;

            respawnCandidates.Add(rideable);
        }

        if (respawnCandidates.Count == 0)
        {
            for (int i = 0; i < activeRideables.Count; i++)
            {
                RiverRideableObject rideable = activeRideables[i];

                if (rideable == null)
                    continue;

                if (!rideable.IsAvailable)
                    continue;

                if (rideable.IsRetiring)
                    continue;

                respawnCandidates.Add(rideable);
            }
        }

        if (respawnCandidates.Count == 0)
            return null;

        return respawnCandidates[
            Random.Range(0, respawnCandidates.Count)
        ];
    }

    private float GetReferenceForwardAmount()
    {
        if (spawnReference == null)
            return 0f;

        Vector3 forward = GetRiverForward();

        if (riverCenterReference != null)
        {
            Vector3 fromCenter = spawnReference.position - riverCenterReference.position;
            return Vector3.Dot(fromCenter, forward);
        }

        return Vector3.Dot(spawnReference.position, forward);
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

    private Vector3 GetSpawnFacingDirection()
    {
        if (faceSameDirectionAsPlayer && playerFacingReference != null)
        {
            Vector3 playerForward = playerFacingReference.forward;
            playerForward.y = 0f;

            if (playerForward.sqrMagnitude > 0.001f)
                return playerForward.normalized;
        }

        Vector3 riverForward = GetRiverForward();
        riverForward.y = 0f;

        if (riverForward.sqrMagnitude > 0.001f)
            return riverForward.normalized;

        return Vector3.forward;
    }

    private void Update()
    {
        if (!isRunning)
            return;

        TickActiveRideables();
        DespawnOldRideables();
        FillAhead();
    }

    public void StartSpawning()
    {
        isRunning = true;

        ClearAllActive();

        for (int i = 0; i < initialSpawnCount; i++)
        {
            float distance = Mathf.Lerp(
                minForwardGap,
                spawnAheadDistance,
                initialSpawnCount <= 1 ? 1f : i / (float)(initialSpawnCount - 1)
            );

            SpawnRideableAtDistance(distance);
        }

        FillAhead();
    }

    public void StopSpawning()
    {
        isRunning = false;
    }

    public void StopAndClear()
    {
        isRunning = false;
        ClearAllActive();
    }

    private bool IsUsefulAheadRideable(RiverRideableObject rideable)
    {
        if (rideable == null)
            return false;

        if (!rideable.IsAvailable)
            return false;

        if (rideable.IsRetiring)
            return false;

        float referenceProjection = GetForwardProjection(spawnReference.position);
        float rideableProjection = GetForwardProjection(rideable.transform.position);

        float distanceFromReference = rideableProjection - referenceProjection;

        return distanceFromReference > 0f;
    }

    private int GetUsefulAheadRideableCount()
    {
        int count = 0;

        for (int i = 0; i < activeRideables.Count; i++)
        {
            if (IsUsefulAheadRideable(activeRideables[i]))
                count++;
        }

        return count;
    }

    public void RegisterSceneRideable(
    RiverRideableObject rideable,
    bool useMoveDirectionOverride = false,
    bool moveOppositeRiverForwardOverride = false,
    Transform runtimeParentOverride = null
    )
    {
        if (rideable == null)
            return;

        if (activeRideables.Contains(rideable))
        {
            activeRideables.Remove(rideable);
            sceneRideableMoveOverrides.Remove(rideable);
            sceneRideableDespawnProtectedUntil.Remove(rideable);
        }

        Transform runtimeParent =
            runtimeParentOverride != null
                ? runtimeParentOverride
                : sceneRideableRuntimeParent;

        if (runtimeParent != null)
            rideable.transform.SetParent(runtimeParent, true);

        rideable.isSceneAuthoredRideable = true;

        rideable.ResetForSpawnerReuse();

        rideable.canBeMounted = true;
        rideable.SetSplashWaterY(GetWaterY());

        Vector3 position = rideable.transform.position;
        position.y = GetWaterY() + rideable.heightAboveWater;
        rideable.transform.position = position;

        bool finalMoveOppositeRiverForward =
            useMoveDirectionOverride
                ? moveOppositeRiverForwardOverride
                : moveOppositeRiverForward;

        if (useMoveDirectionOverride)
            sceneRideableMoveOverrides[rideable] = moveOppositeRiverForwardOverride;
        else
            sceneRideableMoveOverrides.Remove(rideable);

        rideable.isMovingAgainstRiver = false;

        RiverRideableAIBehavior behavior =
            rideable.GetComponent<RiverRideableAIBehavior>();

        if (behavior != null)
        {
            float safeSceneHalfWidth =
      GetSafeHalfWidthForSceneRideable(rideable.transform.position);

            behavior.InitializeFromCurrentPosition(
                riverCenterReference,
                riverDirectionReference,
                safeSceneHalfWidth,
                finalMoveOppositeRiverForward
            );

            behavior.SetManagedBySpawner(true);
        }

        activeRideables.Add(rideable);

        sceneRideableDespawnProtectedUntil[rideable] =
    Time.time + sceneRideableDespawnGraceSeconds;
    }

    private void TickActiveRideables()
    {
        for (int i = 0; i < activeRideables.Count; i++)
        {
            RiverRideableObject rideable = activeRideables[i];

            if (rideable == null)
                continue;

            if (rideable.IsRetiring)
                continue;

            bool finalMoveOppositeRiverForward = moveOppositeRiverForward;

            if (sceneRideableMoveOverrides.TryGetValue(
                    rideable,
                    out bool sceneOverride
                ))
            {
                finalMoveOppositeRiverForward = sceneOverride;
            }

            RiverRideableAIBehavior behavior =
                rideable.GetComponent<RiverRideableAIBehavior>();

            Vector3 beforePosition = rideable.transform.position;

            if (behavior != null)
            {
                behavior.ManagedTick(
                    riverCurrentSpeed,
                    finalMoveOppositeRiverForward
                );
            }
            else if (!rideable.HasRider)
            {
                Vector3 forward = GetRiverForward();
                float moveDirection =
                    finalMoveOppositeRiverForward ? -1f : 1f;

                rideable.transform.position +=
                    forward * moveDirection * riverCurrentSpeed * Time.deltaTime;
            }

            Vector3 afterPosition = rideable.transform.position;
            float movedDistance = Vector3.Distance(beforePosition, afterPosition);

            if (rideable.isSceneAuthoredRideable && movedDistance > 5f)
            {
                Debug.LogWarning(
                    "[RiverRideableSpawner] Scene rideable snapped far during TickActiveRideables: " +
                    rideable.name +
                    "\nBefore: " + beforePosition +
                    "\nAfter: " + afterPosition +
                    "\nMoved: " + movedDistance +
                    "\nRiver Center: " + (riverCenterReference != null ? riverCenterReference.position.ToString() : "NULL") +
                    "\nRiver Half Width: " + riverHalfWidth,
                    rideable
                );
            }
        }
    }

    private void FillAhead()
    {
        if (rideablePrefabs == null || rideablePrefabs.Length == 0)
            return;

        int safety = 0;

        while (activeRideables.Count < maxActiveRideables)
        {
            safety++;

            if (safety > 50)
                break;

            float farthestDistance = GetFarthestActiveForwardDistance();

            if (farthestDistance >= spawnAheadDistance)
                break;

            float nextDistance =
                farthestDistance + Random.Range(minForwardGap, maxForwardGap);

            if (nextDistance < minForwardGap)
                nextDistance = minForwardGap;

            if (nextDistance > spawnAheadDistance)
                break;

            SpawnRideableAtDistance(nextDistance);
        }
    }

    private void SpawnRideableAtDistance(float forwardDistance)
    {
        RideableSpawnEntry entry = PickSpawnEntry();

        if (entry == null || entry.prefab == null)
            return;

        RiverRideableObject rideable = GetFromPool(entry.prefab);

        rideable.ResetForSpawnerReuse();

        Vector3 forward = GetRiverForward();
        Vector3 right = GetRiverRight(forward);

        float lateralLimit = Mathf.Max(0.5f, riverHalfWidth - edgePadding);
        float lateralOffset = Random.Range(-lateralLimit, lateralLimit);

        float referenceForward = GetReferenceForwardAmount();

        Vector3 spawnPosition =
            GetPositionOnRiver(
                referenceForward + forwardDistance,
                lateralOffset
            );

        spawnPosition.y = GetWaterY() + rideable.heightAboveWater;

        rideable.transform.SetParent(activeParent, true);
        rideable.transform.position = spawnPosition;

        rideable.gameObject.SetActive(true);
        rideable.SetRider(null);

        RiverRideableAIBehavior behavior =
            rideable.GetComponent<RiverRideableAIBehavior>();

        if (behavior == null)
            behavior = rideable.gameObject.AddComponent<RiverRideableAIBehavior>();

        if (entry.overrideBehavior)
            behavior.behaviorType = entry.behaviorType;

        behavior.yawOffset = rideable.spawnYawOffset;

        behavior.Initialize(
            riverCenterReference,
            riverDirectionReference,
            riverHalfWidth - edgePadding,
            lateralOffset
        );

        behavior.SetManagedBySpawner(true);

        behavior.faceOppositeScrollDirection = true;
        behavior.yawOffset = rideable.spawnYawOffset;
        behavior.SnapRotationToCorrectFacing();

        if (!activeRideables.Contains(rideable))
            activeRideables.Add(rideable);

        if (debugLogs)
            Debug.Log("[RiverRideableSpawner] Spawned " + rideable.name);
    }

    private void DespawnOldRideables()
    {
        float referenceProjection = GetForwardProjection(spawnReference.position);

        for (int i = activeRideables.Count - 1; i >= 0; i--)
        {
            RiverRideableObject rideable = activeRideables[i];

            if (rideable == null)
            {
                activeRideables.RemoveAt(i);
                continue;
            }

            // Retiring/sinking animals handle their own destroy logic.
            if (rideable.IsRetiring)
                continue;

            // Never despawn the animal the player is currently riding.
            if (rideable.HasRider)
                continue;

            if (rideable.isSceneAuthoredRideable &&
                IsSceneRideableDespawnProtected(rideable))
            {
                continue;
            }

            float rideableProjection =
                GetForwardProjection(rideable.transform.position);

            float distanceFromReference =
                rideableProjection - referenceProjection;

            bool isFarBehindPlayer =
                distanceFromReference < -despawnBehindDistance;

            if (!isFarBehindPlayer)
                continue;

            // Extra safety: do not despawn if it is still visible.
            if (useCameraBasedDespawn && !IsOutsideDespawnCamera(rideable))
                continue;

            ReturnToPool(rideable);

            if (i < activeRideables.Count && activeRideables[i] == rideable)
                activeRideables.RemoveAt(i);
            else
                activeRideables.Remove(rideable);
        }
    }

    private float GetFarthestActiveForwardDistance()
    {
        float referenceProjection = GetForwardProjection(spawnReference.position);
        float farthest = 0f;

        for (int i = 0; i < activeRideables.Count; i++)
        {
            RiverRideableObject rideable = activeRideables[i];

            if (!IsUsefulAheadRideable(rideable))
                continue;

            float projection = GetForwardProjection(rideable.transform.position);
            float distance = projection - referenceProjection;

            if (distance > farthest)
                farthest = distance;
        }

        return farthest;
    }

    private RideableSpawnEntry PickSpawnEntry()
    {
        if (rideablePrefabs == null || rideablePrefabs.Length == 0)
            return null;

        float totalWeight = 0f;

        for (int i = 0; i < rideablePrefabs.Length; i++)
        {
            if (rideablePrefabs[i] == null || rideablePrefabs[i].prefab == null)
                continue;

            totalWeight += Mathf.Max(0f, rideablePrefabs[i].weight);
        }

        if (totalWeight <= 0f)
            return rideablePrefabs[0];

        float randomValue = Random.Range(0f, totalWeight);
        float runningWeight = 0f;

        for (int i = 0; i < rideablePrefabs.Length; i++)
        {
            RideableSpawnEntry entry = rideablePrefabs[i];

            if (entry == null || entry.prefab == null)
                continue;

            runningWeight += Mathf.Max(0f, entry.weight);

            if (randomValue <= runningWeight)
                return entry;
        }

        return rideablePrefabs[0];
    }

    private RiverRideableObject GetFromPool(RiverRideableObject prefab)
    {
        if (!pool.ContainsKey(prefab))
            pool[prefab] = new Queue<RiverRideableObject>();

        Queue<RiverRideableObject> queue = pool[prefab];

        while (queue.Count > 0)
        {
            RiverRideableObject pooled = queue.Dequeue();

            if (pooled != null)
                return pooled;
        }

        RiverRideableObject created = Instantiate(prefab);
        created.name = prefab.name + "_Pooled";

        return created;
    }

    private void ReturnToPool(RiverRideableObject rideable)
    {
        if (rideable == null)
            return;

        sceneRideableMoveOverrides.Remove(rideable);
        sceneRideableDespawnProtectedUntil.Remove(rideable);

        if (rideable.isSceneAuthoredRideable)
        {
            RiverSceneJumpRideable sceneJumpRideable =
                rideable.GetComponent<RiverSceneJumpRideable>();

            if (sceneJumpRideable != null &&
                sceneJumpRideable.returnToOriginalTileOnDespawn)
            {
                sceneJumpRideable.ReturnToOriginalTile();
                return;
            }

            rideable.PrepareForPool();

            if (rideable.destroySceneAuthoredOnDespawn)
                Destroy(rideable.gameObject);
            else
                rideable.gameObject.SetActive(false);

            return;
        }

        rideable.PrepareForPool();
        rideable.gameObject.SetActive(false);

        RiverRideableObject prefabKey = FindPrefabKeyFor(rideable);

        if (prefabKey == null)
        {
            Destroy(rideable.gameObject);
            return;
        }

        if (!pool.ContainsKey(prefabKey))
            pool[prefabKey] = new Queue<RiverRideableObject>();

        pool[prefabKey].Enqueue(rideable);
    }

    private RiverRideableObject FindPrefabKeyFor(RiverRideableObject rideable)
    {
        if (rideablePrefabs == null)
            return null;

        string rideableName = rideable.name.Replace("_Pooled", "");

        for (int i = 0; i < rideablePrefabs.Length; i++)
        {
            if (rideablePrefabs[i] == null || rideablePrefabs[i].prefab == null)
                continue;

            if (rideableName.StartsWith(rideablePrefabs[i].prefab.name))
                return rideablePrefabs[i].prefab;
        }

        return null;
    }

    private void ClearAllActive()
    {
        for (int i = activeRideables.Count - 1; i >= 0; i--)
        {
            RiverRideableObject rideable = activeRideables[i];

            if (rideable == null)
                continue;

            ReturnToPool(rideable);
        }

        activeRideables.Clear();
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

    private Vector3 GetRiverRight(Vector3 forward)
    {
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        if (right.sqrMagnitude <= 0.001f)
            right = Vector3.right;

        return right;
    }
}