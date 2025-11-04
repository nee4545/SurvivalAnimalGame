using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform _rt;
    Rect _lastSafeArea;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void Update()
    {
        // In case of orientation change / resize
        if (_lastSafeArea != Screen.safeArea)
            ApplySafeArea();
    }

    void ApplySafeArea()
    {
        Rect safe = Screen.safeArea;
        _lastSafeArea = safe;

        Vector2 anchorMin = safe.position;
        Vector2 anchorMax = safe.position + safe.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _rt.anchorMin = anchorMin;
        _rt.anchorMax = anchorMax;
    }
}
