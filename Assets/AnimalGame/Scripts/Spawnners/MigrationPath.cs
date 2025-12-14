using System;
using System.Collections.Generic;
using UnityEngine;

public class MigrationPath : MonoBehaviour
{
    [Serializable]
    public class Node
    {
        public Transform point;
        [Min(0f)] public float restSeconds = 2f;
    }

    [Header("Path Nodes (in order)")]
    public List<Node> nodes = new();

    [Header("Looping")]
    public bool loop = true;
    public bool pingPong = false;

    public int Count => nodes != null ? nodes.Count : 0;

    public bool TryGetNode(int index, out Node node)
    {
        node = null;
        if (nodes == null || nodes.Count == 0) return false;
        if (index < 0 || index >= nodes.Count) return false;
        node = nodes[index];
        return node != null && node.point != null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (nodes == null || nodes.Count < 2) return;
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.8f);

        for (int i = 0; i < nodes.Count - 1; i++)
        {
            var a = nodes[i]?.point;
            var b = nodes[i + 1]?.point;
            if (!a || !b) continue;
            Gizmos.DrawLine(a.position, b.position);
        }

        // loop line
        if (loop && !pingPong)
        {
            var a = nodes[^1]?.point;
            var b = nodes[0]?.point;
            if (a && b) Gizmos.DrawLine(a.position, b.position);
        }
    }
#endif
}
