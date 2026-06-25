using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HomeBaseIndicator : MonoBehaviour
{
    [Header("World References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform homeBase;
    [SerializeField] private Camera targetCamera;

    [Header("UI References")]
    [SerializeField] private GameObject indicatorVisual;
    [SerializeField] private RectTransform arrowRect;
    [SerializeField] private Image arrowImage;
    [SerializeField] private TextMeshProUGUI distanceText;

    [Header("Visibility")]
    [SerializeField, Min(0f)] private float hideDistance = 8f;
    [SerializeField] private bool hideWhenBaseIsVisible;

    [Header("Rotation")]
    [Tooltip("Adjust this if the arrow artwork does not naturally point upward.")]
    [SerializeField] private float arrowRotationOffset;

    public Transform Player
    {
        get => player;
        set => player = value;
    }

    public Transform HomeBase
    {
        get => homeBase;
        set => homeBase = value;
    }

    public Camera TargetCamera
    {
        get => targetCamera;
        set => targetCamera = value;
    }

    public Image ArrowImage => arrowImage;

    private void Awake()
    {
        ResolveMissingReferences();
        SetVisible(false);
    }

    private void OnEnable()
    {
        ResolveMissingReferences();
    }

    private void LateUpdate()
    {
        ResolveMissingReferences();

        if (player == null || homeBase == null || targetCamera == null || arrowRect == null)
        {
            SetVisible(false);
            return;
        }

        Vector3 toBase = homeBase.position - player.position;
        toBase.y = 0f;

        float distance = toBase.magnitude;

        if (distance <= hideDistance)
        {
            SetVisible(false);
            return;
        }

        if (hideWhenBaseIsVisible && IsHomeVisibleOnScreen())
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        UpdateArrowRotation(toBase);
        UpdateDistance(distance);
    }

    private void ResolveMissingReferences()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }
    }

    private void UpdateArrowRotation(Vector3 directionToBase)
    {
        if (directionToBase.sqrMagnitude < 0.001f)
            return;

        Vector3 cameraForward = targetCamera.transform.forward;
        Vector3 cameraRight = targetCamera.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();
        directionToBase.Normalize();

        float forwardAmount = Vector3.Dot(cameraForward, directionToBase);
        float rightAmount = Vector3.Dot(cameraRight, directionToBase);
        float angle = Mathf.Atan2(rightAmount, forwardAmount) * Mathf.Rad2Deg;

        arrowRect.localRotation = Quaternion.Euler(0f, 0f, -angle + arrowRotationOffset);
    }

    private void UpdateDistance(float distance)
    {
        if (distanceText != null)
            distanceText.text = $"{Mathf.RoundToInt(distance)}m";
    }

    private bool IsHomeVisibleOnScreen()
    {
        Vector3 viewportPosition = targetCamera.WorldToViewportPoint(homeBase.position);

        return viewportPosition.z > 0f &&
               viewportPosition.x >= 0f && viewportPosition.x <= 1f &&
               viewportPosition.y >= 0f && viewportPosition.y <= 1f;
    }

    private void SetVisible(bool visible)
    {
        if (indicatorVisual != null && indicatorVisual.activeSelf != visible)
            indicatorVisual.SetActive(visible);
    }
}
