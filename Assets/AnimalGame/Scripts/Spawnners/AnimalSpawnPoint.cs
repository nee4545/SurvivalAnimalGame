using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AnimalSpawnPoint : MonoBehaviour
{
    public enum SpawnShape { Circle, Box }
    public enum FaceMode { UseSpawner, Random, FaceCenter, KeepPrefab }

    [System.Serializable]
    public struct WeightedPrefab
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float weight; // relative; doesn’t need to sum to 1
    }

    [Header("Prefabs for THIS point (habitat-specific)")]
    public List<WeightedPrefab> prefabs = new();

    [Header("Local Area (optional jitter)")]
    public SpawnShape shape = SpawnShape.Circle;
    [Tooltip("For Circle: X or Z used as radius. For Box: half-extents.")]
    public Vector3 size = new Vector3(2f, 0f, 2f);

    [Header("Facing (optional override)")]
    public FaceMode faceMode = FaceMode.UseSpawner;

    [Header("Player Separation (optional override)")]
    [Tooltip("If >= 0, overrides the spawner’s minDistanceFromPlayer for this point.")]
    public float minDistanceFromPlayer = -1f;

    // ── NEW: self-spawning options ──────────────────────────────────────────────
    [Header("Self-Spawning (optional)")]
    [Tooltip("If ON, this point will spawn & maintain its own animals.")]
    public bool selfSpawnEnabled = false;

    [Tooltip("Max animals this specific point wants to keep alive.")]
    public int pointMaxAlive = 4;

    [Tooltip("Seconds between spawn attempts while under the cap.")]
    public float pointSpawnInterval = 3.0f;

    [Tooltip("Pre-warm pool for each prefab used here.")]
    public int pointWarmupCount = 4;

    [Tooltip("If assigned, use this as the parent for spawned instances.")]
    public Transform spawnParentOverride;

    [Tooltip("Inherit nav/player/facing settings from the nearest AnimalSpawner (recommended).")]
    public bool inheritFromParentSpawner = true;

    [Tooltip("If ON and a parent AnimalSpawner exists with a global aggressive cap, respect it.")]
    public bool respectAggressiveCapFromParent = true;

    // ── runtime ────────────────────────────────────────────────────────────────
    Transform _player;
    AnimalSpawner _parent;
    readonly List<GameObject> _alive = new();
    float _timer;

    // ——— Gizmos (unchanged) ———
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.25f, 1f, 0.5f, 0.35f);
        if (shape == SpawnShape.Circle)
        {
            float r = Mathf.Max(size.x, size.z);
            const int segs = 36;
            Vector3 c = transform.position;
            Vector3 prev = c + transform.right * r;
            for (int i = 1; i <= segs; i++)
            {
                float ang = (i / (float)segs) * Mathf.PI * 2f;
                Vector3 next = c + (new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * r);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
        else
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, size * 2f);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }

    void Awake()
    {
        // find parent spawner if any
        if (inheritFromParentSpawner)
            _parent = GetComponentInParent<AnimalSpawner>();

        // player by tag (prefer parent’s tag if set)
        string tag = (_parent && !string.IsNullOrEmpty(_parent.playerTag)) ? _parent.playerTag : "Player";
        var go = GameObject.FindGameObjectWithTag(tag);
        _player = go ? go.transform : null;

        // default timer
        _timer = pointSpawnInterval;

        // pool warmup
        HashSet<GameObject> warmed = new();
        foreach (var w in prefabs)
        {
            if (!w.prefab || warmed.Contains(w.prefab)) continue;
            PoolManager.Warmup(w.prefab, pointWarmupCount);
            warmed.Add(w.prefab);
        }

        // ensure we have a parent if none set and no parent spawner
        if (!spawnParentOverride && !_parent)
        {
            var holder = new GameObject($"{name}_PointSpawns");
            holder.transform.SetParent(transform);
            spawnParentOverride = holder.transform;
        }
    }

    void Update()
    {
        if (!selfSpawnEnabled) return;

        // lazy re-find player
        if (!_player)
        {
            string tag = (_parent && !string.IsNullOrEmpty(_parent.playerTag)) ? _parent.playerTag : "Player";
            var go = GameObject.FindGameObjectWithTag(tag);
            _player = go ? go.transform : null;
        }

        // compact list (pooled or dead)
        _alive.RemoveAll(a => !a || !a.activeInHierarchy);

        if (_alive.Count >= Mathf.Max(0, pointMaxAlive)) return;

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = pointSpawnInterval;

        // try one spawn per tick until we reach cap
        TrySpawnOne();
    }

    void TrySpawnOne()
    {
        // pick prefab (respect parent aggressive cap if asked)
        GameObject prefab = respectAggressiveCapFromParent
            ? PickWeightedPrefabRespectingParentCap(prefabs)
            : PickWeightedPrefab(prefabs);

        if (!prefab) return;

        // pick position
        if (!TryGetSpawnPosition(out Vector3 pos)) return;

        // facing
        Quaternion rot = GetFacing(pos);

        // parent to parent spawner’s spawnParent if available → else our override
        Transform parentT = spawnParentOverride;
        if (_parent && _parent.spawnParent) parentT = _parent.spawnParent;

        var go = PoolManager.Spawn(prefab, pos, rot, parentT);
        if (go) _alive.Add(go);
    }

    // ── helpers ────────────────────────────────────────────────────────────────
    GameObject PickWeightedPrefab(List<WeightedPrefab> list)
    {
        float total = 0f;
        for (int i = 0; i < list.Count; i++) total += Mathf.Max(0f, list[i].weight);
        if (total <= 0f) return null;

        float r = Random.value * total, cum = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            cum += Mathf.Max(0f, list[i].weight);
            if (r <= cum) return list[i].prefab;
        }
        return list[^1].prefab;
    }

    GameObject PickWeightedPrefabRespectingParentCap(List<WeightedPrefab> list)
    {
        if (!_parent || !_parent.limitAggressive) return PickWeightedPrefab(list);

        int currAgg = CountAggressiveAliveSceneWide();
        if (currAgg < Mathf.Max(0, _parent.maxAggressiveAlive)) return PickWeightedPrefab(list);

        // cap reached → pick only non-aggressive from this list
        float total = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            var w = list[i];
            if (w.prefab && !_parent.IsAggressivePrefab(w.prefab))
                total += Mathf.Max(0f, w.weight);
        }
        if (total <= 0f) return null;

        float r = Random.value * total, cum = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            var w = list[i];
            if (!w.prefab || _parent.IsAggressivePrefab(w.prefab)) continue;
            cum += Mathf.Max(0f, w.weight);
            if (r <= cum) return w.prefab;
        }
        return null;
    }

    int CountAggressiveAliveSceneWide()
    {
        var all = GameObject.FindObjectsOfType<CuteAnimalAI>();
        int count = 0;
        for (int i = 0; i < all.Length; i++)
        {
            var ai = all[i]; if (!ai) continue;
            bool isAgg = ai.aiType == CuteAnimalAI.AIType.Aggressive
                      || ai.aiType == CuteAnimalAI.AIType.AggressiveType1
                      || ai.aiType == CuteAnimalAI.AIType.AggressiveType2
                      || ai.aiType == CuteAnimalAI.AIType.AggressiveType3
                      || ai.aiType == CuteAnimalAI.AIType.AggressiveJumping;
            if (!isAgg) continue;
            var h = ai.GetComponent<Health>();
            if (h && h.IsDead) continue;
            count++;
        }
        return count;
    }

    bool TryGetSpawnPosition(out Vector3 pos)
    {
        pos = transform.position;

        // roll local offset inside this point’s shape/size
        Vector3 local;
        if (shape == SpawnShape.Circle)
        {
            float r = Mathf.Max(size.x, size.z);
            var flat = Random.insideUnitCircle * r;
            local = new Vector3(flat.x, 0f, flat.y);
        }
        else
        {
            local = new Vector3(
                Random.Range(-size.x, size.x),
                Random.Range(-size.y, size.y),
                Random.Range(-size.z, size.z));
        }

        pos = transform.TransformPoint(local);

        // min player distance (point override → parent → 0)
        float minDist = (minDistanceFromPlayer >= 0f) ? minDistanceFromPlayer
                       : (_parent ? _parent.minDistanceFromPlayer : 0f);

        // a few retries if too close / off-mesh
        const int retries = 6;
        for (int i = 0; i <= retries; i++)
        {
            if (_player && minDist > 0f && Vector3.Distance(pos, _player.position) < minDist)
            {
                // re-roll
                if (shape == SpawnShape.Circle)
                {
                    float r = Mathf.Max(size.x, size.z);
                    var flat = Random.insideUnitCircle * r;
                    local = new Vector3(flat.x, 0f, flat.y);
                }
                else
                {
                    local = new Vector3(
                        Random.Range(-size.x, size.x),
                        Random.Range(-size.y, size.y),
                        Random.Range(-size.z, size.z));
                }
                pos = transform.TransformPoint(local);
                continue;
            }

            bool placeOnNav = _parent ? _parent.placeOnNavMesh : true;
            float nmRadius = _parent ? _parent.navmeshSearchRadius : 5f;

            if (placeOnNav)
            {
                if (NavMesh.SamplePosition(pos, out var hit, nmRadius, NavMesh.AllAreas))
                {
                    pos = hit.position;
                    return true;
                }
                else
                {
                    if (i < retries)
                    {
                        // try again
                        if (shape == SpawnShape.Circle)
                        {
                            float r = Mathf.Max(size.x, size.z);
                            var flat = Random.insideUnitCircle * r;
                            local = new Vector3(flat.x, 0f, flat.y);
                        }
                        else
                        {
                            local = new Vector3(
                                Random.Range(-size.x, size.x),
                                Random.Range(-size.y, size.y),
                                Random.Range(-size.z, size.z));
                        }
                        pos = transform.TransformPoint(local);
                        continue;
                    }
                    return false;
                }
            }

            // no navmesh placement required
            return true;
        }
        return false;
    }

    Quaternion GetFacing(Vector3 pos)
    {
        FaceMode mode = faceMode;
        if (mode == FaceMode.UseSpawner)
        {
            // pull default from parent spawner if present, else Random
            var fallback = _parent ? _parent.defaultFaceMode : AnimalSpawner.FaceMode.Random;
            switch (fallback)
            {
                case AnimalSpawner.FaceMode.FaceCenter:
                    Vector3 dir = (transform.position - pos); dir.y = 0f;
                    if (dir.sqrMagnitude < 0.001f) dir = Random.insideUnitSphere;
                    return Quaternion.LookRotation(dir.normalized);
                case AnimalSpawner.FaceMode.KeepPrefab:
                    return transform.rotation;
                default:
                    return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
        }
        else
        {
            switch (mode)
            {
                case FaceMode.FaceCenter:
                    Vector3 dir = (transform.position - pos); dir.y = 0f;
                    if (dir.sqrMagnitude < 0.001f) dir = Random.insideUnitSphere;
                    return Quaternion.LookRotation(dir.normalized);
                case FaceMode.KeepPrefab:
                    return transform.rotation;
                default:
                    return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
        }
    }
}
