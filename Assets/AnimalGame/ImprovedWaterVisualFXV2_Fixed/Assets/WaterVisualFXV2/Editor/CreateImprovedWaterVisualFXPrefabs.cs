using System.IO;
using UnityEditor;
using UnityEngine;

public static class CreateImprovedWaterVisualFXPrefabs
{
    private static string rootFolder;
    private static string generatedFolder;
    private static string prefabsFolder;
    private static string materialsFolder;
    private static string texturesFolder;

    [MenuItem("Tools/Water Visual FX/Create Improved Water FX Prefabs")]
    public static void CreateAll()
    {
        SetupPaths();
        EnsureFolders();

        string rippleTexPath = CreateRippleTexture();
        string splashTexPath = CreateSplashTexture();

        Material rippleMat = CreateMaterial("M_WaterRipple_Stylized.mat", rippleTexPath, new Color(1f, 1f, 1f, 0.55f));
        Material splashMat = CreateMaterial("M_WaterSplash_Stylized.mat", splashTexPath, new Color(0.95f, 0.98f, 1f, 0.9f));

        GameObject splashPrefab = CreateSplashPrefab(splashMat);
        GameObject ripplePrefab = CreateRipplePrefab(rippleMat);
        CreateTriggerPrefab(splashPrefab, ripplePrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Water Visual FX",
            "Improved splash, ripple, and trigger prefabs were created here:\\n\\n" + prefabsFolder,
            "OK"
        );
    }

    private static void SetupPaths()
    {
        string[] guids = AssetDatabase.FindAssets("CreateImprovedWaterVisualFXPrefabs t:Script");

        if (guids.Length > 0)
        {
            string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            string editorFolder = Path.GetDirectoryName(scriptPath).Replace("\\", "/");
            rootFolder = Directory.GetParent(editorFolder).FullName.Replace("\\", "/");

            int assetsIndex = rootFolder.IndexOf("Assets/");
            if (assetsIndex >= 0)
                rootFolder = rootFolder.Substring(assetsIndex);
            else
                rootFolder = "Assets/WaterVisualFXV2";
        }
        else
        {
            rootFolder = "Assets/WaterVisualFXV2";
        }

        generatedFolder = rootFolder + "/Generated";
        prefabsFolder = generatedFolder + "/Prefabs";
        materialsFolder = generatedFolder + "/Materials";
        texturesFolder = generatedFolder + "/Textures";
    }

    private static void EnsureFolders()
    {
        CreateFolderIfMissing(rootFolder);
        CreateFolderIfMissing(generatedFolder);
        CreateFolderIfMissing(prefabsFolder);
        CreateFolderIfMissing(materialsFolder);
        CreateFolderIfMissing(texturesFolder);
    }

    private static void CreateFolderIfMissing(string assetPath)
    {
        assetPath = assetPath.Replace("\\", "/");

        if (AssetDatabase.IsValidFolder(assetPath))
            return;

        string parent = Path.GetDirectoryName(assetPath).Replace("\\", "/");
        string folderName = Path.GetFileName(assetPath);

        if (!AssetDatabase.IsValidFolder(parent))
            CreateFolderIfMissing(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static string AssetPathToSystemPath(string assetPath)
    {
        assetPath = assetPath.Replace("\\", "/");

        if (assetPath.StartsWith("Assets"))
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace("\\", "/");
            return projectRoot + "/" + assetPath;
        }

        return assetPath;
    }

    private static string CreateRippleTexture()
    {
        string path = texturesFolder + "/T_WaterRipple_Ring.png";
        string systemPath = AssetPathToSystemPath(path);

        Directory.CreateDirectory(Path.GetDirectoryName(systemPath));

        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(0.5f, 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                float v = y / (float)(size - 1);
                float dist = Vector2.Distance(new Vector2(u, v), center);

                float ring1 = SmoothRing(dist, 0.26f, 0.04f);
                float ring2 = SmoothRing(dist, 0.41f, 0.03f) * 0.35f;
                float alpha = Mathf.Clamp01(ring1 + ring2);

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        File.WriteAllBytes(systemPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        ImportTexture(path);

        return path;
    }

    private static string CreateSplashTexture()
    {
        string path = texturesFolder + "/T_WaterSplash_Droplet.png";
        string systemPath = AssetPathToSystemPath(path);

        Directory.CreateDirectory(Path.GetDirectoryName(systemPath));

        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2[] centers =
        {
            new Vector2(0.5f, 0.5f),
            new Vector2(0.35f, 0.42f),
            new Vector2(0.65f, 0.42f),
            new Vector2(0.43f, 0.68f),
            new Vector2(0.57f, 0.68f),
            new Vector2(0.5f, 0.82f)
        };

        float[] radii = { 0.18f, 0.11f, 0.11f, 0.09f, 0.09f, 0.06f };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                float v = y / (float)(size - 1);
                Vector2 p = new Vector2(u, v);
                float alpha = 0f;

                for (int i = 0; i < centers.Length; i++)
                {
                    float d = Vector2.Distance(p, centers[i]);
                    float a = 1f - Mathf.InverseLerp(radii[i], radii[i] * 1.18f, d);
                    alpha = Mathf.Max(alpha, Mathf.Clamp01(a));
                }

                alpha = Mathf.SmoothStep(0f, 1f, Mathf.Pow(alpha, 0.8f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        File.WriteAllBytes(systemPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        ImportTexture(path);

        return path;
    }

    private static float SmoothRing(float dist, float radius, float halfWidth)
    {
        float outer = 1f - Mathf.InverseLerp(radius + halfWidth, radius, dist);
        float inner = Mathf.InverseLerp(radius - halfWidth, radius, dist);
        return Mathf.Clamp01(outer * inner);
    }

    private static void ImportTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static Material CreateMaterial(string fileName, string texturePath, Color tint)
    {
        string path = materialsFolder + "/" + fileName;

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        Shader shader = FindBestParticleShader();
        Material mat = new Material(shader);
        Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);

        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);

        AssetDatabase.CreateAsset(mat, path);
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    private static Shader FindBestParticleShader()
    {
        string[] candidates =
        {
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Legacy Shaders/Particles/Alpha Blended",
            "Unlit/Transparent",
            "Sprites/Default"
        };

        foreach (string candidate in candidates)
        {
            Shader shader = Shader.Find(candidate);
            if (shader != null)
                return shader;
        }

        return Shader.Find("Standard");
    }

    private static GameObject CreateSplashPrefab(Material splashMat)
    {
        GameObject root = new GameObject("FX_WaterSplash_Stylized");
        root.AddComponent<AutoDestroyParticle>();

        ParticleSystem droplets = CreateParticleChild(root.transform, "Droplets", splashMat, ParticleSystemRenderMode.Billboard);

        var main = droplets.main;
        main.duration = 0.35f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.14f, 0.24f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 2.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
        main.gravityModifier = 0.75f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 16;
        main.playOnAwake = true;

        var emission = droplets.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10, 14) });

        var shape = droplets.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 24f;
        shape.radius = 0.08f;

        ApplyFade(droplets, 0.95f, 0f);

        ParticleSystem foam = CreateParticleChild(root.transform, "FoamBase", splashMat, ParticleSystemRenderMode.Billboard);

        var main2 = foam.main;
        main2.duration = 0.4f;
        main2.loop = false;
        main2.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.28f);
        main2.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.85f);
        main2.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
        main2.gravityModifier = 0.1f;
        main2.simulationSpace = ParticleSystemSimulationSpace.World;
        main2.maxParticles = 10;
        main2.playOnAwake = true;

        var emission2 = foam.emission;
        emission2.rateOverTime = 0f;
        emission2.SetBursts(new[] { new ParticleSystem.Burst(0f, 5, 8) });

        var shape2 = foam.shape;
        shape2.enabled = true;
        shape2.shapeType = ParticleSystemShapeType.Circle;
        shape2.radius = 0.12f;

        var velocity2 = foam.velocityOverLifetime;
        velocity2.enabled = true;
        velocity2.space = ParticleSystemSimulationSpace.Local;
        velocity2.radial = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);

        ApplyFade(foam, 0.75f, 0f);

        string prefabPath = prefabsFolder + "/FX_WaterSplash_Stylized.prefab";
        AssetDatabase.DeleteAsset(prefabPath);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

        GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Object.DestroyImmediate(root);
        return saved;
    }

    private static GameObject CreateRipplePrefab(Material rippleMat)
    {
        GameObject root = new GameObject("FX_WaterRipple_Stylized");
        root.AddComponent<AutoDestroyParticle>();

        ParticleSystem ps = root.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 0.8f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 0.85f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.26f, 0.34f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.01f;

        ApplyFade(ps, 0.58f, 0f);

        var sizeLifetime = ps.sizeOverLifetime;
        sizeLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.55f),
            new Keyframe(0.35f, 0.9f),
            new Keyframe(1f, 1.85f)
        );
        sizeLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer pr = ps.GetComponent<ParticleSystemRenderer>();
        pr.material = rippleMat;
        pr.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        pr.sortMode = ParticleSystemSortMode.Distance;

        string prefabPath = prefabsFolder + "/FX_WaterRipple_Stylized.prefab";
        AssetDatabase.DeleteAsset(prefabPath);
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

        GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Object.DestroyImmediate(root);
        return saved;
    }

    private static ParticleSystem CreateParticleChild(Transform parent, string name, Material material, ParticleSystemRenderMode renderMode)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = material;
        renderer.renderMode = renderMode;
        renderer.sortMode = ParticleSystemSortMode.Distance;

        return ps;
    }

    private static void ApplyFade(ParticleSystem ps, float startAlpha, float endAlpha)
    {
        var colorLifetime = ps.colorOverLifetime;
        colorLifetime.enabled = true;

        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.95f, 0.99f, 1f), 0f),
                new GradientColorKey(new Color(0.78f, 0.92f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(startAlpha, 0f),
                new GradientAlphaKey(startAlpha * 0.55f, 0.5f),
                new GradientAlphaKey(endAlpha, 1f)
            }
        );

        colorLifetime.color = new ParticleSystem.MinMaxGradient(g);
    }

    private static void CreateTriggerPrefab(GameObject splashPrefab, GameObject ripplePrefab)
    {
        GameObject trigger = new GameObject("WaterVisualTrigger_Stylized");

        BoxCollider collider = trigger.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(10f, 2f, 10f);

        WaterVisualInteraction interaction = trigger.AddComponent<WaterVisualInteraction>();
        interaction.enterSplashPrefab = splashPrefab;
        interaction.ripplePrefab = ripplePrefab;
        interaction.surfaceOffset = 0.03f;
        interaction.rippleInterval = 0.28f;
        interaction.minMoveDistance = 0.035f;

        string prefabPath = prefabsFolder + "/WaterVisualTrigger_Stylized.prefab";
        AssetDatabase.DeleteAsset(prefabPath);
        PrefabUtility.SaveAsPrefabAsset(trigger, prefabPath);

        Object.DestroyImmediate(trigger);
    }
}