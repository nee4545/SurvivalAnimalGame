using UnityEngine;

[ExecuteAlways]
public class HomeBaseZoneVFX_Updated : MonoBehaviour
{
    [Header("Home Base Ring")]
    public Renderer ringRenderer;
    public bool rotateRing = true;
    public float ringRotationSpeed = 6f;

    [Header("Glow Pulse")]
    public bool pulseGlow = true;
    public float minGlow = 1f;
    public float maxGlow = 2.6f;
    public float glowPulseSpeed = 1.0f;
    public string glowPropertyName = "_GlowIntensity";

    [Header("Particle Look")]
    public Material particleMaterial;
    public float zoneRadius = 5f;
    [Range(0.25f, 3f)] public float density = 1.6f;
    public bool playOnEnable = true;

    private Material runtimeRingMaterial;
    private ParticleSystem warmSparks;
    private ParticleSystem softRays;
    private ParticleSystem homeMotes;
    private bool built;

    private void Awake() { BuildIfNeeded(); CacheRingMaterial(); }
    private void OnEnable() { BuildIfNeeded(); CacheRingMaterial(); if (playOnEnable) PlayAll(); }
    private void OnValidate() { zoneRadius = Mathf.Max(0.5f, zoneRadius); density = Mathf.Max(0.25f, density); if (Application.isPlaying) ApplyParticleSettings(); }

    private void Update()
    {
        if (rotateRing) transform.Rotate(Vector3.up, ringRotationSpeed * Time.deltaTime, Space.World);
        if (pulseGlow && runtimeRingMaterial != null)
        {
            float t = (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f;
            float glow = Mathf.Lerp(minGlow, maxGlow, t);
            if (runtimeRingMaterial.HasProperty(glowPropertyName)) runtimeRingMaterial.SetFloat(glowPropertyName, glow);
            else if (runtimeRingMaterial.HasProperty("_Intensity")) runtimeRingMaterial.SetFloat("_Intensity", glow);
        }
    }

    private void CacheRingMaterial()
    {
        if (ringRenderer == null) ringRenderer = GetComponentInChildren<Renderer>();
        if (ringRenderer != null) runtimeRingMaterial = Application.isPlaying ? ringRenderer.material : ringRenderer.sharedMaterial;
    }

    public void PlayAll() { if (warmSparks) warmSparks.Play(); if (softRays) softRays.Play(); if (homeMotes) homeMotes.Play(); }
    public void StopAll() { if (warmSparks) warmSparks.Stop(); if (softRays) softRays.Stop(); if (homeMotes) homeMotes.Stop(); }

    private void BuildIfNeeded()
    {
        if (built) return;
        warmSparks = CreateSystem("Home_Warm_Sparks");
        softRays = CreateSystem("Home_Soft_Light_Rays");
        homeMotes = CreateSystem("Home_Rim_Motes");
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
        if (ps == null) ps = child.gameObject.AddComponent<ParticleSystem>();
        var renderer = child.GetComponent<ParticleSystemRenderer>();
        if (renderer != null && particleMaterial != null) renderer.sharedMaterial = particleMaterial;
        return ps;
    }

    private void ApplyParticleSettings()
    {
        SetupWarmSparks();
        SetupSoftRays();
        SetupHomeMotes();
    }

    private void SetupWarmSparks()
    {
        if (!warmSparks) return;
        var main = warmSparks.main;
        main.loop = true; main.playOnAwake = playOnEnable; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.35f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.20f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.98f,1f,0.80f,0.75f), new Color(0.62f,1f,0.62f,0.30f));
        main.gravityModifier = -0.05f;
        main.maxParticles = Mathf.RoundToInt(220 * density);

        var emission = warmSparks.emission; emission.enabled = true; emission.rateOverTime = 34f * density;

        var shape = warmSparks.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = zoneRadius * 0.85f; shape.radiusThickness = 0.24f; shape.arc = 360f; shape.rotation = new Vector3(90f,0f,0f);

        var velocity = warmSparks.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.Local; velocity.x = new ParticleSystem.MinMaxCurve(-0.16f, 0.16f); velocity.y = new ParticleSystem.MinMaxCurve(0.45f, 1.55f); velocity.z = new ParticleSystem.MinMaxCurve(-0.16f, 0.16f);

        var color = warmSparks.colorOverLifetime; color.enabled = true; Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.96f,1f,0.80f),0f), new GradientColorKey(new Color(0.80f,1f,0.66f),0.5f), new GradientColorKey(new Color(0.55f,0.95f,0.50f),1f)},
            new GradientAlphaKey[] { new GradientAlphaKey(0f,0f), new GradientAlphaKey(0.95f,0.12f), new GradientAlphaKey(0.55f,0.62f), new GradientAlphaKey(0f,1f)}
        );
        color.color = g;

        var renderer = warmSparks.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Billboard; renderer.sortingOrder = 5; if (particleMaterial) renderer.sharedMaterial = particleMaterial;
    }

    private void SetupSoftRays()
    {
        if (!softRays) return;
        var main = softRays.main;
        main.loop = true; main.playOnAwake = playOnEnable; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(1.8f, 3.4f);
        main.startSizeZ = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.95f,1f,0.82f,0.28f), new Color(0.72f,1f,0.66f,0.10f));
        main.maxParticles = Mathf.RoundToInt(42 * density);

        var emission = softRays.emission; emission.enabled = true; emission.rateOverTime = 6.5f * density;
        var shape = softRays.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = zoneRadius * 0.72f; shape.radiusThickness = 0.38f; shape.rotation = new Vector3(90f,0f,0f);
        var renderer = softRays.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.VerticalBillboard; renderer.sortingOrder = 6; if (particleMaterial) renderer.sharedMaterial = particleMaterial;
    }

    private void SetupHomeMotes()
    {
        if (!homeMotes) return;
        var main = homeMotes.main;
        main.loop = true; main.playOnAwake = playOnEnable; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.06f, 0.34f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.22f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.96f,1f,0.84f,0.40f), new Color(0.70f,1f,0.70f,0.16f));
        main.maxParticles = Mathf.RoundToInt(130 * density);

        var emission = homeMotes.emission; emission.enabled = true; emission.rateOverTime = 18f * density;
        var shape = homeMotes.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = zoneRadius; shape.radiusThickness = 0.06f; shape.rotation = new Vector3(90f,0f,0f);
        var velocity = homeMotes.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.Local; velocity.x = new ParticleSystem.MinMaxCurve(0f,0f); velocity.y = new ParticleSystem.MinMaxCurve(0.12f, 0.65f); velocity.z = new ParticleSystem.MinMaxCurve(0f,0f);
        var renderer = homeMotes.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Billboard; renderer.sortingOrder = 7; if (particleMaterial) renderer.sharedMaterial = particleMaterial;
    }
}
