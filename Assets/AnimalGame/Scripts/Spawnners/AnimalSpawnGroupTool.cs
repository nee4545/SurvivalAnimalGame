using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimalSpawnGroupTool : MonoBehaviour
{
    [Header("Group Spawn Settings")]
    [Tooltip("Where spawned animals from this group should be placed in the hierarchy.")]
    public Transform spawnedAnimalsParent;

    [Tooltip("Include inactive child spawn points.")]
    public bool includeInactiveSpawnPoints = true;

    [Tooltip("If no spawned parent is assigned, create one automatically under this object.")]
    public bool autoCreateSpawnedParent = true;

    [ContextMenu("Spawn All Points To Max")]
    public void SpawnAllPointsToMax()
    {
        EnsureSpawnedParent();

        AnimalSpawnPoint[] points = GetComponentsInChildren<AnimalSpawnPoint>(includeInactiveSpawnPoints);

        foreach (AnimalSpawnPoint point in points)
        {
            if (!point) continue;
            point.SpawnToPointMaxForLevelDesign(spawnedAnimalsParent);
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Clear Spawned Animals")]
    public void ClearSpawnedAnimals()
    {
        if (!spawnedAnimalsParent) return;

        for (int i = spawnedAnimalsParent.childCount - 1; i >= 0; i--)
        {
            Transform child = spawnedAnimalsParent.GetChild(i);

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(child.gameObject);
#else
                DestroyImmediate(child.gameObject);
#endif
            }
        }
    }

    void EnsureSpawnedParent()
    {
        if (spawnedAnimalsParent || !autoCreateSpawnedParent) return;

        GameObject holder = new GameObject($"{name}_SpawnedAnimals");
        holder.transform.SetParent(transform);
        holder.transform.localPosition = Vector3.zero;
        holder.transform.localRotation = Quaternion.identity;
        holder.transform.localScale = Vector3.one;

        spawnedAnimalsParent = holder.transform;

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(holder, "Create Spawned Animals Parent");
        EditorUtility.SetDirty(this);
#endif
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AnimalSpawnGroupTool))]
public class AnimalSpawnGroupToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AnimalSpawnGroupTool tool = (AnimalSpawnGroupTool)target;

        GUILayout.Space(12);

        if (GUILayout.Button("Spawn All Points To Max", GUILayout.Height(34)))
        {
            tool.SpawnAllPointsToMax();
        }

        if (GUILayout.Button("Clear Spawned Animals", GUILayout.Height(28)))
        {
            tool.ClearSpawnedAnimals();
        }
    }
}
#endif