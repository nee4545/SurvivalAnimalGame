using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GroundAlignTool : EditorWindow
{
    private enum GroundHitMode
    {
        PhysicsColliders,
        SceneMeshRenderers
    }

    private const string PrefAutoAlign = "GroundAlignTool_AutoAlign";
    private const string PrefAlignRotation = "GroundAlignTool_AlignRotation";
    private const string PrefUseBoundsBottom = "GroundAlignTool_UseBoundsBottom";
    private const string PrefExtraOffset = "GroundAlignTool_ExtraOffset";
    private const string PrefRaycastHeight = "GroundAlignTool_RaycastHeight";
    private const string PrefRaycastDistance = "GroundAlignTool_RaycastDistance";
    private const string PrefLayerMask = "GroundAlignTool_LayerMask";
    private const string PrefGroundHitMode = "GroundAlignTool_GroundHitMode";

    private static bool autoAlign;
    private static bool alignRotationToGround;
    private static bool useBoundsBottom;
    private static float extraYOffset;
    private static float raycastHeight;
    private static float raycastDistance;
    private static LayerMask groundLayerMask;
    private static GroundHitMode groundHitMode = GroundHitMode.SceneMeshRenderers;

    private static readonly Dictionary<int, Vector3> lastPositions =
        new Dictionary<int, Vector3>();

    private static bool isLoaded;

    [MenuItem("Tools/Level Design/Ground Align Tool")]
    public static void OpenWindow()
    {
        GroundAlignTool window = GetWindow<GroundAlignTool>("Ground Align");
        window.minSize = new Vector2(340f, 280f);
        window.Show();
    }

    [InitializeOnLoadMethod]
    private static void InitializeOnLoad()
    {
        if (isLoaded)
            return;

        isLoaded = true;

        LoadPrefs();

        EditorApplication.update -= EditorUpdate;
        EditorApplication.update += EditorUpdate;
    }

    private void OnEnable()
    {
        LoadPrefs();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Ground Align Tool", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Auto-align selected objects to ground while moving them in Scene view. Use Scene Mesh Renderers if your ground/rocks do not have colliders.",
            MessageType.Info
        );

        EditorGUI.BeginChangeCheck();

        autoAlign = EditorGUILayout.Toggle("Auto Align Selected", autoAlign);

        groundHitMode = (GroundHitMode)EditorGUILayout.EnumPopup(
            "Ground Hit Mode",
            groundHitMode
        );

        if (groundHitMode == GroundHitMode.PhysicsColliders)
        {
            groundLayerMask = LayerMaskField("Ground Layer Mask", groundLayerMask);
        }

        EditorGUILayout.Space(6);

        useBoundsBottom = EditorGUILayout.Toggle(
            new GUIContent(
                "Use Bounds Bottom",
                "ON = object's bottom sits on ground. OFF = object's pivot sits on ground."
            ),
            useBoundsBottom
        );

        alignRotationToGround = EditorGUILayout.Toggle(
            new GUIContent(
                "Align Rotation To Ground",
                "Aligns object's up direction to the mesh/ground normal."
            ),
            alignRotationToGround
        );

        extraYOffset = EditorGUILayout.FloatField(
            new GUIContent("Extra Y Offset", "Additional height above the snapped surface."),
            extraYOffset
        );

        raycastHeight = EditorGUILayout.FloatField(
            new GUIContent("Raycast Start Height", "How far above the object the ray starts."),
            raycastHeight
        );

        raycastDistance = EditorGUILayout.FloatField(
            new GUIContent("Raycast Distance", "How far downward the ray checks."),
            raycastDistance
        );

        if (EditorGUI.EndChangeCheck())
        {
            SavePrefs();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Snap Selected To Ground Now", GUILayout.Height(32)))
        {
            SnapSelectedToGround(true);
        }

        EditorGUILayout.Space(6);

        if (GUILayout.Button("Clear Cached Positions"))
        {
            lastPositions.Clear();
        }
    }

    private static void EditorUpdate()
    {
        if (!autoAlign)
            return;

        if (EditorApplication.isPlaying)
            return;

        if (Selection.transforms == null || Selection.transforms.Length == 0)
            return;

        SnapSelectedToGround(false);
    }

    private static void SnapSelectedToGround(bool useUndo)
    {
        Transform[] selectedTransforms = Selection.transforms;

        if (selectedTransforms == null || selectedTransforms.Length == 0)
            return;

        for (int i = 0; i < selectedTransforms.Length; i++)
        {
            Transform selected = selectedTransforms[i];

            if (selected == null)
                continue;

            if (PrefabUtility.IsPartOfPrefabAsset(selected.gameObject))
                continue;

            if (!ShouldProcessTransform(selected, useUndo))
                continue;

            AlignTransformToGround(selected, selectedTransforms, useUndo);
        }

        SceneView.RepaintAll();
    }

    private static bool ShouldProcessTransform(Transform target, bool force)
    {
        if (force)
            return true;

        int id = target.GetInstanceID();
        Vector3 currentPosition = target.position;

        if (!lastPositions.TryGetValue(id, out Vector3 lastPosition))
        {
            lastPositions[id] = currentPosition;
            return true;
        }

        if ((currentPosition - lastPosition).sqrMagnitude < 0.0001f)
            return false;

        lastPositions[id] = currentPosition;
        return true;
    }

    private static void AlignTransformToGround(
        Transform target,
        Transform[] selectedRoots,
        bool useUndo
    )
    {
        Vector3 rayOrigin = target.position + Vector3.up * raycastHeight;
        Ray ray = new Ray(rayOrigin, Vector3.down);

        bool foundValidHit;
        RaycastHit bestHit;

        if (groundHitMode == GroundHitMode.PhysicsColliders)
        {
            foundValidHit = TryFindColliderHit(
                ray,
                selectedRoots,
                out bestHit
            );
        }
        else
        {
            foundValidHit = TryFindMeshRendererHit(
                ray,
                selectedRoots,
                out bestHit
            );
        }

        if (!foundValidHit)
            return;

        if (useUndo)
            Undo.RecordObject(target, "Snap To Ground");

        Vector3 position = target.position;

        float yOffset = extraYOffset;

        if (useBoundsBottom)
        {
            Bounds bounds;

            if (TryGetCombinedBounds(target, out bounds))
            {
                float pivotToBottom = target.position.y - bounds.min.y;
                yOffset += pivotToBottom;
            }
        }

        position.y = bestHit.point.y + yOffset;
        target.position = position;

        if (alignRotationToGround)
        {
            target.rotation = GetGroundAlignedRotation(
                target,
                bestHit.normal
            );
        }

        lastPositions[target.GetInstanceID()] = target.position;

        EditorUtility.SetDirty(target);
    }

    private static bool TryFindColliderHit(
        Ray ray,
        Transform[] selectedRoots,
        out RaycastHit bestHit
    )
    {
        bestHit = default;

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            raycastDistance,
            groundLayerMask,
            QueryTriggerInteraction.Ignore
        );

        if (hits == null || hits.Length == 0)
            return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];

            if (hit.collider == null)
                continue;

            if (IsHitPartOfSelection(hit.transform, selectedRoots))
                continue;

            bestHit = hit;
            return true;
        }

        return false;
    }

    private static bool TryFindMeshRendererHit(
    Ray ray,
    Transform[] selectedRoots,
    out RaycastHit bestHit
)
    {
        bestHit = default;

        MeshFilter[] meshFilters = Object.FindObjectsOfType<MeshFilter>();

        bool foundHit = false;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter meshFilter = meshFilters[i];

            if (meshFilter == null)
                continue;

            Mesh sharedMesh = meshFilter.sharedMesh;

            if (sharedMesh == null)
                continue;

            Renderer renderer = meshFilter.GetComponent<Renderer>();

            if (renderer == null)
                continue;

            if (!renderer.enabled)
                continue;

            if (!renderer.gameObject.activeInHierarchy)
                continue;

            if (IsHitPartOfSelection(meshFilter.transform, selectedRoots))
                continue;

            if (!renderer.bounds.IntersectRay(ray))
                continue;

            Vector3[] vertices = sharedMesh.vertices;
            int[] triangles = sharedMesh.triangles;
            Matrix4x4 localToWorld = meshFilter.transform.localToWorldMatrix;

            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 v0 = localToWorld.MultiplyPoint3x4(vertices[triangles[t]]);
                Vector3 v1 = localToWorld.MultiplyPoint3x4(vertices[triangles[t + 1]]);
                Vector3 v2 = localToWorld.MultiplyPoint3x4(vertices[triangles[t + 2]]);

                float distance;
                Vector3 hitPoint;

                if (!RayIntersectsTriangle(ray, v0, v1, v2, out distance, out hitPoint))
                    continue;

                if (distance < 0f || distance > raycastDistance)
                    continue;

                if (distance >= closestDistance)
                    continue;

                Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0);

                if (normal.sqrMagnitude < 0.0001f)
                    continue;

                normal.Normalize();

                // Keep the normal pointing upward for ground alignment.
                if (normal.y < 0f)
                    normal = -normal;

                closestDistance = distance;

                bestHit.point = hitPoint;
                bestHit.normal = normal;
                bestHit.distance = distance;

                foundHit = true;
            }
        }

        return foundHit;
    }


    private static bool RayIntersectsTriangle(
    Ray ray,
    Vector3 v0,
    Vector3 v1,
    Vector3 v2,
    out float distance,
    out Vector3 hitPoint
)
    {
        distance = 0f;
        hitPoint = Vector3.zero;

        const float epsilon = 0.000001f;

        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;

        Vector3 h = Vector3.Cross(ray.direction, edge2);
        float a = Vector3.Dot(edge1, h);

        // Two-sided triangle hit test.
        if (a > -epsilon && a < epsilon)
            return false;

        float f = 1f / a;
        Vector3 s = ray.origin - v0;
        float u = f * Vector3.Dot(s, h);

        if (u < 0f || u > 1f)
            return false;

        Vector3 q = Vector3.Cross(s, edge1);
        float v = f * Vector3.Dot(ray.direction, q);

        if (v < 0f || u + v > 1f)
            return false;

        distance = f * Vector3.Dot(edge2, q);

        if (distance <= epsilon)
            return false;

        hitPoint = ray.origin + ray.direction * distance;
        return true;
    }

    private static Quaternion GetGroundAlignedRotation(
        Transform target,
        Vector3 groundNormal
    )
    {
        Vector3 forward = target.forward;

        forward = Vector3.ProjectOnPlane(forward, groundNormal);

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.ProjectOnPlane(Vector3.forward, groundNormal);

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.Cross(target.right, groundNormal);

        forward.Normalize();

        return Quaternion.LookRotation(forward, groundNormal);
    }

    private static bool TryGetCombinedBounds(
        Transform root,
        out Bounds combinedBounds
    )
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);

        bool hasBounds = false;
        combinedBounds = new Bounds(root.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
                continue;

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            if (collider == null)
                continue;

            if (!hasBounds)
            {
                combinedBounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(collider.bounds);
            }
        }

        return hasBounds;
    }

    private static bool IsHitPartOfSelection(
        Transform hitTransform,
        Transform[] selectedRoots
    )
    {
        if (hitTransform == null || selectedRoots == null)
            return false;

        for (int i = 0; i < selectedRoots.Length; i++)
        {
            Transform selected = selectedRoots[i];

            if (selected == null)
                continue;

            if (hitTransform == selected || hitTransform.IsChildOf(selected))
                return true;
        }

        return false;
    }

    private static LayerMask LayerMaskField(string label, LayerMask selected)
    {
        List<string> layerNames = new List<string>();
        List<int> layerNumbers = new List<int>();

        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);

            if (string.IsNullOrEmpty(layerName))
                continue;

            layerNames.Add(layerName);
            layerNumbers.Add(i);
        }

        int maskWithoutEmpty = 0;

        for (int i = 0; i < layerNumbers.Count; i++)
        {
            if (((1 << layerNumbers[i]) & selected.value) != 0)
                maskWithoutEmpty |= 1 << i;
        }

        maskWithoutEmpty = EditorGUILayout.MaskField(
            label,
            maskWithoutEmpty,
            layerNames.ToArray()
        );

        int newMask = 0;

        for (int i = 0; i < layerNumbers.Count; i++)
        {
            if ((maskWithoutEmpty & (1 << i)) != 0)
                newMask |= 1 << layerNumbers[i];
        }

        selected.value = newMask;
        return selected;
    }

    private static void LoadPrefs()
    {
        autoAlign = EditorPrefs.GetBool(PrefAutoAlign, false);
        alignRotationToGround = EditorPrefs.GetBool(PrefAlignRotation, false);
        useBoundsBottom = EditorPrefs.GetBool(PrefUseBoundsBottom, true);
        extraYOffset = EditorPrefs.GetFloat(PrefExtraOffset, 0f);
        raycastHeight = EditorPrefs.GetFloat(PrefRaycastHeight, 50f);
        raycastDistance = EditorPrefs.GetFloat(PrefRaycastDistance, 150f);
        groundLayerMask = EditorPrefs.GetInt(PrefLayerMask, Physics.DefaultRaycastLayers);

        groundHitMode = (GroundHitMode)EditorPrefs.GetInt(
            PrefGroundHitMode,
            (int)GroundHitMode.SceneMeshRenderers
        );
    }

    private static void SavePrefs()
    {
        EditorPrefs.SetBool(PrefAutoAlign, autoAlign);
        EditorPrefs.SetBool(PrefAlignRotation, alignRotationToGround);
        EditorPrefs.SetBool(PrefUseBoundsBottom, useBoundsBottom);
        EditorPrefs.SetFloat(PrefExtraOffset, extraYOffset);
        EditorPrefs.SetFloat(PrefRaycastHeight, raycastHeight);
        EditorPrefs.SetFloat(PrefRaycastDistance, raycastDistance);
        EditorPrefs.SetInt(PrefLayerMask, groundLayerMask.value);
        EditorPrefs.SetInt(PrefGroundHitMode, (int)groundHitMode);
    }
}