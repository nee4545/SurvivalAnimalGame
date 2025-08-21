using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackRing : MonoBehaviour
{
    [Tooltip("If left empty, will auto-find on parent.")]
    public CuteAnimalAI ai;

    [Tooltip("Visual thickness (Y scale) of the ring mesh.")]
    public float yThickness = 0.1f;

    void Reset()
    {
        if (!ai) ai = GetComponentInParent<CuteAnimalAI>();
    }

    void OnValidate() { UpdateRing(); }
    void LateUpdate() { UpdateRing(); }

    void UpdateRing()
    {
        if (!ai) { ai = GetComponentInParent<CuteAnimalAI>(); if (!ai) return; }

        // Desired world diameter must equal 2 * attackRange.
        float desiredWorldDiameterX = ai.attackRange * 2f;
        float desiredWorldDiameterZ = ai.attackRange * 2f;

        // Compensate for parent scaling so the cylinder's *local* scale produces that world diameter.
        var parent = transform.parent;
        Vector3 parentLossy = parent ? parent.lossyScale : Vector3.one;

        // Base cylinder diameter is 1, so worldDiameter = localScale.x * parentLossy.x
        float sx = desiredWorldDiameterX / Mathf.Max(parentLossy.x, 1e-4f);
        float sz = desiredWorldDiameterZ / Mathf.Max(parentLossy.z, 1e-4f);

        transform.localScale = new Vector3(sx, yThickness, sz);

        // Sit the ring on the ground (assuming parent origin is at ground height).
        transform.localPosition = new Vector3(0f, yThickness * 0.5f, 0f);
    }
}
