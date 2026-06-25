using UnityEngine;

public class SpecialZoneRingAnimator : MonoBehaviour
{
    [Header("Rotation")]
    public bool rotate = true;
    public float rotationSpeed = 25f;

    [Header("Glow Pulse")]
    public Renderer targetRenderer;
    public float minGlow = 1f;
    public float maxGlow = 3f;
    public float glowPulseSpeed = 1.5f;

    [Header("Shader Property")]
    public string glowPropertyName = "_GlowIntensity";

    private Material runtimeMaterial;
    private bool hasGlowProperty;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (targetRenderer != null)
        {
            runtimeMaterial = targetRenderer.material;
            hasGlowProperty = runtimeMaterial.HasProperty(glowPropertyName);

            if (!hasGlowProperty)
            {
                Debug.LogWarning(
                    $"Glow property '{glowPropertyName}' not found on material. Check shader property name.",
                    this
                );
            }
        }
    }

    private void Update()
    {
        if (rotate)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        if (runtimeMaterial != null && hasGlowProperty)
        {
            float t = (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f;
            float glow = Mathf.Lerp(minGlow, maxGlow, t);

            runtimeMaterial.SetFloat(glowPropertyName, glow);
        }
    }
}