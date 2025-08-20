using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class PoolManager
{
    private class Pool
    {
        public readonly Stack<GameObject> stack = new();
        public readonly GameObject prefab;
        public readonly Transform root;
        public Pool(GameObject prefab, Transform root) { this.prefab = prefab; this.root = root; }
    }

    private static readonly Dictionary<int, Pool> pools = new();
    private static Transform poolRoot;
    private static Transform Root
    {
        get
        {
            if (!poolRoot)
            {
                var go = new GameObject("~PoolRoot");
                Object.DontDestroyOnLoad(go);
                go.SetActive(false);
                poolRoot = go.transform;
            }
            return poolRoot;
        }
    }

    public static void Warmup(GameObject prefab, int count)
    {
        var pool = GetOrCreatePool(prefab);
        for (int i = 0; i < count; i++)
        {
            var go = CreateInstance(prefab, pool.root);
            go.SetActive(false);
            pool.stack.Push(go);
        }
    }

    public static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        var pool = GetOrCreatePool(prefab);
        GameObject go = pool.stack.Count > 0 ? pool.stack.Pop() : CreateInstance(prefab, parent);

        if (go.transform.parent != parent) go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);

        if (go.TryGetComponent<NavMeshAgent>(out var agent))
        {
            if (!agent.enabled) agent.enabled = true;
            if (!agent.isOnNavMesh && NavMesh.SamplePosition(pos, out var hit, 2f, NavMesh.AllAreas))
                agent.Warp(hit.position);
            agent.isStopped = false;
            agent.ResetPath();
        }

        if (go.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (var p in go.GetComponents<IPoolable>()) p.OnSpawned();
        return go;
    }

    public static void Despawn(GameObject go)
    {
        if (!go) return;
        foreach (var p in go.GetComponents<IPoolable>()) p.OnDespawned();

        go.SendMessage("StopAllCoroutines", SendMessageOptions.DontRequireReceiver);

        if (go.TryGetComponent<NavMeshAgent>(out var agent))
        {
                if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.ResetPath();
                    agent.velocity = Vector3.zero;
                    agent.isStopped = true;
                }
        }

        go.transform.SetParent(Root, false);
        go.SetActive(false);

        var po = go.GetComponent<PooledObject>();
        int key = po && po.prefab ? po.prefab.GetInstanceID() : go.GetInstanceID();
        if (pools.TryGetValue(key, out var pool)) pool.stack.Push(go);
        else Object.Destroy(go); // shouldn’t happen
    }

    private static Pool GetOrCreatePool(GameObject prefab)
    {
        int key = prefab.GetInstanceID();
        if (!pools.TryGetValue(key, out var pool))
        {
            var root = new GameObject($"~Pool_{prefab.name}").transform;
            root.SetParent(Root, false);
            root.gameObject.SetActive(false);
            pool = new Pool(prefab, root);
            pools.Add(key, pool);
        }
        return pool;
    }

    private static GameObject CreateInstance(GameObject prefab, Transform parent)
    {
        var go = Object.Instantiate(prefab, parent);
        go.name = prefab.name;
        var po = go.GetComponent<PooledObject>() ?? go.AddComponent<PooledObject>();
        po.prefab = prefab;
        return go;
    }
}
