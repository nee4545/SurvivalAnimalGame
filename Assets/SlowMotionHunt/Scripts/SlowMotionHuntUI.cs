using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Speedometer-style timing UI for the Slow Motion Hunt mechanic.
/// Attach this to the SlowMotionHuntRoot object.
/// </summary>
public class SlowMotionHuntUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject root;
    public RectTransform arrow;
    public RectTransform track;
    public RectTransform greenZone;

    [Header("Arrow Settings")]
    [Tooltip("Higher value = faster arrow movement.")]
    public float arrowSpeed = 1.8f;

    [Header("Optional Visual Feedback")]
    public Image arrowImage;
    public Image greenZoneImage;

    [Header("Tap Button")]
    public Button tapButton;

    public event Action TapRequested;

    private float t;
    private bool active;

    private void Awake()
    {
        if (tapButton)
            tapButton.onClick.AddListener(HandleTapButtonClicked);
    }

    private void HandleTapButtonClicked()
    {
        TapRequested?.Invoke();
    }

    public void SubscribeTap(Action callback)
    {
        TapRequested -= callback;
        TapRequested += callback;
    }

    public void UnsubscribeTap(Action callback)
    {
        TapRequested -= callback;
    }

    private void OnDestroy()
    {
        if (tapButton)
            tapButton.onClick.RemoveListener(HandleTapButtonClicked);

        TapRequested = null;
    }

    public void Show()
    {
        active = true;
        t = 0f;

        if (root)
            root.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public void Hide()
    {
        active = false;

        if (root)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void Tick()
    {
        if (!active || arrow == null || track == null)
            return;

        t += Time.unscaledDeltaTime * arrowSpeed;

        float pingPong = Mathf.PingPong(t, 1f);
        float halfWidth = track.rect.width * 0.5f;
        float x = Mathf.Lerp(-halfWidth, halfWidth, pingPong);

        arrow.anchoredPosition = new Vector2(x, arrow.anchoredPosition.y);
    }

    public bool IsArrowInGreenZone()
    {
        if (arrow == null || greenZone == null)
            return false;

        float arrowX = arrow.anchoredPosition.x;
        float greenMin = greenZone.anchoredPosition.x - greenZone.rect.width * 0.5f;
        float greenMax = greenZone.anchoredPosition.x + greenZone.rect.width * 0.5f;

        return arrowX >= greenMin && arrowX <= greenMax;
    }

    public float GetArrowNormalizedPosition()
    {
        if (arrow == null || track == null || track.rect.width <= 0f)
            return 0.5f;

        float halfWidth = track.rect.width * 0.5f;
        return Mathf.InverseLerp(-halfWidth, halfWidth, arrow.anchoredPosition.x);
    }
}
