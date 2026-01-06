using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;

public class GrassPainterTool : EditorWindow
{
    [Header("Brush Settings")]
    public GameObject grassPrefab;
    public Transform parent;
    public float brushRadius = 2f;
    public int density = 5;

    [Header("Placement")]
    public LayerMask groundLayer = ~0;
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    public bool alignToNormal = true;

    private bool painting;
    private Vector3 lastPaintPos;

    [MenuItem("Tools/Level Design/Grass Painter")]
    static void Open()
    {
        GetWindow<GrassPainterTool>("Grass Painter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Grass Painting Tool", EditorStyles.boldLabel);

        grassPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Grass Prefab",
            grassPrefab,
            typeof(GameObject),
            false
        );

        parent = (Transform)EditorGUILayout.ObjectField(
            "Parent Object",
            parent,
            typeof(Transform),
            true
        );

        brushRadius = EditorGUILayout.Slider("Brush Radius", brushRadius, 0.5f, 10f);
        density = EditorGUILayout.IntSlider("Density", density, 1, 20);

        groundLayer = LayerMaskField("Ground Layer", groundLayer);
        scaleRange = EditorGUILayout.Vector2Field("Scale Range", scaleRange);
        alignToNormal = EditorGUILayout.Toggle("Align To Ground", alignToNormal);

        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Hold LEFT MOUSE in Scene View to paint grass.\n" +
            "Use the brush circle to cover areas quickly.",
            MessageType.Info
        );
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            // Draw brush gizmo
            Handles.color = new Color(0, 1, 0, 0.25f);
            Handles.DrawSolidDisc(hit.point, hit.normal, brushRadius);
            Handles.color = Color.green;
            Handles.DrawWireDisc(hit.point, hit.normal, brushRadius);

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                painting = true;
                lastPaintPos = hit.point;
                Paint(hit);
                e.Use();
            }

            if (e.type == EventType.MouseDrag && painting)
            {
                if (Vector3.Distance(hit.point, lastPaintPos) > brushRadius * 0.3f)
                {
                    Paint(hit);
                    lastPaintPos = hit.point;
                }
                e.Use();
            }

            if (e.type == EventType.MouseUp)
            {
                painting = false;
            }
        }

        sceneView.Repaint();
    }

    void Paint(RaycastHit hit)
    {
        if (!grassPrefab)
        {
            Debug.LogWarning("Grass Painter: No grass prefab assigned.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        for (int i = 0; i < density; i++)
        {
            Vector2 random = Random.insideUnitCircle * brushRadius;
            Vector3 origin = hit.point + hit.normal * 0.5f +
                             hit.transform.right * random.x +
                             hit.transform.forward * random.y;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit groundHit, 5f, groundLayer))
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab);
                Undo.RegisterCreatedObjectUndo(instance, "Paint Grass");

                instance.transform.position = groundHit.point;

                if (alignToNormal)
                    instance.transform.rotation = Quaternion.FromToRotation(Vector3.up, groundHit.normal);
                else
                    instance.transform.rotation = Quaternion.identity;

                instance.transform.Rotate(Vector3.up, Random.Range(0f, 360f), Space.Self);

                float scale = Random.Range(scaleRange.x, scaleRange.y);
                instance.transform.localScale = Vector3.one * scale;

                if (parent)
                    instance.transform.SetParent(parent);
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
    }

    static LayerMask LayerMaskField(string label, LayerMask selected)
    {
        var layers = InternalEditorUtility.layers;
        var layerNumbers = layers.Select(LayerMask.NameToLayer).ToArray();

        int maskWithoutEmpty = 0;
        for (int i = 0; i < layerNumbers.Length; i++)
        {
            if (((1 << layerNumbers[i]) & selected.value) != 0)
                maskWithoutEmpty |= (1 << i);
        }

        maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layers);

        int mask = 0;
        for (int i = 0; i < layerNumbers.Length; i++)
        {
            if ((maskWithoutEmpty & (1 << i)) != 0)
                mask |= (1 << layerNumbers[i]);
        }

        selected.value = mask;
        return selected;
    }
}
