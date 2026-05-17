using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public Transform target;

    [Header("Follow")]
    public float followSpeed = 10f;
    public float rotationReturnSpeed = 10f;

    [Header("Hunt Zoom")]
    public float huntZoomForwardAmount = 2.2f;
    public float huntZoomInSpeed = 8f;
    public float huntZoomOutSpeed = 6f;

    [Tooltip("Optional FOV zoom. Set this to 0 if you only want position zoom.")]
    public float huntFov = 45f;

    [Header("Temporary Look Target")]
    public Vector3 temporaryLookOffset = new Vector3(0f, 1.2f, 0f);
    public float temporaryLookSpeed = 8f;

    private Vector3 baseOffset;
    private Quaternion baseRotation;

    private bool huntZoomActive;
    private float originalFov;
    private float currentZoomAmount;

    private Transform temporaryLookTarget;
    private bool hasTemporaryLookTarget;

    private void Start()
    {
        if (!cam)
            cam = GetComponent<Camera>();

        if (!target)
        {
            Debug.LogWarning("[CameraSystem] Target is not assigned.");
            return;
        }

        baseOffset = transform.position - target.position;
        baseRotation = transform.rotation;

        if (cam)
            originalFov = cam.fieldOfView;
    }

    private void LateUpdate()
    {
        if (!target)
            return;

        UpdatePosition();
        UpdateRotation();
        UpdateFov();
    }

    private void UpdatePosition()
    {
        float targetZoom = huntZoomActive ? huntZoomForwardAmount : 0f;
        float speed = huntZoomActive ? huntZoomInSpeed : huntZoomOutSpeed;

        currentZoomAmount = Mathf.Lerp(
            currentZoomAmount,
            targetZoom,
            Time.unscaledDeltaTime * speed
        );

        Vector3 zoomOffset = transform.forward * currentZoomAmount;
        Vector3 desiredPosition = target.position + baseOffset + zoomOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            Time.unscaledDeltaTime * followSpeed
        );
    }

    private void UpdateRotation()
    {
        Quaternion desiredRotation = baseRotation;

        if (hasTemporaryLookTarget && temporaryLookTarget)
        {
            Vector3 lookPoint = temporaryLookTarget.position + temporaryLookOffset;
            Vector3 dir = lookPoint - transform.position;

            if (dir.sqrMagnitude > 0.01f)
                desiredRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        float speed = hasTemporaryLookTarget ? temporaryLookSpeed : rotationReturnSpeed;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            Time.unscaledDeltaTime * speed
        );
    }

    private void UpdateFov()
    {
        if (!cam || huntFov <= 0f)
            return;

        float speed = huntZoomActive ? huntZoomInSpeed : huntZoomOutSpeed;
        float targetFov = huntZoomActive ? huntFov : originalFov;

        cam.fieldOfView = Mathf.Lerp(
            cam.fieldOfView,
            targetFov,
            Time.unscaledDeltaTime * speed
        );
    }

    public void SetHuntZoom(bool enabled)
    {
        huntZoomActive = enabled;
    }

    public void SetTemporaryLookTarget(Transform lookTarget)
    {
        temporaryLookTarget = lookTarget;
        hasTemporaryLookTarget = lookTarget != null;
    }

    public void ClearTemporaryLookTarget()
    {
        temporaryLookTarget = null;
        hasTemporaryLookTarget = false;
    }

    public void ForceResetZoom()
    {
        huntZoomActive = false;
        currentZoomAmount = 0f;
        ClearTemporaryLookTarget();

        transform.rotation = baseRotation;

        if (cam)
            cam.fieldOfView = originalFov;
    }
}