using UnityEngine;

public class AnimalSkinVariant : MonoBehaviour
{
    [Header("Renderer")]
    public Renderer targetRenderer;

    [Header("Skin Variants")]
    public Material[] skinMaterials;

    [Header("Settings")]
    public bool applyRandomSkinOnStart = true;

    [Tooltip("Used only if random skin is disabled.")]
    public int selectedSkinIndex = 0;

    [Tooltip("Apply to all materials on the renderer.")]
    public bool replaceAllMaterials = false;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();
    }

    private void Start()
    {
        ApplySkin();
    }

    public void ApplySkin()
    {
        if (targetRenderer == null)
            return;

        if (skinMaterials == null || skinMaterials.Length == 0)
            return;

        int index = applyRandomSkinOnStart
            ? Random.Range(0, skinMaterials.Length)
            : Mathf.Clamp(selectedSkinIndex, 0, skinMaterials.Length - 1);

        Material chosenMaterial = skinMaterials[index];

        if (replaceAllMaterials)
        {
            Material[] mats = targetRenderer.materials;

            for (int i = 0; i < mats.Length; i++)
                mats[i] = chosenMaterial;

            targetRenderer.materials = mats;
        }
        else
        {
            targetRenderer.material = chosenMaterial;
        }
    }

    public void ApplySkin(int index)
    {
        if (targetRenderer == null)
            return;

        if (skinMaterials == null || skinMaterials.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, skinMaterials.Length - 1);

        Material chosenMaterial = skinMaterials[index];

        if (replaceAllMaterials)
        {
            Material[] mats = targetRenderer.materials;

            for (int i = 0; i < mats.Length; i++)
                mats[i] = chosenMaterial;

            targetRenderer.materials = mats;
        }
        else
        {
            targetRenderer.material = chosenMaterial;
        }
    }
}