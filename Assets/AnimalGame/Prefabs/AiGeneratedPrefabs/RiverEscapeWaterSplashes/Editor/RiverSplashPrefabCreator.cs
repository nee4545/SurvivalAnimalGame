#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RiverSplashPrefabCreator
{
    private const string RootFolder = "Assets/AnimalGame/Prefabs/AiGenerated prefabs/RiverEscapeWaterSplash";
    private const string TextureSearchName = "RiverSplashSoftParticle";

    [MenuItem("Tools/River Escape/Create Water Splash Prefabs")]
    public static void CreatePrefabs()
    {
        EnsureFolders();

        Texture2D texture = FindTexture();
        Material material = CreateSplashMaterial(texture);

        CreateContinuousSplashPrefab(material);
        CreateLandingBurstPrefab(material);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[RiverSplashPrefabCreator] Created water splash prefabs in: " + RootFolder);
    }

    private static void EnsureFolders()
    {
        CreateFolderIfNeeded("Assets/AnimalGame");
        CreateFolderIfNeeded("Assets/AnimalGame/Prefabs");
        CreateFolderIfNeeded("Assets/AnimalGame/Prefabs/AiGenerated prefabs");
        CreateFolderIfNeeded(RootFolder);
        CreateFolderIfNeeded(RootFolder + "/Materials");
        CreateFolderIfNeeded(RootFolder + "/Prefabs");
    }

    private static void CreateFolderIfNeeded(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string folder = Path.GetFileName(path);

        AssetDatabase.CreateFolder(parent, folder);
    }

    private static Texture2D FindTexture()
    {
        string[] guids = AssetDatabase.FindAssets(TextureSearchName + " t:Texture2D");

        if (guids == null || guids.Length == 0)
        {
            Debug.LogWarning("[RiverSplashPrefabCreator] Could not find texture named " + TextureSearchName + ". Prefab will still be created with a default particle material.");
            return null;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Material CreateSplashMaterial(Texture2D texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");

        if (shader == null)
            shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        material.name = "M_RiverSplashParticle";

        if (texture != null)
        {
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);

            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", new Color(0.82f, 1f, 0.97f, 0.72f));

        if (material.HasProperty("_TintColor"))
            material.SetColor("_TintColor", new Color(0.82f, 1f, 0.97f, 0.72f));

        string materialPath = RootFolder + "/Materials/M_RiverSplashParticle.mat";
        AssetDatabase.CreateAsset(material, materialPath);

        return AssetDatabase.LoadAssetAtPath<Material>(materialPath);
    }

    private static void CreateContinuousSplashPrefab(Material material)
    {
        GameObject root = new GameObject("RiverContinuousSplash");

        RiverSplashLockToWater lockToWater = root.AddComponent<RiverSplashLockToWater>();
        lockToWater.localOffset = new Vector3(0f, 0f, -0.45f);
        lockToWater.heightOffset = 0.05f;
        lockToWater.rotateWithTarget = true;
        lockToWater.rotationEulerOffset = Vector3.zero;

        ParticleSystem ps = root.AddComponent<ParticleSystem>();
        ConfigureContinuousParticleSystem(ps, material);

        string path = RootFolder + "/Prefabs/RiverContinuousSplash.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void CreateLandingBurstPrefab(Material material)
    {
        GameObject root = new GameObject("RiverLandingSplashBurst");

        RiverSplashLockToWater lockToWater = root.AddComponent<RiverSplashLockToWater>();
        lockToWater.localOffset = new Vector3(0f, 0f, 0f);
        lockToWater.heightOffset = 0.06f;
        lockToWater.rotateWithTarget = true;

        RiverSplashBurst burst = root.AddComponent<RiverSplashBurst>();
        burst.defaultBurstCount = 26;

        ParticleSystem ps = root.AddComponent<ParticleSystem>();
        ConfigureBurstParticleSystem(ps, material);
        burst.burstParticles = new[] { ps };

        string path = RootFolder + "/Prefabs/RiverLandingSplashBurst.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void ConfigureContinuousParticleSystem(ParticleSystem ps, Material material)
    {
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.48f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.75f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.80f, 1f, 0.96f, 0.42f), new Color(1f, 1f, 1f, 0.68f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 45;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 13f;
        // Continuous splash uses rateOverTime only. Do not pass null to SetBursts.
        emission.SetBursts(new ParticleSystem.Burst[0]);

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 20f;
        shape.radius = 0.06f;
        shape.length = 0.2f;
        shape.position = Vector3.zero;
        shape.rotation = new Vector3(-20f, 0f, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.18f, 0.18f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.75f, -0.25f);

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.25f);
        sizeCurve.AddKey(0.25f, 1f);
        sizeCurve.AddKey(1f, 0f);
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.82f, 1f, 0.97f), 0f),
                new GradientColorKey(Color.white, 0.35f),
                new GradientColorKey(new Color(0.82f, 1f, 0.97f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.68f, 0.15f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        color.color = gradient;

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = material;
        renderer.sortingOrder = 5;
    }

    private static void ConfigureBurstParticleSystem(ParticleSystem ps, Material material)
    {
        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.55f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.58f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 1.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.38f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.82f, 1f, 0.97f, 0.55f), new Color(1f, 1f, 1f, 0.85f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 60;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.22f;
        shape.arc = 360f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.65f, 0.65f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.45f, 1.25f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.65f, 0.65f);

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.15f);
        sizeCurve.AddKey(0.2f, 1f);
        sizeCurve.AddKey(1f, 0f);
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.82f, 1f, 0.97f), 0f),
                new GradientColorKey(Color.white, 0.4f),
                new GradientColorKey(new Color(0.82f, 1f, 0.97f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.85f, 0.12f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        color.color = gradient;

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = material;
        renderer.sortingOrder = 6;
    }
}
#endif
