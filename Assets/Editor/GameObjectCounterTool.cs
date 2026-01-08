using UnityEngine;
using UnityEditor;

public class GameObjectCounterTool : EditorWindow
{
    private GameObject root;
    private bool countInactive = true;
    private int result;

    [MenuItem("Tools/Debug/GameObject Counter")]
    static void Open()
    {
        GetWindow<GameObjectCounterTool>("GameObject Counter");
    }

    void OnGUI()
    {
        GUILayout.Label("Hierarchy GameObject Counter", EditorStyles.boldLabel);
        GUILayout.Space(6);

        root = (GameObject)EditorGUILayout.ObjectField(
            "Root GameObject",
            root,
            typeof(GameObject),
            true
        );

        countInactive = EditorGUILayout.Toggle("Include Inactive", countInactive);

        GUILayout.Space(8);

        EditorGUI.BeginDisabledGroup(root == null);
        if (GUILayout.Button("Count GameObjects", GUILayout.Height(30)))
        {
            Count();
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(8);

        EditorGUILayout.LabelField("Total Count:", result.ToString(), EditorStyles.boldLabel);
    }

    void Count()
    {
        if (root == null)
        {
            result = 0;
            return;
        }

        Transform[] all = root.GetComponentsInChildren<Transform>(countInactive);
        result = all.Length;
    }
}
