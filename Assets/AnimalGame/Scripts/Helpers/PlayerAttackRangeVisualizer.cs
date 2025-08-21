using UnityEngine;

[ExecuteAlways]
public class PlayerAttackRangeVisualizer : MonoBehaviour
{
    public CCActor player;          // assign in inspector, or auto-find by tag
    public float yThickness = 0.1f; // how flat the cylinder is

    void AutoLink()
    {
        if (!player)
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged) player = tagged.GetComponent<CCActor>();
        }
    }

    void LateUpdate()
    {
        AutoLink();
        if (!player) return;

        // Player’s attackRange is distance to target center
        float r = player.attackHitRadius;

        // Parent scaling compensation
        var parent = transform.parent;
        Vector3 parentLossy = parent ? parent.lossyScale : Vector3.one;

        float desiredWorldDiameter = r;
        float sx = desiredWorldDiameter / Mathf.Max(parentLossy.x, 1e-4f);
        float sz = desiredWorldDiameter / Mathf.Max(parentLossy.z, 1e-4f);

        transform.localScale = new Vector3(sx, yThickness, sz);

        // keep ring sitting on ground plane
        transform.localPosition = new Vector3(0f, yThickness * 1.5f, 0f);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!player) AutoLink();
        if (!player) return;

        Vector3 p = transform.parent ? transform.parent.position : transform.position;
        p.y += 0.02f;
        UnityEditor.Handles.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        UnityEditor.Handles.DrawWireDisc(p, Vector3.up, player.attackRange);
    }
#endif
}
