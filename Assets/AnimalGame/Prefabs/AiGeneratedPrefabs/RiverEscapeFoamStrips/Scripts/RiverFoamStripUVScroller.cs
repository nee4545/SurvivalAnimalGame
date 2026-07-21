using UnityEngine;

[ExecuteAlways]
public class RiverFoamStripUVScroller : MonoBehaviour
{
    [Header("References")]
    public Renderer targetRenderer;

    [Header("Texture Scroll")]
    public bool scroll = true;
    public Vector2 scrollSpeed = new Vector2(0.35f, 0f);

    [Tooltip("Creates a material instance so this strip can scroll independently.")]
    public bool useMaterialInstance = true;

    [Header("Tint")]
    public Color tint = new Color(0.85f, 1f, 0.95f, 0.65f);

    private Material runtimeMaterial;
    private Vector2 offset;
    private int texturePropertyId;
    private int colorPropertyId;

    private void Awake()
    {
        Setup();
    }

    private void OnEnable()
    {
        Setup();
    }

    private void Update()
    {
        if (targetRenderer == null)
            return;

        Material mat = GetMaterial();

        if (mat == null)
            return;

        if (scroll)
        {
            offset += scrollSpeed * Time.deltaTime;
            offset.x = Mathf.Repeat(offset.x, 1f);
            offset.y = Mathf.Repeat(offset.y, 1f);

            mat.SetTextureOffset(texturePropertyId, offset);
        }

        if (mat.HasProperty(colorPropertyId))
            mat.SetColor(colorPropertyId, tint);
    }

    public void SetScrollDirection(Vector2 direction, float speed)
    {
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.right;

        scrollSpeed = direction.normalized * speed;
    }

    private void Setup()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        Material mat = GetMaterial();

        if (mat == null)
            return;

        texturePropertyId = GetTexturePropertyId(mat);
        colorPropertyId = GetColorPropertyId(mat);

        if (mat.HasProperty(colorPropertyId))
            mat.SetColor(colorPropertyId, tint);
    }

    private Material GetMaterial()
    {
        if (targetRenderer == null)
            return null;

        if (!useMaterialInstance)
            return targetRenderer.sharedMaterial;

        if (runtimeMaterial == null)
        {
            runtimeMaterial = Application.isPlaying
                ? targetRenderer.material
                : targetRenderer.sharedMaterial;
        }

        return runtimeMaterial;
    }

    private int GetTexturePropertyId(Material mat)
    {
        if (mat.HasProperty("_BaseMap"))
            return Shader.PropertyToID("_BaseMap");

        return Shader.PropertyToID("_MainTex");
    }

    private int GetColorPropertyId(Material mat)
    {
        if (mat.HasProperty("_BaseColor"))
            return Shader.PropertyToID("_BaseColor");

        return Shader.PropertyToID("_Color");
    }
}
