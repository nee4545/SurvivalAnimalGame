using UnityEditor;
using UnityEngine;

public class RendererOptimizationTool : EditorWindow
{
    private Transform root;
    private bool includeInactive = true;

    [MenuItem("Tools/Optimization/Disable Shadows & GI")]
    static void Open()
    {
        GetWindow<RendererOptimizationTool>("Renderer Optimizer");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Renderer Optimization Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        root = (Transform)EditorGUILayout.ObjectField(
            "Root Object",
            root,
            typeof(Transform),
            true
        );

        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);

        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(root == null))
        {
            if (GUILayout.Button("Apply to All Child Renderers", GUILayout.Height(40)))
            {
                Apply();
            }
        }

        EditorGUILayout.HelpBox(
            "Disables:\n" +
            "- Cast Shadows\n" +
            "- Receive Shadows\n" +
            "- Contribute Global Illumination\n\n" +
            "Uses SerializedObject for version safety.",
            MessageType.Info
        );
    }

    void Apply()
    {
        if (!root)
            return;

        var renderers = root.GetComponentsInChildren<MeshRenderer>(includeInactive);
        int count = 0;

        Undo.RecordObjects(renderers, "Disable Shadows & GI");

        foreach (var r in renderers)
        {
            if (!r) continue;

            // --- Shadows ---
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            // --- Contribute GI (serialized, version-safe) ---
            SerializedObject so = new SerializedObject(r);
            SerializedProperty contributeGI = so.FindProperty("m_ContributeGI");
            if (contributeGI != null)
            {
                contributeGI.boolValue = false;
            }
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(r);
            count++;
        }

        Debug.Log($"RendererOptimizationTool: Optimized {count} MeshRenderers.");
    }
}
