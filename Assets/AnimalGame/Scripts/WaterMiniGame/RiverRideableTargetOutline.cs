using System.Collections.Generic;
using UnityEngine;

public class RiverRideableTargetOutline : MonoBehaviour
{
    [Header("Outline")]
    public Material outlineMaterial;

    [Tooltip("If empty, renderers will be found automatically in children.")]
    public Renderer[] targetRenderers;

    [Header("Debug")]
    public bool debugLogs;

    private bool isHighlighted;
    private Material[][] originalSharedMaterials;

    private void Awake()
    {
        CacheRenderers();
        CacheOriginalMaterials();
    }

    private void OnDisable()
    {
        SetHighlighted(false);
    }

    private void OnDestroy()
    {
        SetHighlighted(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted)
            return;

        isHighlighted = highlighted;

        if (highlighted)
            ApplyOutline();
        else
            RemoveOutline();
    }

    private void CacheRenderers()
    {
        if (targetRenderers != null && targetRenderers.Length > 0)
            return;

        targetRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void CacheOriginalMaterials()
    {
        if (targetRenderers == null)
            CacheRenderers();

        originalSharedMaterials = new Material[targetRenderers.Length][];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null)
                continue;

            originalSharedMaterials[i] = targetRenderers[i].sharedMaterials;
        }
    }

    private void ApplyOutline()
    {
        if (outlineMaterial == null)
        {
            if (debugLogs)
                Debug.LogWarning("[RiverOutline] Missing outline material on " + name);

            return;
        }

        if (targetRenderers == null || targetRenderers.Length == 0)
            CacheRenderers();

        if (originalSharedMaterials == null ||
            originalSharedMaterials.Length != targetRenderers.Length)
        {
            CacheOriginalMaterials();
        }

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer renderer = targetRenderers[i];

            if (renderer == null)
                continue;

            Material[] originalMaterials = originalSharedMaterials[i];

            if (originalMaterials == null)
                continue;

            List<Material> materials = new List<Material>(originalMaterials);

            if (!materials.Contains(outlineMaterial))
                materials.Add(outlineMaterial);

            renderer.sharedMaterials = materials.ToArray();
        }
    }

    private void RemoveOutline()
    {
        if (targetRenderers == null ||
            originalSharedMaterials == null)
            return;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer renderer = targetRenderers[i];

            if (renderer == null)
                continue;

            if (i >= originalSharedMaterials.Length)
                continue;

            if (originalSharedMaterials[i] == null)
                continue;

            renderer.sharedMaterials = originalSharedMaterials[i];
        }
    }
}