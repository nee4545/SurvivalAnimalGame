#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class StampedeDustTrailPrefabCreator
{
    private const string RootFolder = "Assets/AnimalGame/Prefabs/AiGenerated prefabs/StampedeDustTrail";
    private const string PrefabFolder = RootFolder + "/Prefabs";
    private const string MaterialFolder = RootFolder + "/Materials";
    private const string TextureFolder = RootFolder + "/Textures";

    [MenuItem("Tools/Stampede/Create Dust Trail Prefab")]
    public static void CreateDustTrailPrefab()
    {
        EnsureFolders();

        Texture2D dustTexture = LoadOrCreateDustTexture();
        Material dustMaterial = CreateDustMaterial(dustTexture);

        GameObject root = new GameObject("StampedeDustTrail");
        StampedeDustTrail trail = root.AddComponent<StampedeDustTrail>();
        trail.localOffset = new Vector3(0f, 0.08f, -0.65f);
        trail.minSpeedToEmit = 0.75f;
        trail.fullEmissionSpeed = 8f;
        trail.emissionMultiplier = 1f;
        trail.groundYOffset = 0.04f;

        CreateDustCore(root.transform, dustMaterial);
        CreateDustMist(root.transform, dustMaterial);
        CreateDustSpecks(root.transform, dustMaterial);

        string prefabPath = PrefabFolder + "/StampedeDustTrail.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        EditorGUIUtility.PingObject(Selection.activeObject);

        Debug.Log("[StampedeDustTrail] Created prefab at: " + prefabPath);
    }

    private static void EnsureFolders()
    {
        EnsureFolderPath(RootFolder);
        EnsureFolderPath(PrefabFolder);
        EnsureFolderPath(MaterialFolder);
        EnsureFolderPath(TextureFolder);
    }

    private static void EnsureFolderPath(string fullPath)
    {
        if (AssetDatabase.IsValidFolder(fullPath))
            return;

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

    private static Texture2D LoadOrCreateDustTexture()
    {
        string texturePath = TextureFolder + "/Stampede_DustSoftCircle.png";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

        if (existing != null)
            return existing;

        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y);
                float distance = Vector2.Distance(p, center) / (size * 0.5f);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = Mathf.Pow(alpha, 2.2f);

                // Slight organic unevenness so the particles do not look like perfect circles.
                float noise = Mathf.PerlinNoise(x * 0.055f, y * 0.055f);
                alpha *= Mathf.Lerp(0.65f, 1f, noise);

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        File.WriteAllBytes(texturePath, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(texturePath);

        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
    }

    private static Material CreateDustMaterial(Texture2D texture)
    {
        string materialPath = MaterialFolder + "/Stampede_DustParticle.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        if (material == null)
        {
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.mainTexture = texture;
        material.color = new Color(0.78f, 0.52f, 0.27f, 0.75f);

        if (material.HasProperty("_Mode"))
            material.SetFloat("_Mode", 2f);

        if (material.HasProperty("_ColorMode"))
            material.SetFloat("_ColorMode", 0f);

        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);

        // Make the material reliably alpha blended across Built-in/URP-style projects.
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;

        EditorUtility.SetDirty(material);
        return material;
    }

    private static ParticleSystem CreateParticleSystemObject(string name, Transform parent, Material material)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 20;
        return ps;
    }

    private static void CreateDustCore(Transform parent, Material material)
    {
        ParticleSystem ps = CreateParticleSystemObject("Dust_Core_Puffs", parent, material);

        var main = ps.main;
        main.loop = true;
        main.duration = 4f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.95f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.70f, 0.46f, 0.23f, 0.22f),
            new Color(0.96f, 0.74f, 0.43f, 0.42f)
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = new ParticleSystem.MinMaxCurve(0.04f);
        main.maxParticles = 160;

        var emission = ps.emission;
        emission.rateOverTime = 22f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 24f;
        shape.radius = 0.32f;
        shape.rotation = new Vector3(0f, 180f, 0f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.z = new ParticleSystem.MinMaxCurve(-0.6f, -1.7f);
        velocity.x = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.1f, 0.6f);

        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.96f, 0.70f, 0.38f), 0f),
                new GradientColorKey(new Color(0.52f, 0.33f, 0.18f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.42f, 0.15f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        color.color = gradient;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.25f);
        curve.AddKey(0.2f, 1f);
        curve.AddKey(1f, 1.35f);
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private static void CreateDustMist(Transform parent, Material material)
    {
        ParticleSystem ps = CreateParticleSystemObject("Dust_Ground_Mist", parent, material);

        var main = ps.main;
        main.loop = true;
        main.duration = 4f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-3.14f, 3.14f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.78f, 0.52f, 0.28f, 0.12f),
            new Color(0.95f, 0.77f, 0.48f, 0.28f)
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = new ParticleSystem.MinMaxCurve(0f);
        main.maxParticles = 80;

        var emission = ps.emission;
        emission.rateOverTime = 8f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.4f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        // IMPORTANT: x/y/z must all use the same MinMaxCurve mode.
        // Unity logs "Particle Velocity curves must all be in the same mode" if one axis
        // is TwoConstants and another stays as the default Constant.
        velocity.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.z = new ParticleSystem.MinMaxCurve(-1.2f, -0.4f);

        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.95f, 0.74f, 0.42f), 0f),
                new GradientColorKey(new Color(0.54f, 0.36f, 0.20f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.24f, 0.25f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        color.color = gradient;
    }

    private static void CreateDustSpecks(Transform parent, Material material)
    {
        ParticleSystem ps = CreateParticleSystemObject("Dust_Specks_Debris", parent, material);

        var main = ps.main;
        main.loop = true;
        main.duration = 4f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-3.14f, 3.14f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.36f, 0.20f, 0.08f, 0.65f),
            new Color(0.84f, 0.54f, 0.22f, 0.85f)
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = new ParticleSystem.MinMaxCurve(0.6f);
        main.maxParticles = 110;

        var emission = ps.emission;
        emission.rateOverTime = 12f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 36f;
        shape.radius = 0.2f;
        shape.rotation = new Vector3(0f, 180f, 0f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.2f, 1.0f);
        velocity.z = new ParticleSystem.MinMaxCurve(-1.0f, -2.4f);

        var color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.78f, 0.48f, 0.18f), 0f),
                new GradientColorKey(new Color(0.22f, 0.12f, 0.04f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.9f, 0.1f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        color.color = gradient;
    }
}
#endif
