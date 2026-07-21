using UnityEngine;

public class RiverLassoCatchRing : MonoBehaviour
{
    [Header("References")]
    public Transform followTarget;
    public Transform ringVisual;
    public Renderer ringRenderer;

    [Header("Placement")]
    public float waterY = 0f;
    public float heightOffsetFromWater = 0.08f;

    [Tooltip("Extra diameter padding so the visual looks slightly bigger than catch radius.")]
    public float diameterPadding = 0.15f;

    [Header("Rotation")]
    public bool rotateRing = true;

    [Tooltip("Degrees per second.")]
    public float rotationSpeed = 25f;

    [Tooltip("Randomizes starting rotation so it does not always begin at same angle.")]
    public bool randomizeStartRotation = true;

    private float currentRotationAngle;

    [Header("Visual")]
    public Color normalTint = new Color(1f, 0.8f, 0.15f, 0.75f);
    public Color activeTint = new Color(1f, 0.95f, 0.35f, 1f);

    public float normalBrightness = 1f;
    public float activeBrightness = 1.8f;

    [Header("Pulse")]
    public bool pulseWhenActive = true;
    public float pulseSpeed = 6f;
    public float pulseScaleAmount = 0.08f;

    [Header("Rotation")]
    [Tooltip("Keeps the quad flat on the river surface. Use -90 for most Unity quad setups. If the ring is invisible, try 90 or use a double-sided shader/material.")]
    public Vector3 flatWorldRotation = new Vector3(-90f, 0f, 0f);

    [Header("Startup")]
    public bool visibleOnStart = false;

    private bool isVisible;
    private bool isActiveCatch;
    private float baseRadius = 2.5f;

    private MaterialPropertyBlock propertyBlock;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        SetVisible(visibleOnStart);
        ApplyVisualImmediate();

        if (randomizeStartRotation)
            currentRotationAngle = Random.Range(0f, 360f);
    }

    private void LateUpdate()
    {
        if (!isVisible || followTarget == null)
            return;

        UpdatePlacement();
        ApplyVisualImmediate();
    }

    public void Show(
        Transform target,
        float ringRadius,
        float ringWaterY,
        bool activeCatch
    )
    {
        followTarget = target;
        baseRadius = ringRadius;
        waterY = ringWaterY;
        isActiveCatch = activeCatch;

        SetVisible(true);

        UpdatePlacement();
        ApplyVisualImmediate();
    }

    public void SetActiveCatch(bool activeCatch)
    {
        isActiveCatch = activeCatch;
        ApplyVisualImmediate();
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;

        if (ringRenderer != null)
            ringRenderer.enabled = visible;
    }

    private void UpdatePlacement()
    {
        if (followTarget == null || ringVisual == null)
            return;

        Vector3 position = followTarget.position;
        position.y = waterY + heightOffsetFromWater;

        ringVisual.position = position;
        if (rotateRing)
        {
            currentRotationAngle += rotationSpeed * Time.deltaTime;

            if (currentRotationAngle >= 360f)
                currentRotationAngle -= 360f;
        }

        ringVisual.rotation =
            Quaternion.AngleAxis(currentRotationAngle, Vector3.up) *
            Quaternion.Euler(90f, 0f, 0f);

        float diameter = (baseRadius * 2f) + diameterPadding;
        float pulseScale = 1f;

        if (pulseWhenActive && isActiveCatch)
        {
            pulseScale += Mathf.Sin(Time.time * pulseSpeed) * pulseScaleAmount;
        }

        ringVisual.localScale = new Vector3(
            diameter * pulseScale,
            diameter * pulseScale,
            1f
        );
    }

    private void ApplyVisualImmediate()
    {
        if (ringRenderer == null)
            return;

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        Color tint = isActiveCatch ? activeTint : normalTint;
        float brightness = isActiveCatch ? activeBrightness : normalBrightness;

        Color finalColor = tint * brightness;
        finalColor.a = tint.a;

        ringRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetColor(BaseColorId, finalColor);
        propertyBlock.SetColor(ColorId, finalColor);
        propertyBlock.SetColor(EmissionColorId, finalColor * brightness);

        ringRenderer.SetPropertyBlock(propertyBlock);
    }
}
