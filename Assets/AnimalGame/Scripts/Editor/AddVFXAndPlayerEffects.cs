using UnityEngine;
using UnityEditor;

public class AddVFXAndPlayerEffects : EditorWindow
{
    private GameObject vfxPrefab;

    [MenuItem("Tools/Add VFX + PlayerEffects to Selected")]
    public static void ShowWindow()
    {
        GetWindow<AddVFXAndPlayerEffects>("Add VFX & PlayerEffects");
    }

    void OnGUI()
    {
        GUILayout.Label("Setup Parameters", EditorStyles.boldLabel);
        vfxPrefab = (GameObject)EditorGUILayout.ObjectField("Blood VFX Prefab", vfxPrefab, typeof(GameObject), false);

        if (GUILayout.Button("Apply to Selected"))
        {
            if (vfxPrefab == null)
            {
                EditorUtility.DisplayDialog("Missing Prefab", "Please assign the blood VFX prefab.", "OK");
                return;
            }

            ApplyToSelectedObjects(vfxPrefab);
        }
    }

    void ApplyToSelectedObjects(GameObject vfxPrefab)
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            // Check if object is a prefab or scene object
            GameObject instanceRoot = PrefabUtility.IsPartOfPrefabInstance(obj)
                ? PrefabUtility.GetOutermostPrefabInstanceRoot(obj)
                : obj;

            Undo.RegisterFullObjectHierarchyUndo(instanceRoot, "Add VFX and PlayerEffects");

            // 1. Add PlayerEffects script if not already
            if (instanceRoot.GetComponent<PlayerEffects>() == null)
            {
                instanceRoot.AddComponent<PlayerEffects>();
                Debug.Log($"Added PlayerEffects to {instanceRoot.name}");
            }

            // 2. Add VFX prefab as child (if not already added)
            bool alreadyHasVFX = false;
            foreach (Transform child in instanceRoot.transform)
            {
                if (PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == vfxPrefab)
                {
                    alreadyHasVFX = true;
                    break;
                }
            }

            if (!alreadyHasVFX)
            {
                GameObject vfxInstance = (GameObject)PrefabUtility.InstantiatePrefab(vfxPrefab, instanceRoot.transform);
                vfxInstance.transform.localPosition = Vector3.zero;
                Debug.Log($"Added VFX prefab to {instanceRoot.name}");
            }

            EditorUtility.SetDirty(instanceRoot);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Done", "Applied VFX and PlayerEffects to selected objects.", "OK");
    }
}
