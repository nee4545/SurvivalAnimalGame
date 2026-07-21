#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class RiverLassoCatchRingPrefabCreator
{
    private const string TextureFileName = "RiverLassoRing_Transparent";
    private const string TextureFileNameWithExtension = "RiverLassoRing_Transparent.png";
    private const string DefaultPrefabFolder = "Assets/AnimalGame/Prefabs/AiGenerated prefabs/RiverEscapeLasso/Prefabs";
    private const string PrefabPath = DefaultPrefabFolder + "/RiverLassoCatchRing.prefab";

    [MenuItem("Tools/River Escape/Create Lasso Catch Ring Prefab")]
    public static void CreatePrefab()
    {
        string texturePath = FindTexturePath();

        if (string.IsNullOrEmpty(texturePath))
        {
            Debug.LogError(
                "[RiverEscape] Could not find " + TextureFileNameWithExtension + " anywhere under Assets.\n" +
                "Make sure the RiverEscapeLasso package folder contains Textures/" + TextureFileNameWithExtension + "."
            );
            return;
        }

        string packageRoot = GetPackageRootFromTexturePath(texturePath);
        string materialFolder = packageRoot + "/Materials";
        string materialPath = materialFolder + "/RiverLassoRing_Mat.mat";

        EnsureFolder(materialFolder);
        EnsureFolder(DefaultPrefabFolder);

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

        if (texture == null)
        {
            Debug.LogError("[RiverEscape] Found texture path but could not load texture: " + texturePath);
            return;
        }

        ConfigureTextureImporter(texturePath);
        texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

        Material material = CreateOrUpdateMaterial(texture, materialPath);

        GameObject root = new GameObject("RiverLassoCatchRing");
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "RingQuad";
        quad.transform.SetParent(root.transform, false);
        quad.transform.localPosition = Vector3.zero;
        quad.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        quad.transform.localScale = Vector3.one;

        Collider collider = quad.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        MeshRenderer meshRenderer = quad.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.sortingOrder = 50;

        RiverLassoCatchRing ring = root.AddComponent<RiverLassoCatchRing>();
        ring.ringVisual = quad.transform;
        ring.ringRenderer = meshRenderer;
        ring.heightOffsetFromWater = 0.08f;
        ring.diameterPadding = 0.15f;
        ring.normalBrightness = 1f;
        ring.activeBrightness = 1.8f;
        ring.pulseWhenActive = true;
        ring.pulseSpeed = 6f;
        ring.pulseScaleAmount = 0.08f;
        ring.flatWorldRotation = new Vector3(-90f, 0f, 0f);
        ring.visibleOnStart = false;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = prefab;

        Debug.Log(
            "[RiverEscape] Created lasso catch ring prefab at: " + PrefabPath +
            "\n[RiverEscape] Used texture: " + texturePath +
            "\n[RiverEscape] Used material: " + materialPath
        );
    }

    private static string FindTexturePath()
    {
        string[] guids = AssetDatabase.FindAssets(TextureFileName + " t:Texture2D", new[] { "Assets" });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            path = path.Replace("\\", "/");

            if (path.EndsWith(TextureFileNameWithExtension))
                return path;
        }

        return null;
    }

    private static string GetPackageRootFromTexturePath(string texturePath)
    {
        texturePath = texturePath.Replace("\\", "/");

        string marker = "/Textures/" + TextureFileNameWithExtension;
        int markerIndex = texturePath.IndexOf(marker, System.StringComparison.OrdinalIgnoreCase);

        if (markerIndex > 0)
            return texturePath.Substring(0, markerIndex);

        int lastSlash = texturePath.LastIndexOf('/');
        if (lastSlash > 0)
            return texturePath.Substring(0, lastSlash);

        return "Assets/RiverEscapeLasso";
    }

    private static Material CreateOrUpdateMaterial(Texture2D texture, string materialPath)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            if (shader == null)
                shader = Shader.Find("Unlit/Transparent");

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.name = "RiverLassoRing_Mat";

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);

        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);

        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);

        if (material.HasProperty("_AlphaClip"))
            material.SetFloat("_AlphaClip", 0f);

        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", 0f);

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.Transparent;

        EditorUtility.SetDirty(material);

        return material;
    }

    private static void ConfigureTextureImporter(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 1024;

        importer.SaveAndReimport();
    }

    private static void EnsureFolder(string folderPath)
    {
        folderPath = folderPath.Replace("\\", "/");

        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }
}
#endif
