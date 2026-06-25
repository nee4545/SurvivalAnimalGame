using UnityEngine;

[ExecuteAlways]
public class SpecialZoneParticleVFX : MonoBehaviour
{
    [Header("Ring")]
    public Renderer ringRenderer;
    public bool rotateRing = true;
    public float ringRotationSpeed = 18f;
    public bool pulseGlow = true;
    public float minGlow = 1f;
    public float maxGlow = 3f;
    public float glowPulseSpeed = 1.35f;
    public string glowPropertyName = "_GlowIntensity";

    [Header("Particle Look")]
    public Material particleMaterial;
    public Color particleColor = new Color(1f, 0.62f, 0.12f, 1f);
    public float zoneRadius = 5f;
    public bool playOnEnable = true;

    [Header("Particle Density")]
    [Range(0.25f, 2f)] public float density = 1f;

    private Material runtimeRingMaterial;
    private ParticleSystem sparks;
    private ParticleSystem lightRays;
    private ParticleSystem rimMotes;
    private bool built;

    private void Awake()
    {
        BuildIfNeeded();
        CacheRingMaterial();
    }

    private void OnEnable()
    {
        BuildIfNeeded();
        CacheRingMaterial();

        if (playOnEnable)
            PlayAll();
    }

    private void OnValidate()
    {
        zoneRadius = Mathf.Max(0.5f, zoneRadius);
        density = Mathf.Max(0.25f, density);

        if (Application.isPlaying)
            ApplyParticleSettings();
    }

    private void Update()
    {
        if (rotateRing)
            transform.Rotate(Vector3.up, ringRotationSpeed * Time.deltaTime, Space.World);

        if (pulseGlow && runtimeRingMaterial != null)
        {
            float t = (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f;
            float glow = Mathf.Lerp(minGlow, maxGlow, t);

            if (runtimeRingMaterial.HasProperty(glowPropertyName))
                runtimeRingMaterial.SetFloat(glowPropertyName, glow);
            else if (runtimeRingMaterial.HasProperty("_Intensity"))
                runtimeRingMaterial.SetFloat("_Intensity", glow);
        }
    }

    private void CacheRingMaterial()
    {
        if (ringRenderer == null)
            ringRenderer = GetComponentInChildren<Renderer>();

        if (ringRenderer != null)
            runtimeRingMaterial = Application.isPlaying ? ringRenderer.material : ringRenderer.sharedMaterial;
    }

    public void PlayAll()
    {
        if (sparks) sparks.Play();
        if (lightRays) lightRays.Play();
        if (rimMotes) rimMotes.Play();
    }

    public void StopAll()
    {
        if (sparks) sparks.Stop();
        if (lightRays) lightRays.Stop();
        if (rimMotes) rimMotes.Stop();
    }

    private void BuildIfNeeded()
    {
        if (built)
            return;

        sparks = CreateSystem("Golden_Sparks");
        lightRays = CreateSystem("Vertical_Light_Rays");
        rimMotes = CreateSystem("Rim_Motes");

        ApplyParticleSettings();
        built = true;
    }

    private ParticleSystem CreateSystem(string childName)
    {
        Transform child = transform.Find(childName);

        if (child == null)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        ParticleSystem ps = child.GetComponent<ParticleSystem>();
        if (ps == null)
            ps = child.gameObject.AddComponent<ParticleSystem>();

        ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
        if (renderer != null && particleMaterial != null)
            renderer.sharedMaterial = particleMaterial;

        return ps;
    }

    private void ApplyParticleSettings()
    {
        SetupSparks();
        SetupLightRays();
        SetupRimMotes();
    }

    private void SetupSparks()
    {
        if (!sparks) return;

        var main = sparks.main;
        main.loop = true;
        main.playOnAwake = playOnEnable;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.72f, 0.22f, 0.65f),
            new Color(1f, 0.42f, 0.08f, 0.15f)
        );
        main.gravityModifier = -0.05f;
        main.maxParticles = Mathf.RoundToInt(140 * density);

        var emission = sparks.emission;
        emission.enabled = true;
        emission.rateOverTime = 22f * density;

        var shape = sparks.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = zoneRadius * 0.9f;
        shape.radiusThickness = 0.18f;
        shape.arc = 360f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        var velocity = sparks.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        // All velocity axes must use the same curve mode in Unity.
        // RandomBetweenTwoConstants on X/Y/Z prevents:
        // "Particle velocity curves must be in same mode".
        velocity.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.45f, 1.8f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);

        var color = sparks.colorOverLifetime;
        color.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.62f, 0.14f), 0f),
                new GradientColorKey(new Color(1f, 0.82f, 0.34f), 0.4f),
                new GradientColorKey(new Color(1f, 0.36f, 0.06f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.9f, 0.12f),
                new GradientAlphaKey(0.45f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        color.color = g;

        var renderer = sparks.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 5;
        if (particleMaterial) renderer.sharedMaterial = particleMaterial;
    }

    private void SetupLightRays()
    {
        if (!lightRays) return;

        var main = lightRays.main;
        main.loop = true;
        main.playOnAwake = playOnEnable;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 2.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(1.3f, 2.9f);
        main.startSizeZ = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.72f, 0.24f, 0.22f),
            new Color(1f, 0.55f, 0.08f, 0.05f)
        );
        main.maxParticles = Mathf.RoundToInt(30 * density);

        var emission = lightRays.emission;
        emission.enabled = true;
        emission.rateOverTime = 4.5f * density;

        var shape = lightRays.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = zoneRadius * 0.85f;
        shape.radiusThickness = 0.25f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        var color = lightRays.colorOverLifetime;
        color.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.72f, 0.24f), 0f),
                new GradientColorKey(new Color(1f, 0.56f, 0.08f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.35f, 0.18f),
                new GradientAlphaKey(0.18f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        color.color = g;

        var renderer = lightRays.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.VerticalBillboard;
        renderer.sortingOrder = 6;
        if (particleMaterial) renderer.sharedMaterial = particleMaterial;
    }

    private void SetupRimMotes()
    {
        if (!rimMotes) return;

        var main = rimMotes.main;
        main.loop = true;
        main.playOnAwake = playOnEnable;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.35f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.84f, 0.34f, 0.38f),
            new Color(1f, 0.48f, 0.08f, 0.12f)
        );
        main.maxParticles = Mathf.RoundToInt(80 * density);

        var emission = rimMotes.emission;
        emission.enabled = true;
        emission.rateOverTime = 10f * density;

        var shape = rimMotes.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = zoneRadius;
        shape.radiusThickness = 0.04f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        var velocity = rimMotes.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        // All velocity axes must use the same curve mode in Unity.
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.15f, 0.75f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var renderer = rimMotes.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 7;
        if (particleMaterial) renderer.sharedMaterial = particleMaterial;
    }
}
