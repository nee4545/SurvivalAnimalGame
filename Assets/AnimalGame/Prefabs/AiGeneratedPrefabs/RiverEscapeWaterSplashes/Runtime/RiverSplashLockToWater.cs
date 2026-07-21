using UnityEngine;

/// <summary>
/// Keeps a splash particle effect locked to the river water height while following a rideable object.
/// Attach this to the splash prefab root.
/// </summary>
public class RiverSplashLockToWater : MonoBehaviour
{
    [Header("Follow")]
    public Transform followTarget;
    public bool useTargetLocalOffset = true;
    public Vector3 localOffset = new Vector3(0f, 0f, -0.45f);

    [Header("Water Height")]
    public bool lockToWaterY = true;
    public float waterY = 0f;
    public float heightOffset = 0.04f;

    [Header("Rotation")]
    public bool rotateWithTarget = true;
    public Transform riverDirectionReference;
    public bool faceRiverDirection = false;
    public Vector3 rotationEulerOffset = Vector3.zero;

    private void LateUpdate()
    {
        UpdatePosition();
        UpdateRotation();
    }

    private void UpdatePosition()
    {
        if (followTarget == null)
            return;

        Vector3 position = useTargetLocalOffset
            ? followTarget.TransformPoint(localOffset)
            : followTarget.position + localOffset;

        if (lockToWaterY)
            position.y = waterY + heightOffset;

        transform.position = position;
    }

    private void UpdateRotation()
    {
        Vector3 forward = Vector3.zero;

        if (faceRiverDirection && riverDirectionReference != null)
        {
            forward = riverDirectionReference.forward;
        }
        else if (rotateWithTarget && followTarget != null)
        {
            forward = followTarget.forward;
        }

        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up) * Quaternion.Euler(rotationEulerOffset);
    }

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
        UpdatePosition();
        UpdateRotation();
    }
}
