using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;

public class GrassPainterTool : EditorWindow
{
    public enum PaintMode
    {
        Fill,
        Ring
    }

    [Header("Prefabs")]
    public List<GameObject> grassPrefabs = new List<GameObject>();
    public Transform parent;

    [Header("Brush")]
    public float brushRadius = 2f;
    public int density = 5;
    public PaintMode paintMode = PaintMode.Fill;
    public float ringThickness = 0.4f;

    [Header("Placement")]
    public LayerMask groundLayer = ~0;
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    public bool alignToNormal = true;

    [Header("Offsets")]
    [Tooltip("Vertical offset applied after ground snap")]
    public float yOffset = 0.02f;

    private bool painting;
    private Vector3 lastPaintPos;

    [MenuItem("Tools/Level Design/Grass Painter")]
    static void Open()
    {
        GetWindow<GrassPainterTool>("Grass Painter");
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    void OnGUI()
    {
        GUILayout.Label("Grass Painter Tool", EditorStyles.boldLabel);

        GUILayout.Space(4);
        GUILayout.Label("Paint Prefabs", EditorStyles.boldLabel);
        DrawPrefabList();

        parent = (Transform)EditorGUILayout.ObjectField(
            "Parent Object",
            parent,
            typeof(Transform),
            true
        );

        GUILayout.Space(8);
        GUILayout.Label("Brush Settings", EditorStyles.boldLabel);

        brushRadius = EditorGUILayout.Slider("Brush Radius", brushRadius, 0.5f, 15f);
        density = EditorGUILayout.IntSlider("Density", density, 1, 30);

        paintMode = (PaintMode)EditorGUILayout.EnumPopup("Paint Mode", paintMode);

        if (paintMode == PaintMode.Ring)
        {
            ringThickness = EditorGUILayout.Slider(
                "Ring Thickness",
                ringThickness,
                0.1f,
                brushRadius
            );
        }

        GUILayout.Space(8);
        GUILayout.Label("Placement Settings", EditorStyles.boldLabel);

        groundLayer = LayerMaskField("Ground Layer", groundLayer);
        scaleRange = EditorGUILayout.Vector2Field("Scale Range", scaleRange);
        yOffset = EditorGUILayout.FloatField("Y Offset", yOffset);
        alignToNormal = EditorGUILayout.Toggle("Align To Ground", alignToNormal);

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "LEFT MOUSE: Paint in Scene View\n" +
            "Fill = paint inside brush\n" +
            "Ring = paint perimeter only\n" +
            "Y Offset prevents thin meshes from sinking",
            MessageType.Info
        );
    }

    void DrawPrefabList()
    {
        int removeIndex = -1;

        for (int i = 0; i < grassPrefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            grassPrefabs[i] = (GameObject)EditorGUILayout.ObjectField(
                grassPrefabs[i],
                typeof(GameObject),
                false
            );

            if (GUILayout.Button("X", GUILayout.Width(22)))
                removeIndex = i;

            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex >= 0)
            grassPrefabs.RemoveAt(removeIndex);

        if (GUILayout.Button("+ Add Prefab"))
            grassPrefabs.Add(null);
    }

    void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            // Brush gizmo
            Handles.color = new Color(0, 1, 0, 0.2f);
            Handles.DrawSolidDisc(hit.point, hit.normal, brushRadius);

            Handles.color = Color.green;
            Handles.DrawWireDisc(hit.point, hit.normal, brushRadius);

            if (paintMode == PaintMode.Ring)
            {
                Handles.color = Color.yellow;
                Handles.DrawWireDisc(
                    hit.point,
                    hit.normal,
                    Mathf.Max(0.01f, brushRadius - ringThickness)
                );
            }

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
                painting = false;
        }

        sceneView.Repaint();
    }

    void Paint(RaycastHit hit)
    {
        if (grassPrefabs.Count == 0 || grassPrefabs.All(p => p == null))
            return;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        for (int i = 0; i < density; i++)
        {
            Vector2 random;

            if (paintMode == PaintMode.Fill)
            {
                random = Random.insideUnitCircle * brushRadius;
            }
            else
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float innerRadius = Mathf.Max(0f, brushRadius - ringThickness);
                float radius = Random.Range(innerRadius, brushRadius);

                random = new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                ) * radius;
            }

            Vector3 origin =
                hit.point +
                hit.normal * 0.5f +
                hit.transform.right * random.x +
                hit.transform.forward * random.y;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit groundHit, 5f, groundLayer))
            {
                GameObject prefab = grassPrefabs[Random.Range(0, grassPrefabs.Count)];
                if (!prefab) continue;

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(instance, "Paint Grass");

                Vector3 pos = groundHit.point;
                pos.y += yOffset;
                instance.transform.position = pos;

                if (alignToNormal)
                    instance.transform.rotation =
                        Quaternion.FromToRotation(Vector3.up, groundHit.normal);
                else
                    instance.transform.rotation = Quaternion.identity;

                instance.transform.Rotate(Vector3.up, Random.Range(0f, 360f));

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
