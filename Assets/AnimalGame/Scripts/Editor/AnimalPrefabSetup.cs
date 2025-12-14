#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class AnimalPrefabSetup
{
    [MenuItem("Tools/Cute Animals/Setup Selected As Animal AI %#a")]
    private static void SetupSelected()
    {
        var selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("[AnimalPrefabSetup] No GameObjects selected.");
            return;
        }

        foreach (var go in selected)
        {
            SetupGameObject(go);
        }
    }

    private static void SetupGameObject(GameObject go)
    {
        Undo.RegisterFullObjectHierarchyUndo(go, "Setup Animal AI");

        // Core components
        EnsureComponent<Animator>(go);
        EnsureComponent<Health>(go);
        EnsureComponent<CuteAnimalAnimHandler>(go);
        EnsureComponent<NavMeshAgent>(go);
        EnsureComponent<CuteAnimalAI>(go);
        EnsureComponent<CapsuleCollider>(go);
        EnsureComponent<PooledObject>(go);
        EnsureComponent<EnemyPoolReset>(go);
        EnsureComponent<LootDropper>(go);

        // Tag & Layer (if they exist)
        TrySetTag(go, "Animal");
        TrySetLayer(go, "Enemy");

        EditorUtility.SetDirty(go);
        Debug.Log($"[AnimalPrefabSetup] '{go.name}' wired as Animal AI.");
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var comp = go.GetComponent<T>();
        if (comp == null)
        {
            comp = go.AddComponent<T>();
        }
        return comp;
    }

    private static void TrySetTag(GameObject go, string tagName)
    {
        if (IsTagDefined(tagName))
        {
            go.tag = tagName;
        }
        else
        {
            Debug.LogWarning($"[AnimalPrefabSetup] Tag '{tagName}' is not defined in Tag Manager.");
        }
    }

    private static bool IsTagDefined(string tagName)
    {
        try
        {
            var temp = new GameObject();
            temp.tag = tagName;
            Object.DestroyImmediate(temp);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void TrySetLayer(GameObject go, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer != -1)
        {
            go.layer = layer;
        }
        else
        {
            Debug.LogWarning($"[AnimalPrefabSetup] Layer '{layerName}' not found. Set it manually if needed.");
        }
    }
}
#endif
