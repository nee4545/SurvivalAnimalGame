using System.Collections.Generic;
using UnityEngine;

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

    // ——— Gizmos ———
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
}
