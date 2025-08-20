using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AnimalSpawner : MonoBehaviour
{
    public enum PointPickMode { Random, RoundRobin }
    public enum FaceMode { Random, FaceCenter, KeepPrefab } // default if point doesn’t override

    [Header("Points")]
    [Tooltip("Leave empty to auto-grab AnimalSpawnPoint children.")]
    public List<AnimalSpawnPoint> points = new();

    [Header("Limits & Timing")]
    public int maxAlive = 12;
    public float spawnInterval = 2.0f;
    public int warmupCount = 6;

    [Header("NavMesh Placement")]
    public bool placeOnNavMesh = true;
    public float navmeshSearchRadius = 5f;

    [Header("Player Separation")]
    public float minDistanceFromPlayer = 4f;
    public string playerTag = "Player";

    [Header("Facing (default)")]
    public FaceMode defaultFaceMode = FaceMode.Random;

    [Header("Organization")]
    public Transform spawnParent;

    [Header("Point Selection")]
    public PointPickMode pickMode = PointPickMode.Random;

    // NEW: Scene-wide cap for aggressive animals
    [Header("Aggressive AI Limit (scene-wide)")]
    [Tooltip("If ON, limits total count of all aggressive AIs alive in the scene (from ALL spawners + pre-placed).")]
    public bool limitAggressive = true;

    [Tooltip("Max alive aggressive AIs allowed at once in the scene.")]
    public int maxAggressiveAlive = 8;

    [Header("🌍 Dynamic Grind Spawn")]
    public bool enableDynamicSpawns = false;

    [Tooltip("Animals this spawner can spawn dynamically around the player.")]
    public List<GameObject> dynamicAnimalPrefabs;

    [Tooltip("How often to attempt a dynamic spawn (seconds).")]
    public float dynamicSpawnInterval = 5f;

    [Tooltip("Minimum distance from player to spawn.")]
    public float minSpawnDistance = 20f;

    [Tooltip("Maximum distance from player to spawn.")]
    public float maxSpawnDistance = 40f;

    [Tooltip("Maximum number of dynamic creatures alive from this spawner.")]
    public int maxDynamicAlive = 6;

    [Header("Dynamic Spawn NavMesh")]
    [Tooltip("How far around the candidate point to search the NavMesh for a valid position.")]
    public float dynamicNavmeshSearchRadius = 12f;

    [Tooltip("How many attempts per tick to find a valid spawn around the player.")]
    public int dynamicSpawnTries = 12;

    [Tooltip("If ON, only spawn points that are reachable (complete path) from near the player.")]
    public bool requireReachableFromPlayer = true;

    [Header("Dynamic Spawn Constraints")]
    [Tooltip("Area mask used for sampling/paths (default = AllAreas).")]
    public int dynamicAreaMask = NavMesh.AllAreas;

    [Tooltip("Reject points whose height differs too much from player’s nav pos.")]
    public float maxYDeltaFromPlayer = 8f;

    [Tooltip("Reject points too close to navmesh edge.")]
    public float minEdgeDistance = 0.6f;

    [Tooltip("Optional world bounds (BoxCollider) to keep spawns inside your map).")]
    public BoxCollider worldBounds;

    [Tooltip("If > 0, auto-despawn dynamic spawns that drift this far from the player.")]
    public float dynamicDespawnDistance = 120f;

    [Tooltip("How often to scan for far dynamic spawns to despawn.")]
    public float dynamicDespawnCheckInterval = 2f;

    // NEW: No-Spawn zones (by Tag with Sphere Colliders)
    [Header("🚫 No-Spawn Zones")]
    [Tooltip("Respect zones tagged with 'NoSpawn' (spheres with SphereCollider).")]
    public bool respectNoSpawnZones = true;

    [Tooltip("Tag used for no-spawn zones.")]
    public string noSpawnTag = "NoSpawn";

    [Tooltip("Probe radius used to test if a candidate point is inside a NoSpawn collider (small is fine).")]
    public float noSpawnProbeRadius = 0.1f;

    private float _nextDynamicSpawnTime;
    private readonly List<GameObject> _activeDynamic = new();

    public bool disableSpawns = false;

    // ——— runtime ———
    float timer;
    Transform player;
    int rrIndex = 0; // round-robin
    readonly HashSet<GameObject> warmed = new();

    // temp buffer to avoid GC for no-spawn checks
    static readonly Collider[] _noSpawnBuf = new Collider[8];

    void Awake()
    {
        if (spawnParent == null)
        {
            var go = new GameObject($"{name}_Spawns");
            go.transform.SetParent(transform);
            spawnParent = go.transform;
        }

        if (points == null || points.Count == 0)
            points = new List<AnimalSpawnPoint>(GetComponentsInChildren<AnimalSpawnPoint>(true));
    }

    void Start()
    {
        // Warm up pools for all prefabs used by any point (unique)
        foreach (var p in points)
        {
            if (!p) continue;
            foreach (var w in p.prefabs)
            {
                if (!w.prefab || warmed.Contains(w.prefab)) continue;
                PoolManager.Warmup(w.prefab, warmupCount);
                warmed.Add(w.prefab);
            }
        }

        if (enableDynamicSpawns && dynamicAnimalPrefabs != null)
        {
            foreach (var d in dynamicAnimalPrefabs)
            {
                if (!d || warmed.Contains(d)) continue;
                PoolManager.Warmup(d, warmupCount);
                warmed.Add(d);
            }
        }

        // Cache player if present
        var go = GameObject.FindGameObjectWithTag(string.IsNullOrEmpty(playerTag) ? "Player" : playerTag);
        player = go ? go.transform : null;

        timer = spawnInterval;
    }

    void Update()
    {
        // Lazy refind player (handles runtime-spawned player)
        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag(string.IsNullOrEmpty(playerTag) ? "Player" : playerTag);
            if (go) player = go.transform;
        }

        if (enableDynamicSpawns && dynamicDespawnDistance > 0f && Time.time >= _nextDespawnSweep)
        {
            _nextDespawnSweep = Time.time + dynamicDespawnCheckInterval;
            float d2Max = dynamicDespawnDistance * dynamicDespawnDistance;

            for (int i = _activeDynamic.Count - 1; i >= 0; i--)
            {
                var go2 = _activeDynamic[i];
                if (!go2 || !go2.activeInHierarchy) { _activeDynamic.RemoveAt(i); continue; }

                Vector3 toPlayer = (go2.transform.position - player.position);
                if (toPlayer.sqrMagnitude > d2Max)
                {
                    var po = go2.GetComponent<PooledObject>();
                    if (po) po.Despawn(); else go2.SetActive(false);
                    _activeDynamic.RemoveAt(i);
                }
            }
        }

        if (enableDynamicSpawns && Time.time >= _nextDynamicSpawnTime)
        {
            _nextDynamicSpawnTime = Time.time + dynamicSpawnInterval;
            TryDynamicSpawn();
        }


        if (disableSpawns)
            return;

        // Respect cap of total alive under this spawner
        int alive = spawnParent ? spawnParent.childCount : 0;
        if (alive >= maxAlive) { timer = spawnInterval; return; }

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = spawnInterval;

        // Choose a point
        var point = PickPoint();
        if (!point) return;

        // Compute position
        if (!TryGetPointSpawnPosition(point, out Vector3 pos))
        {
            //Do nothing for now
        }

        // Pick prefab with aggressive limit respected
        var prefab = PickWeightedPrefabRespectingAggressiveCap(point.prefabs);
        if (!prefab) return;

        // Facing (point override or spawner default)
        Quaternion rot = GetFacing(point, pos);

        // Spawn (pooled)
        PoolManager.Spawn(prefab, pos, rot, spawnParent);
    }

    AnimalSpawnPoint PickPoint()
    {
        if (points == null || points.Count == 0) return null;

        // Filter to valid (enabled, has at least one prefab with weight>0 and not null)
        var candidates = s_list; candidates.Clear();
        foreach (var p in points)
        {
            if (!p || !p.isActiveAndEnabled) continue;
            bool hasPrefab = false;
            foreach (var w in p.prefabs)
                if (w.prefab && w.weight > 0f) { hasPrefab = true; break; }
            if (hasPrefab) candidates.Add(p);
        }
        if (candidates.Count == 0) return null;

        if (pickMode == PointPickMode.Random)
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];

        // Round-robin
        rrIndex = (rrIndex + 1) % candidates.Count;
        return candidates[rrIndex];
    }

    private float _nextDespawnSweep;

    // ---------- NO-SPAWN CHECK ----------
    bool IsInNoSpawnZone(Vector3 position)
    {
        if (!respectNoSpawnZones) return false;

        // Small probe; if the point lies inside a SphereCollider tagged NoSpawn, this will overlap it.
        int count = Physics.OverlapSphereNonAlloc(
            position,
            Mathf.Max(0.01f, noSpawnProbeRadius),
            _noSpawnBuf,
            ~0, // all layers
            QueryTriggerInteraction.Collide // include triggers (common for helper volumes)
        );

        for (int i = 0; i < count; i++)
        {
            var col = _noSpawnBuf[i];
            if (!col) continue;
            if (!col.CompareTag(noSpawnTag)) continue;

            // Be strict: use ClosestPoint to ensure the point is truly inside the collider volume
            Vector3 cp = col.ClosestPoint(position);
            // If ClosestPoint returns the same point, we’re inside (or on surface)
            if ((cp - position).sqrMagnitude < 0.0001f)
                return true;
        }
        return false;
    }
    // -----------------------------------

    private bool TryFindSpawnAroundPlayer(out Vector3 outPos)
    {
        outPos = default;
        if (!player) return false;

        // Player’s nav island
        Vector3 playerNav = player.position;
        if (placeOnNavMesh && NavMesh.SamplePosition(player.position, out var pHit, 4f, dynamicAreaMask))
            playerNav = pHit.position;

        for (int i = 0; i < dynamicSpawnTries; i++)
        {
            float angle = UnityEngine.Random.Range(0f, 360f);
            float dist = UnityEngine.Random.Range(minSpawnDistance, maxSpawnDistance);

            Vector3 cand = player.position + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * dist;

            // Keep candidates inside world bounds (optional)
            if (worldBounds)
            {
                Vector3 local = worldBounds.transform.InverseTransformPoint(cand);
                Vector3 half = worldBounds.size * 0.5f;
                local.x = Mathf.Clamp(local.x, -half.x, half.x);
                local.y = Mathf.Clamp(local.y, -half.y, half.y);
                local.z = Mathf.Clamp(local.z, -half.z, half.z);
                cand = worldBounds.transform.TransformPoint(local);
            }

            if (!placeOnNavMesh)
            {
                if (IsInNoSpawnZone(cand)) continue; // 🔴 respect no-spawn
                outPos = cand;
                return true;
            }

            // Snap to NavMesh near candidate
            if (!NavMesh.SamplePosition(cand, out var hit, dynamicNavmeshSearchRadius, dynamicAreaMask))
                continue;

            // Height sanity vs player's nav pos
            if (Mathf.Abs(hit.position.y - playerNav.y) > maxYDeltaFromPlayer)
                continue;

            // Stay away from edges
            if (NavMesh.FindClosestEdge(hit.position, out var edge, dynamicAreaMask))
            {
                if (edge.distance < minEdgeDistance) continue;
            }

            if (requireReachableFromPlayer)
            {
                var path = new NavMeshPath();
                if (!NavMesh.CalculatePath(playerNav, hit.position, dynamicAreaMask, path) ||
                    path.status != NavMeshPathStatus.PathComplete ||
                    path.corners == null || path.corners.Length < 2)
                {
                    continue; // different island / blocked / degenerate
                }
            }

            if (IsInNoSpawnZone(hit.position)) continue; // 🔴 respect no-spawn

            outPos = hit.position;
            return true;
        }

        return false;
    }

    private void TryDynamicSpawn()
    {
        // Clean null OR inactive pooled instances
        _activeDynamic.RemoveAll(a => !a || !a.activeInHierarchy);

        if (_activeDynamic.Count >= maxDynamicAlive) return;
        if (dynamicAnimalPrefabs == null || dynamicAnimalPrefabs.Count == 0) return;
        if (!player) return;

        // Find a valid position on NavMesh around the player (ring)
        if (!TryFindSpawnAroundPlayer(out var pos)) return;

        // Pick a prefab (respect aggressive cap if you want)
        GameObject prefab = dynamicAnimalPrefabs[UnityEngine.Random.Range(0, dynamicAnimalPrefabs.Count)];

        if (limitAggressive && IsAggressivePrefab(prefab))
        {
            int currAgg = CountAggressiveAliveSceneWide();
            if (currAgg >= Mathf.Max(0, maxAggressiveAlive))
            {
                // Try to pick a non-aggressive from the dynamic list
                var nonAgg = dynamicAnimalPrefabs.FindAll(p => p && !IsAggressivePrefab(p));
                if (nonAgg.Count == 0) return; // nothing else to spawn
                prefab = nonAgg[UnityEngine.Random.Range(0, nonAgg.Count)];
            }
        }

        // Random facing
        Quaternion rot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

        // ✅ Use the pool, and parent under spawnParent (consistent with your other spawns)
        var go = PoolManager.Spawn(prefab, pos, rot, spawnParent);
        if (go) _activeDynamic.Add(go);
    }

    GameObject PickWeightedPrefab(List<AnimalSpawnPoint.WeightedPrefab> list)
    {
        float total = 0f;
        for (int i = 0; i < list.Count; i++)
            total += Mathf.Max(0f, list[i].weight);
        if (total <= 0f) return null;

        float r = UnityEngine.Random.value * total;
        float cum = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            cum += Mathf.Max(0f, list[i].weight);
            if (r <= cum) return list[i].prefab;
        }
        return list[list.Count - 1].prefab;
    }

    // NEW: Prefab picker that respects the scene-wide aggressive cap
    GameObject PickWeightedPrefabRespectingAggressiveCap(List<AnimalSpawnPoint.WeightedPrefab> list)
    {
        if (!limitAggressive) return PickWeightedPrefab(list);

        int currentAggressive = CountAggressiveAliveSceneWide();
        if (currentAggressive < Mathf.Max(0, maxAggressiveAlive))
        {
            // Under cap → normal pick
            return PickWeightedPrefab(list);
        }

        // Cap reached → try to pick ONLY non-aggressive prefabs from this point (by weight)
        float total = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            var w = list[i];
            if (!w.prefab) continue;
            if (!IsAggressivePrefab(w.prefab)) total += Mathf.Max(0f, w.weight);
        }
        if (total <= 0f)
        {
            // No non-aggressive prefabs available here → skip this tick
            return null;
        }

        float r = UnityEngine.Random.value * total;
        float cum = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            var w = list[i];
            if (!w.prefab) continue;
            if (IsAggressivePrefab(w.prefab)) continue;

            cum += Mathf.Max(0f, w.weight);
            if (r <= cum) return w.prefab;
        }
        // Fallback (shouldn't really hit)
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var w = list[i];
            if (w.prefab && !IsAggressivePrefab(w.prefab))
                return w.prefab;
        }
        return null;
    }

    // Determine if a prefab is an aggressive AI by inspecting its CuteAnimalAI type
    public bool IsAggressivePrefab(GameObject prefab)
    {
        if (!prefab) return false;
        var ai = prefab.GetComponent<CuteAnimalAI>();
        if (!ai) return false;

        // Treat these as aggressive families
        var t = ai.aiType;
        return t == CuteAnimalAI.AIType.Aggressive
            || t == CuteAnimalAI.AIType.AggressiveType1
            || t == CuteAnimalAI.AIType.AggressiveType2
            || t == CuteAnimalAI.AIType.AggressiveType3
            || t == CuteAnimalAI.AIType.AggressiveJumping;
        // (If you add AggressiveType4 later, include it here.)
    }

    // Scene-wide count of currently-alive aggressive AIs
    int CountAggressiveAliveSceneWide()
    {
        var all = GameObject.FindObjectsOfType<CuteAnimalAI>(); // active only
        int count = 0;
        for (int i = 0; i < all.Length; i++)
        {
            var ai = all[i];
            if (!ai) continue;

            bool isAgg = ai.aiType == CuteAnimalAI.AIType.Aggressive
                      || ai.aiType == CuteAnimalAI.AIType.AggressiveType1
                      || ai.aiType == CuteAnimalAI.AIType.AggressiveType2
                      || ai.aiType == CuteAnimalAI.AIType.AggressiveType3
                      || ai.aiType == CuteAnimalAI.AIType.AggressiveJumping;

            if (!isAgg) continue;

            // Exclude dead
            var h = ai.GetComponent<Health>();
            if (h != null && h.IsDead) continue;

            count++;
        }
        return count;
    }

    bool TryGetPointSpawnPosition(AnimalSpawnPoint point, out Vector3 pos)
    {
        // roll local offset based on the point’s shape/size
        Vector3 local;
        if (point.shape == AnimalSpawnPoint.SpawnShape.Circle)
        {
            float r = Mathf.Max(point.size.x, point.size.z);
            var flat = UnityEngine.Random.insideUnitCircle * r;
            local = new Vector3(flat.x, 0f, flat.y);
        }
        else
        {
            local = new Vector3(
                UnityEngine.Random.Range(-point.size.x, point.size.x),
                UnityEngine.Random.Range(-point.size.y, point.size.y),
                UnityEngine.Random.Range(-point.size.z, point.size.z));
        }

        pos = point.transform.TransformPoint(local);

        // Player distance (point override if >=0)
        float minDist = (point.minDistanceFromPlayer >= 0f) ? point.minDistanceFromPlayer : minDistanceFromPlayer;

        // Try a few retries if too close to player or off-mesh or inside no-spawn zones
        const int retries = 6;
        for (int i = 0; i <= retries; i++)
        {
            if (player && Vector3.Distance(pos, player.position) < minDist)
            {
                // reroll
                if (point.shape == AnimalSpawnPoint.SpawnShape.Circle)
                {
                    float r = Mathf.Max(point.size.x, point.size.z);
                    var flat = UnityEngine.Random.insideUnitCircle * r;
                    local = new Vector3(flat.x, 0f, flat.y);
                }
                else
                {
                    local = new Vector3(
                        UnityEngine.Random.Range(-point.size.x, point.size.x),
                        UnityEngine.Random.Range(-point.size.y, point.size.y),
                        UnityEngine.Random.Range(-point.size.z, point.size.z));
                }
                pos = point.transform.TransformPoint(local);
                continue;
            }

            if (respectNoSpawnZones && IsInNoSpawnZone(pos))
            {
                // reroll another local offset
                if (i < retries)
                {
                    if (point.shape == AnimalSpawnPoint.SpawnShape.Circle)
                    {
                        float r = Mathf.Max(point.size.x, point.size.z);
                        var flat = UnityEngine.Random.insideUnitCircle * r;
                        local = new Vector3(flat.x, 0f, flat.y);
                    }
                    else
                    {
                        local = new Vector3(
                            UnityEngine.Random.Range(-point.size.x, point.size.x),
                            UnityEngine.Random.Range(-point.size.y, point.size.y),
                            UnityEngine.Random.Range(-point.size.z, point.size.z));
                    }
                    pos = point.transform.TransformPoint(local);
                    continue;
                }
                return false;
            }

            if (placeOnNavMesh)
            {
                if (NavMesh.SamplePosition(pos, out var hit, navmeshSearchRadius, NavMesh.AllAreas))
                {
                    if (respectNoSpawnZones && IsInNoSpawnZone(hit.position))
                    {
                        // try again if allowed
                        if (i < retries)
                        {
                            if (point.shape == AnimalSpawnPoint.SpawnShape.Circle)
                            {
                                float r = Mathf.Max(point.size.x, point.size.z);
                                var flat = UnityEngine.Random.insideUnitCircle * r;
                                local = new Vector3(flat.x, 0f, flat.y);
                            }
                            else
                            {
                                local = new Vector3(
                                    UnityEngine.Random.Range(-point.size.x, point.size.x),
                                    UnityEngine.Random.Range(-point.size.y, point.size.y),
                                    UnityEngine.Random.Range(-point.size.z, point.size.z));
                            }
                            pos = point.transform.TransformPoint(local);
                            continue;
                        }
                        return false;
                    }

                    pos = hit.position;
                    return true;
                }
                else
                {
                    // re-roll a new local offset and try again
                    if (i < retries)
                    {
                        if (point.shape == AnimalSpawnPoint.SpawnShape.Circle)
                        {
                            float r = Mathf.Max(point.size.x, point.size.z);
                            var flat = UnityEngine.Random.insideUnitCircle * r;
                            local = new Vector3(flat.x, 0f, flat.y);
                        }
                        else
                        {
                            local = new Vector3(
                                UnityEngine.Random.Range(-point.size.x, point.size.x),
                                UnityEngine.Random.Range(-point.size.y, point.size.y),
                                UnityEngine.Random.Range(-point.size.z, point.size.z));
                        }
                        pos = point.transform.TransformPoint(local);
                        continue;
                    }
                    return false;
                }
            }

            // no NavMesh placement
            return true;
        }

        return false;
    }

    Quaternion GetFacing(AnimalSpawnPoint point, Vector3 pos)
    {
        var mode = point.faceMode;
        if (mode == AnimalSpawnPoint.FaceMode.UseSpawner)
        {
            switch (defaultFaceMode)
            {
                case FaceMode.FaceCenter:
                    Vector3 dir = (point.transform.position - pos); dir.y = 0f;
                    if (dir.sqrMagnitude < 0.001f) dir = UnityEngine.Random.insideUnitSphere;
                    return Quaternion.LookRotation(dir.normalized);
                case FaceMode.KeepPrefab:
                    return transform.rotation; // or Quaternion.identity if you prefer
                default:
                    return Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            }
        }
        else
        {
            switch (mode)
            {
                case AnimalSpawnPoint.FaceMode.FaceCenter:
                    Vector3 dir = (point.transform.position - pos); dir.y = 0f;
                    if (dir.sqrMagnitude < 0.001f) dir = UnityEngine.Random.insideUnitSphere;
                    return Quaternion.LookRotation(dir.normalized);
                case AnimalSpawnPoint.FaceMode.KeepPrefab:
                    return transform.rotation;
                default:
                    return Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            }
        }
    }

    // small temp list to avoid GC
    static List<AnimalSpawnPoint> s_list = new(32);

#if UNITY_EDITOR
    // Optional: quick gizmo to visualize dynamic ring & rejected points (toggle in editor)
    [Header("Debug / Gizmos")]
    public bool drawDynamicRing = true;
    public Color ringMinColor = new Color(0f, 1f, 0.6f, 0.3f);
    public Color ringMaxColor = new Color(0f, 0.8f, 1f, 0.15f);

    private void OnDrawGizmosSelected()
    {
        if (!drawDynamicRing) return;
        if (!player && Application.isPlaying)
        {
            var go = GameObject.FindGameObjectWithTag(string.IsNullOrEmpty(playerTag) ? "Player" : playerTag);
            if (go) player = go.transform;
        }
        var center = player ? player.position : transform.position;

        Gizmos.color = ringMinColor;
        UnityEditor.Handles.color = ringMinColor;
        UnityEditor.Handles.DrawWireDisc(center, Vector3.up, minSpawnDistance);

        Gizmos.color = ringMaxColor;
        UnityEditor.Handles.color = ringMaxColor;
        UnityEditor.Handles.DrawWireDisc(center, Vector3.up, maxSpawnDistance);

        // Draw world bounds if assigned
        if (worldBounds)
        {
            Gizmos.color = Color.yellow * 0.6f;
            Gizmos.matrix = worldBounds.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
#endif
}
