using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class GrassChunkBakerWindow : EditorWindow
{
    [Header("Input")]
    [SerializeField] private Transform sourceRoot;
    [SerializeField] private bool includeInactive = false;

    [Header("Chunking")]
    [SerializeField] private float chunkSize = 12f;          // meters
    [SerializeField] private Vector2 chunkOriginXZ = Vector2.zero; // anchor grid; set to terrain origin if you want

    [Header("Output")]
    [SerializeField] private Transform outputParent;
    [SerializeField] private string outputFolder = "Assets/BakedGrassChunks";
    [SerializeField] private string chunkNamePrefix = "GrassChunk_";
    [SerializeField] private bool disableSourceRenderers = true;
    [SerializeField] private bool markChunksStatic = true;

    [MenuItem("Tools/Grass/Chunk Baker")]
    public static void Open() => GetWindow<GrassChunkBakerWindow>("Grass Chunk Baker");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Grass Chunk Baker", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        sourceRoot = (Transform)EditorGUILayout.ObjectField("Source Root", sourceRoot, typeof(Transform), true);
        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);

        EditorGUILayout.Space(6);
        chunkSize = EditorGUILayout.FloatField("Chunk Size (m)", chunkSize);
        chunkOriginXZ = EditorGUILayout.Vector2Field("Chunk Origin XZ", chunkOriginXZ);

        EditorGUILayout.Space(6);
        outputParent = (Transform)EditorGUILayout.ObjectField("Output Parent", outputParent, typeof(Transform), true);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        chunkNamePrefix = EditorGUILayout.TextField("Chunk Name Prefix", chunkNamePrefix);
        disableSourceRenderers = EditorGUILayout.Toggle("Disable Source Renderers", disableSourceRenderers);
        markChunksStatic = EditorGUILayout.Toggle("Mark Chunks Static", markChunksStatic);

        EditorGUILayout.Space(12);

        using (new EditorGUI.DisabledScope(sourceRoot == null || chunkSize <= 0.01f))
        {
            if (GUILayout.Button("Bake Chunks", GUILayout.Height(40)))
            {
                Bake();
            }
        }

        EditorGUILayout.HelpBox(
            "Tip: Start with Chunk Size = 12m for mobile. If each chunk ends up too heavy (high verts), switch to 8m.\n" +
            "This tool bakes Mesh assets. Your grass bending shader continues to work because it bends in world space.",
            MessageType.Info
        );
    }

    private struct CellKey
    {
        public int x, z;
        public CellKey(int x, int z) { this.x = x; this.z = z; }
        public override int GetHashCode() => (x * 73856093) ^ (z * 19349663);
        public override bool Equals(object obj) => obj is CellKey other && other.x == x && other.z == z;
    }

    private void Bake()
    {
        if (!EnsureFolder(outputFolder))
        {
            Debug.LogError($"Could not create output folder: {outputFolder}");
            return;
        }

        // Collect renderers under source root
        var renderers = sourceRoot.GetComponentsInChildren<MeshRenderer>(includeInactive);
        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning("No MeshRenderers found under Source Root.");
            return;
        }

        // Group by grid cell, then by material
        var buckets = new Dictionary<CellKey, Dictionary<Material, List<CombineInstance>>>(1024);

        int totalMeshes = 0;
        int skipped = 0;

        foreach (var mr in renderers)
        {
            if (!mr) continue;
            var mf = mr.GetComponent<MeshFilter>();
            if (!mf || !mf.sharedMesh) { skipped++; continue; }

            // Determine cell
            Vector3 p = mr.transform.position;
            int cx = Mathf.FloorToInt((p.x - chunkOriginXZ.x) / chunkSize);
            int cz = Mathf.FloorToInt((p.z - chunkOriginXZ.y) / chunkSize);
            var key = new CellKey(cx, cz);

            if (!buckets.TryGetValue(key, out var matMap))
            {
                matMap = new Dictionary<Material, List<CombineInstance>>(4);
                buckets.Add(key, matMap);
            }

            // Handle multiple materials: we will combine per-submesh with its material.
            var mats = mr.sharedMaterials;
            var mesh = mf.sharedMesh;
            int subMeshCount = Mathf.Min(mesh.subMeshCount, mats.Length);

            for (int s = 0; s < subMeshCount; s++)
            {
                var mat = mats[s];
                if (!mat) { skipped++; continue; }

                if (!matMap.TryGetValue(mat, out var list))
                {
                    list = new List<CombineInstance>(64);
                    matMap.Add(mat, list);
                }

                var ci = new CombineInstance
                {
                    mesh = mesh,
                    subMeshIndex = s,
                    transform = mr.transform.localToWorldMatrix
                };

                list.Add(ci);
                totalMeshes++;
            }
        }

        // Create output parent if missing
        if (!outputParent)
        {
            var go = new GameObject("__BakedGrassChunks__");
            outputParent = go.transform;
        }

        Undo.RegisterFullObjectHierarchyUndo(outputParent.gameObject, "Bake Grass Chunks");

        int chunkCount = 0;
        int meshAssetCount = 0;

        try
        {
            EditorUtility.DisplayProgressBar("Baking Grass Chunks", "Combining meshes...", 0f);

            foreach (var kvp in buckets)
            {
                var cell = kvp.Key;
                var matMap = kvp.Value;

                // Create one chunk GO per cell
                var chunkGO = new GameObject($"{chunkNamePrefix}{cell.x}_{cell.z}");
                chunkGO.transform.SetParent(outputParent, false);

                // Place chunk at cell center (helps hierarchy readability; not required for shader)
                float centerX = chunkOriginXZ.x + (cell.x + 0.5f) * chunkSize;
                float centerZ = chunkOriginXZ.y + (cell.z + 0.5f) * chunkSize;
                chunkGO.transform.position = new Vector3(centerX, 0f, centerZ);

                if (markChunksStatic)
                    GameObjectUtility.SetStaticEditorFlags(chunkGO, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);

                // For each material in cell, build a combined mesh
                foreach (var matKvp in matMap)
                {
                    var mat = matKvp.Key;
                    var combines = matKvp.Value;
                    if (combines.Count == 0) continue;

                    var combinedMesh = new Mesh();
                    // Use 32-bit index if needed
                    // (Unity decides based on vertex count, but we can force safety here)
                    combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

                    // Combine in WORLD space, but make mesh LOCAL to chunk to keep transforms stable
                    // Trick: bake world, then transform into chunk local space by applying inverse chunk matrix.
                    // We'll rewrite transforms accordingly:
                    var invChunk = chunkGO.transform.worldToLocalMatrix;
                    for (int i = 0; i < combines.Count; i++)
                    {
                        var c = combines[i];
                        c.transform = invChunk * c.transform;
                        combines[i] = c;
                    }

                    combinedMesh.CombineMeshes(combines.ToArray(), true, true, false);
                    combinedMesh.RecalculateBounds();

                    // Create child renderer for this material
                    var partGO = new GameObject(mat.name);
                    partGO.transform.SetParent(chunkGO.transform, false);

                    var mf = partGO.AddComponent<MeshFilter>();
                    var mr = partGO.AddComponent<MeshRenderer>();
                    mf.sharedMesh = combinedMesh;
                    mr.sharedMaterial = mat;

                    // Save mesh asset
                    string meshPath = $"{outputFolder}/{chunkGO.name}_{Sanitize(mat.name)}.asset";
                    meshPath = AssetDatabase.GenerateUniqueAssetPath(meshPath);
                    AssetDatabase.CreateAsset(combinedMesh, meshPath);
                    meshAssetCount++;

                    chunkCount++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // Optionally disable source renderers (keeps objects for later rebake)
        if (disableSourceRenderers)
        {
            foreach (var mr in renderers)
                if (mr) mr.enabled = false;
        }

        Debug.Log($"GrassChunkBaker: Baked {buckets.Count} cells, created ~{chunkCount} renderer parts, saved {meshAssetCount} mesh assets. Total submeshes combined: {totalMeshes}. Skipped: {skipped}.");
    }

    private static bool EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return true;

        // Create folders recursively under Assets
        if (!folderPath.StartsWith("Assets"))
            return false;

        string[] parts = folderPath.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
        return AssetDatabase.IsValidFolder(folderPath);
    }

    private static string Sanitize(string s)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s.Replace(' ', '_');
    }
}
