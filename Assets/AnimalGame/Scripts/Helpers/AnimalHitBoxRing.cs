using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class AnimalHitBoxRing : MonoBehaviour
{
    [Tooltip("If left empty, auto-finds the CuteAnimalAI on the parent.")]
    public CuteAnimalAI ai;

    [Tooltip("Visual thickness (local Y scale) of the cylinder ring.")]
    public float yThickness = 0.1f;

    [Tooltip("Optional small visual padding so the ring isn’t too tight.")]
    public float padding = 0.0f;

    void Reset()
    {
        if (!ai) ai = GetComponentInParent<CuteAnimalAI>();
    }

    void OnValidate() { UpdateRing(); }
    void LateUpdate() { UpdateRing(); }

    private void UpdateRing()
    {
        if (!ai) ai = GetComponentInParent<CuteAnimalAI>();
        if (!ai) return;

        // True hit condition in your code is center distance <= ai.attackRange
        float r = Mathf.Max(0f, ai.attackRange + padding);

        // Unity cylinder base diameter = 1 → worldDiameter = localScale.x * parentLossy.x
        var parent = transform.parent;
        Vector3 parentLossy = parent ? parent.lossyScale : Vector3.one;

        float desiredWorldDiameter = 2f * r;
        float sx = desiredWorldDiameter / Mathf.Max(parentLossy.x, 1e-4f);
        float sz = desiredWorldDiameter / Mathf.Max(parentLossy.z, 1e-4f);

        transform.localScale = new Vector3(sx, yThickness, sz);

        // Keep it resting on ground at parent origin
        transform.localPosition = new Vector3(0f, yThickness * 0.5f, 0f);
        // Match parent rotation so it stays flat
        transform.localRotation = Quaternion.identity;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!ai) ai = GetComponentInParent<CuteAnimalAI>();
        if (!ai) return;

        // Draw the exact ring your attack uses (center distance)
        Vector3 p = (ai ? ai.transform.position : transform.position);
        p.y += 0.02f;
        UnityEditor.Handles.color = new Color(1f, 0.6f, 0.2f, 0.9f);
        UnityEditor.Handles.DrawWireDisc(p, Vector3.up, ai.attackRange);
    }
#endif
}
