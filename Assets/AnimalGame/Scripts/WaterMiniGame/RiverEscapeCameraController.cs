using UnityEngine;

public class RiverEscapeCameraController : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform riverDirectionReference;

    [Header("Camera Offset")]
    public Vector3 localCameraOffset = new Vector3(0f, 9f, -10f);

    [Tooltip("Extra look target height above the player.")]
    public float lookHeight = 1.2f;

    [Header("Follow")]
    public float forwardFollowSharpness = 10f;
    public float sideFollowSharpness = 4f;
    public float verticalFollowSharpness = 8f;

    [Header("Look")]
    public float lookSharpness = 12f;

    [Header("Optional Screen Framing")]
    [Tooltip("Pushes camera slightly behind the player so player appears lower on screen.")]
    public float playerScreenAnchorBackwardOffset = 0f;

    [Header("Runtime")]
    public bool followEnabled = true;

    private Vector3 followPosition;

    private void OnEnable()
    {
        if (player != null)
            SnapToPlayer();
    }

    private void LateUpdate()
    {
        if (!followEnabled || player == null)
            return;

        UpdateCameraFollow();
    }

    public void ApplyExternalRecenterShift(Vector3 shift)
    {
        followPosition += shift;
        transform.position += shift;
    }

    public void ForceSyncFollowPosition()
    {
        followPosition = transform.position;
    }

    public void SnapToPlayer()
    {
        Vector3 targetPosition = GetTargetCameraPosition();

        followPosition = targetPosition;
        transform.position = targetPosition;

        LookAtPlayer(true);
    }

    private void UpdateCameraFollow()
    {
        Vector3 targetPosition = GetTargetCameraPosition();

        Vector3 riverForward = GetRiverForward();
        Vector3 riverRight = GetRiverRight();

        Vector3 delta = targetPosition - followPosition;

        float forwardAmount = Vector3.Dot(delta, riverForward);
        float sideAmount = Vector3.Dot(delta, riverRight);
        float verticalAmount = delta.y;

        float forwardT =
            1f - Mathf.Exp(-forwardFollowSharpness * Time.deltaTime);

        float sideT =
            1f - Mathf.Exp(-sideFollowSharpness * Time.deltaTime);

        float verticalT =
            1f - Mathf.Exp(-verticalFollowSharpness * Time.deltaTime);

        followPosition += riverForward * forwardAmount * forwardT;
        followPosition += riverRight * sideAmount * sideT;
        followPosition.y += verticalAmount * verticalT;

        transform.position = followPosition;

        LookAtPlayer(false);
    }

    private Vector3 GetTargetCameraPosition()
    {
        Vector3 riverForward = GetRiverForward();
        Vector3 riverRight = GetRiverRight();

        Vector3 target =
            player.position +
            riverRight * localCameraOffset.x +
            Vector3.up * localCameraOffset.y -
            riverForward * Mathf.Abs(localCameraOffset.z);

        if (playerScreenAnchorBackwardOffset != 0f)
            target -= riverForward * playerScreenAnchorBackwardOffset;

        return target;
    }

    private void LookAtPlayer(bool instant)
    {
        Vector3 lookTarget =
            player.position +
            Vector3.up * lookHeight;

        Vector3 lookDirection = lookTarget - transform.position;

        if (lookDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(lookDirection.normalized, Vector3.up);

        if (instant)
        {
            transform.rotation = targetRotation;
        }
        else
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                1f - Mathf.Exp(-lookSharpness * Time.deltaTime)
            );
        }
    }

    private Vector3 GetRiverForward()
    {
        Vector3 forward =
            riverDirectionReference != null
                ? riverDirectionReference.forward
                : Vector3.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        return forward.normalized;
    }

    private Vector3 GetRiverRight()
    {
        Vector3 right =
            riverDirectionReference != null
                ? riverDirectionReference.right
                : Vector3.right;

        right.y = 0f;

        if (right.sqrMagnitude < 0.001f)
            right = Vector3.right;

        return right.normalized;
    }
}