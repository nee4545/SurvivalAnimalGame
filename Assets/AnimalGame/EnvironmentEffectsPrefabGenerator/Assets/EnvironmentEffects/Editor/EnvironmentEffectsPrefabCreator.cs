#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class EnvironmentEffectsPrefabCreator
{
    private const string RootPath = "Assets/EnvironmentEffects";
    private const string PrefabPath = RootPath + "/Prefabs";
    private const string MaterialPath = RootPath + "/Materials";

    [MenuItem("Tools/Environment Effects/Create Weather Effect Prefabs")]
    public static void CreateWeatherPrefabs()
    {
        EnsureFolders();

        Material rainMat = CreateParticleMaterial("M_Rain_Streak", new Color(0.72f, 0.88f, 1f, 0.55f));
        Material snowMat = CreateParticleMaterial("M_Snow_Flake", new Color(1f, 1f, 1f, 0.78f));
        Material windMat = CreateParticleMaterial("M_Wind_Dust", new Color(0.92f, 0.86f, 0.72f, 0.35f));

        CreateRainPrefab(rainMat);
        CreateSnowPrefab(snowMat);
        CreateWindPrefab(windMat);
        CreateWeatherRigPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Environment effect prefabs created at Assets/EnvironmentEffects/Prefabs");
    }

    private static void EnsureFolders()
    {
        CreateFolderIfMissing(RootPath);
        CreateFolderIfMissing(PrefabPath);
        CreateFolderIfMissing(MaterialPath);
    }

    private static void CreateFolderIfMissing(string fullPath)
    {
        if (AssetDatabase.IsValidFolder(fullPath)) return;

        string[] parts = fullPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static Material CreateParticleMaterial(string name, Color tint)
    {
        string shaderName = "Universal Render Pipeline/Particles/Unlit";
        Shader shader = Shader.Find(shaderName);

        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        mat.name = name;
        mat.color = tint;

        string assetPath = $"{MaterialPath}/{name}.mat";
        AssetDatabase.CreateAsset(mat, assetPath);
        return AssetDatabase.LoadAssetAtPath<Material>(assetPath);
    }

    private static void CreateRainPrefab(Material mat)
    {
        GameObject go = new GameObject("FX_Rain_Light");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(20f, 28f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.07f);
        main.maxParticles = 900;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 650f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(28f, 1f, 28f);
        shape.position = new Vector3(0f, 10f, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-1.5f, -3f);
        velocity.y = new ParticleSystem.MinMaxCurve(-22f, -30f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.55f, 0.12f), new GradientAlphaKey(0.4f, 0.8f), new GradientAlphaKey(0f, 1f) }
        );
        color.color = gradient;

        renderer.material = mat;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.08f;
        renderer.lengthScale = 2.2f;
        renderer.maxParticleSize = 0.25f;

        SavePrefab(go, "FX_Rain_Light");
    }

    private static void CreateSnowPrefab(Material mat)
    {
        GameObject go = new GameObject("FX_Snow_Light");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.055f, 0.16f);
        main.maxParticles = 700;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.08f;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 220f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(28f, 1f, 28f);
        shape.position = new Vector3(0f, 9f, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.9f, -2f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        noise.frequency = 0.55f;
        noise.scrollSpeed = 0.2f;

        renderer.material = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.maxParticleSize = 0.3f;

        SavePrefab(go, "FX_Snow_Light");
    }

    private static void CreateWindPrefab(Material mat)
    {
        GameObject go = new GameObject("FX_Wind_Dust");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 10f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.28f);
        main.maxParticles = 220;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 55f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(8f, 4f, 28f);
        shape.position = new Vector3(-8f, 2f, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(8f, 14f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.3f, 0.8f);
        velocity.z = new ParticleSystem.MinMaxCurve(-1f, 1f);

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.6f, 1.5f);
        noise.frequency = 0.8f;
        noise.scrollSpeed = 0.6f;

        renderer.material = mat;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.04f;
        renderer.lengthScale = 1.5f;
        renderer.maxParticleSize = 0.45f;

        SavePrefab(go, "FX_Wind_Dust");
    }

    private static void CreateWeatherRigPrefab()
    {
        GameObject root = new GameObject("EnvironmentEffects_Rig");
        WeatherFollowTarget follow = root.AddComponent<WeatherFollowTarget>();
        EnvironmentEffectsController controller = root.AddComponent<EnvironmentEffectsController>();

        GameObject rain = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/FX_Rain_Light.prefab")) as GameObject;
        GameObject snow = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/FX_Snow_Light.prefab")) as GameObject;
        GameObject wind = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/FX_Wind_Dust.prefab")) as GameObject;

        rain.transform.SetParent(root.transform, false);
        snow.transform.SetParent(root.transform, false);
        wind.transform.SetParent(root.transform, false);

        controller.rainEffect = rain.GetComponent<ParticleSystem>();
        controller.snowEffect = snow.GetComponent<ParticleSystem>();
        controller.windEffect = wind.GetComponent<ParticleSystem>();

        controller.SetClear();
        SavePrefab(root, "EnvironmentEffects_Rig");
    }

    private static void SavePrefab(GameObject go, string name)
    {
        string path = $"{PrefabPath}/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }
}
#endif
