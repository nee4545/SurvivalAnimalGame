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



    // ——— runtime ———
    float timer;
    Transform player;
    int rrIndex = 0; // round-robin
    readonly HashSet<GameObject> warmed = new();

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
        if (!TryGetPointSpawnPosition(point, out Vector3 pos)) return;

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
    bool IsAggressivePrefab(GameObject prefab)
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

        // Try a few retries if too close to player or off-mesh
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

            if (placeOnNavMesh)
            {
                if (NavMesh.SamplePosition(pos, out var hit, navmeshSearchRadius, NavMesh.AllAreas))
                {
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
}
