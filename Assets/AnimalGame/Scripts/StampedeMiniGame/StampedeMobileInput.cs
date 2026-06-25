using UnityEngine;
using UnityEngine.EventSystems;

public class StampedeMobileInput : MonoBehaviour
{
    [Header("References")]
    public StampedeLaneController laneController;

    [Header("Swipe")]
    public float minSwipeDistance = 80f;
    public float maxSwipeTime = 0.45f;
    public bool ignoreVerticalSwipes = true;

    [Header("Double Tap Jump")]
    public float doubleTapMaxDelay = 0.28f;
    public float maxTapMovement = 35f;

    [Header("UI")]
    public bool ignoreTouchesOverUI = true;

    [Header("Debug")]
    public bool debugLogs;

    private Vector2 touchStartPosition;
    private float touchStartTime;

    private float lastTapTime = -999f;
    private Vector2 lastTapPosition;

    private bool trackingTouch;

    private void Update()
    {
        if (laneController == null)
            return;

        if (!laneController.IsActive)
            return;

#if UNITY_EDITOR || UNITY_STANDALONE
        // Mobile input not needed in editor if you are already using A/D/Space.
        // Remove this return if you want to test swipe with mouse later.
#endif

        HandleTouchInput();
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount <= 0)
            return;

        Touch touch = Input.GetTouch(0);

        if (ignoreTouchesOverUI && IsPointerOverUI(touch.fingerId))
            return;

        switch (touch.phase)
        {
            case TouchPhase.Began:
                trackingTouch = true;
                touchStartPosition = touch.position;
                touchStartTime = Time.time;
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (!trackingTouch)
                    return;

                trackingTouch = false;

                Vector2 endPosition = touch.position;
                float touchDuration = Time.time - touchStartTime;
                Vector2 delta = endPosition - touchStartPosition;

                bool wasSwipe = TryHandleSwipe(delta, touchDuration);

                if (!wasSwipe)
                    TryHandleTap(endPosition, delta);

                break;
        }
    }

    private bool TryHandleSwipe(Vector2 delta, float duration)
    {
        if (duration > maxSwipeTime)
            return false;

        if (delta.magnitude < minSwipeDistance)
            return false;

        float absX = Mathf.Abs(delta.x);
        float absY = Mathf.Abs(delta.y);

        // Swipe up = jump
        if (delta.y > 0f && absY > absX)
        {
            laneController.TryJump();

            if (debugLogs)
                Debug.Log("[StampedeMobileInput] Swipe Up Jump");

            return true;
        }

        // Horizontal swipes = lane movement
        if (absX > absY)
        {
            if (delta.x < 0f)
            {
                laneController.MoveLane(-1);

                if (debugLogs)
                    Debug.Log("[StampedeMobileInput] Swipe Left");

                return true;
            }

            if (delta.x > 0f)
            {
                laneController.MoveLane(1);

                if (debugLogs)
                    Debug.Log("[StampedeMobileInput] Swipe Right");

                return true;
            }
        }

        return false;
    }

    private void TryHandleTap(Vector2 tapPosition, Vector2 delta)
    {
        if (delta.magnitude > maxTapMovement)
            return;

        float timeSinceLastTap = Time.time - lastTapTime;
        float distanceFromLastTap = Vector2.Distance(tapPosition, lastTapPosition);

        if (timeSinceLastTap <= doubleTapMaxDelay && distanceFromLastTap <= maxTapMovement * 2f)
        {
            laneController.TryJump();

            lastTapTime = -999f;

            if (debugLogs)
                Debug.Log("[StampedeMobileInput] Double Tap Jump");

            return;
        }

        lastTapTime = Time.time;
        lastTapPosition = tapPosition;
    }

    private bool IsPointerOverUI(int fingerId)
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject(fingerId);
    }
}