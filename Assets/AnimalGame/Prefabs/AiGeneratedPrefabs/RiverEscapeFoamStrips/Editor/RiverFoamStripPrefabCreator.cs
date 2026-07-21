#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class RiverFoamStripPrefabCreator
{
    private const string PrefabFolder = "Assets/AnimalGame/Prefabs/AiGenerated prefabs/RiverEscapeFoam/Prefabs";
    private const string MaterialFolder = "Assets/AnimalGame/Prefabs/AiGenerated prefabs/RiverEscapeFoam/Materials";

    [MenuItem("Tools/River Escape/Create Foam Strip Prefab")]
    public static void CreatePrefab()
    {
        string texturePath = FindAssetPath("RiverFoamStrip_Transparent", "Texture2D");

        if (string.IsNullOrEmpty(texturePath))
        {
            Debug.LogError("[RiverFoamStrip] Could not find RiverFoamStrip_Transparent texture anywhere in Assets.");
            return;
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

        if (texture == null)
        {
            Debug.LogError("[RiverFoamStrip] Failed to load texture at: " + texturePath);
            return;
        }

        EnsureTextureSettings(texturePath);

        Directory.CreateDirectory(PrefabFolder);
        Directory.CreateDirectory(MaterialFolder);

        Material material = CreateFoamMaterial(texture);
        string materialPath = AssetDatabase.GenerateUniqueAssetPath(MaterialFolder + "/RiverFoamStrip_Mat.mat");
        AssetDatabase.CreateAsset(material, materialPath);

        GameObject root = new GameObject("RiverFoamStrip");
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "FoamQuad";
        quad.transform.SetParent(root.transform, false);
        quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localScale = new Vector3(8f, 1.4f, 1f);

        Collider collider = quad.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        RiverFoamStripUVScroller scroller = root.AddComponent<RiverFoamStripUVScroller>();
        scroller.targetRenderer = renderer;
        scroller.scrollSpeed = new Vector2(0.35f, 0f);
        scroller.tint = new Color(0.85f, 1f, 0.95f, 0.65f);

        string prefabPath = AssetDatabase.GenerateUniqueAssetPath(PrefabFolder + "/RiverFoamStrip.prefab");
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[RiverFoamStrip] Created prefab at: " + prefabPath);
    }

    private static string FindAssetPath(string name, string type)
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:" + type);

        if (guids == null || guids.Length == 0)
            return null;

        return AssetDatabase.GUIDToAssetPath(guids[0]);
    }

    private static Material CreateFoamMaterial(Texture2D texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        material.name = "RiverFoamStrip_Mat";

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", texture);

        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);

        Color tint = new Color(0.85f, 1f, 0.95f, 0.65f);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", tint);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", tint);

        SetupTransparentMaterial(material);

        return material;
    }

    private static void SetupTransparentMaterial(Material material)
    {
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);

        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);

        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);

        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private static void EnsureTextureSettings(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;

        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 1024;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;

        importer.SaveAndReimport();
    }
}
#endif
