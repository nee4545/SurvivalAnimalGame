#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CreateDustTrailPrefab
{
    private const string RootFolder = "Assets/DustTrailEffect";
    private const string PrefabFolder = "Assets/DustTrailEffect/Prefabs";
    private const string MaterialFolder = "Assets/DustTrailEffect/Materials";

    [MenuItem("Tools/Dust Trail/Create Dust Trail Prefab")]
    public static void CreatePrefab()
    {
        EnsureFolders();

        Material mat = CreateDustMaterial();

        GameObject root = new GameObject("FX_DustTrail_Player");
        DustTrailController controller = root.AddComponent<DustTrailController>();

        GameObject psObj = new GameObject("DustParticles");
        psObj.transform.SetParent(root.transform);
        psObj.transform.localPosition = Vector3.zero;
        psObj.transform.localRotation = Quaternion.identity;

        ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
        ConfigureParticleSystem(ps, mat);

        controller.dustParticles = ps;
        controller.localOffset = new Vector3(0f, 0.08f, -0.35f);
        controller.minMoveSpeed = 0.15f;
        controller.maxMoveSpeed = 5f;
        controller.maxEmission = 45f;
        controller.emissionSmooth = 10f;

        string prefabPath = PrefabFolder + "/FX_DustTrail_Player.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Dust Trail Created", "Created prefab:\n" + prefabPath, "OK");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(RootFolder))
            AssetDatabase.CreateFolder("Assets", "DustTrailEffect");

        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder(RootFolder, "Prefabs");

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder(RootFolder, "Materials");
    }

    private static Material CreateDustMaterial()
    {
        string path = MaterialFolder + "/M_DustTrail.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

        Material mat = new Material(shader);
        mat.name = "M_DustTrail";
        mat.color = new Color(0.62f, 0.50f, 0.36f, 0.35f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void ConfigureParticleSystem(ParticleSystem ps, Material mat)
    {
        var main = ps.main;
        main.loop = true;
        main.duration = 2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.38f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.56f, 0.46f, 0.32f, 0.25f),
            new Color(0.78f, 0.68f, 0.50f, 0.45f)
        );
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 120;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.18f;
        shape.arc = 360f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.05f, 0.35f);
        velocity.z = new ParticleSystem.MinMaxCurve(-1.2f, -0.25f);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.35f);
        sizeCurve.AddKey(0.25f, 1f);
        sizeCurve.AddKey(1f, 0.15f);
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.65f, 0.55f, 0.38f), 0f),
                new GradientColorKey(new Color(0.75f, 0.66f, 0.48f), 0.45f),
                new GradientColorKey(new Color(0.75f, 0.66f, 0.48f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.35f, 0.15f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        color.color = gradient;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.18f;
        noise.frequency = 0.7f;
        noise.scrollSpeed = 0.25f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = mat;
        renderer.sortingOrder = 0;
    }
}
#endif
