using UnityEngine;

[ExecuteAlways]
public class PlayerHitRangeRing : MonoBehaviour
{
    public CuteAnimalAI ai;                   // auto-finds parent if left empty
    public Transform player;                  // auto-finds Player tag if left empty

    [Tooltip("Pull from CCActor if present; else use fallbackRange.")]
    public bool readFromCCActor = true;

    [Tooltip("If CCActor not found or read disabled, use this.")]
    public float fallbackPlayerAttackRange = 2f;

    [Tooltip("Should the ring represent center distance or surface distance to the animal?")]
    public Mode distanceMode = Mode.SurfaceDistance;
    public enum Mode { CenterDistance, SurfaceDistance }

    [Tooltip("Optional manual collider (if your animal uses multiple colliders). Leave empty to use the biggest attached collider.")]
    public Collider animalColliderOverride;

    [Tooltip("Visual thickness (local Y scale) of the ring mesh.")]
    public float yThickness = 0.1f;

    [Tooltip("Small padding so the ring doesn’t look too tight.")]
    public float padding = 0.05f;

    // --- CCActor hook (adjust names to match your script if needed) ---
    private float GetPlayerAttackRange()
    {
        if (!player) return fallbackPlayerAttackRange;

        if (readFromCCActor)
        {
            // Try to get from your CCActor, adjust names if your field is different.
            var cc = player.GetComponent<CCActor>();
            if (cc != null)
            {
                // Try the common suspects. Rename if your CCActor uses a different field/property.
                // Example assumptions:
                //  - public float attackRange;
                //  - or public float meleeRange;
                //  - or a property CurrentAttackRange
                try
                {
                    var t = cc.GetType();
                    var f = t.GetField("attackRange") ?? t.GetField("meleeRange");
                    if (f != null && f.FieldType == typeof(float))
                        return Mathf.Max(0f, (float)f.GetValue(cc));

                    var p = t.GetProperty("CurrentAttackRange");
                    if (p != null && p.PropertyType == typeof(float))
                        return Mathf.Max(0f, (float)p.GetValue(cc));
                }
                catch { /* fall back */ }
            }
        }

        return Mathf.Max(0f, fallbackPlayerAttackRange);
    }

    private float GetAnimalHorizontalRadius()
    {
        Collider chosen = animalColliderOverride;
        if (!chosen)
        {
            var cols = GetComponentsInParent<Collider>(true);
            float best = 0f;
            Collider bestCol = null;
            foreach (var c in cols)
            {
                if (!c.enabled) continue;
                var e = c.bounds.extents;
                // Horizontal “radius” from bounds (approx)
                float r = Mathf.Max(e.x, e.z);
                if (r > best) { best = r; bestCol = c; }
            }
            chosen = bestCol;
        }

        if (!chosen) return 0.5f; // safe default
        var ext = chosen.bounds.extents;
        return Mathf.Max(ext.x, ext.z);
    }

    private void AutoLink()
    {
        if (!ai) ai = GetComponentInParent<CuteAnimalAI>();
        if (!player)
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged) player = tagged.transform;
        }
    }

    private void UpdateRing()
    {
        AutoLink();
        if (!ai) return;

        // Compute the ring radius we want in world space
        float playerRange = GetPlayerAttackRange();
        float animalR = (distanceMode == Mode.SurfaceDistance) ? GetAnimalHorizontalRadius() : 0f;

        float visualRadius = playerRange + animalR + padding;
        visualRadius = Mathf.Max(0.01f, visualRadius);

        // The Unity Cylinder base has diameter 1 (radius 0.5) before scaling.
        // WorldDiameterX = localScale.x * parentLossy.x. We want WorldDiameter = 2 * visualRadius.
        var parent = transform.parent;
        Vector3 parentLossy = parent ? parent.lossyScale : Vector3.one;

        float worldDiameter = 2f * visualRadius;
        float sx = worldDiameter / Mathf.Max(parentLossy.x, 1e-4f);
        float sz = worldDiameter / Mathf.Max(parentLossy.z, 1e-4f);

        transform.localScale = new Vector3(sx, yThickness, sz);

        // Sit it on the ground relative to parent. Adjust if your meshes pivot differently.
        transform.localPosition = new Vector3(0f, yThickness * 0.5f, 0f);
    }

    void LateUpdate() { UpdateRing(); }
    void OnValidate() { UpdateRing(); }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!ai) AutoLink();
        if (!ai) return;

        float playerRange = GetPlayerAttackRange();
        float animalR = (distanceMode == Mode.SurfaceDistance) ? GetAnimalHorizontalRadius() : 0f;
        float r = playerRange + animalR + padding;

        var p = ai.transform.position;
        p.y += 0.03f;
        UnityEditor.Handles.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        UnityEditor.Handles.DrawWireDisc(p, Vector3.up, r);
    }
#endif
}
