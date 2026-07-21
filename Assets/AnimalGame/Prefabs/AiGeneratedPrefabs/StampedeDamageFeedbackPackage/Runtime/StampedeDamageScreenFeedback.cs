using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen edge damage feedback for the Stampede mini game.
/// Attach this to a full-screen UI prefab with a CanvasGroup and Image layers.
/// Call PlayHitFeedback() when the player loses a life.
/// </summary>
public class StampedeDamageScreenFeedback : MonoBehaviour
{
    public static StampedeDamageScreenFeedback Instance { get; private set; }

    [Header("References")]
    public CanvasGroup rootGroup;
    public Image redVignetteImage;
    public Image bloodBorderImage;

    [Header("Timing")]
    public bool useUnscaledTime = true;
    public float popInDuration = 0.05f;
    public float holdDuration = 0.08f;
    public float fadeOutDuration = 0.45f;

    [Header("Intensity")]
    [Range(0f, 1f)] public float vignettePeakAlpha = 0.55f;
    [Range(0f, 1f)] public float bloodPeakAlpha = 0.9f;
    [Range(0f, 1f)] public float pulseScaleAmount = 0.035f;

    [Header("Optional Impact Flash")]
    public bool useCenterFlash = true;
    public Image centerFlashImage;
    [Range(0f, 1f)] public float centerFlashPeakAlpha = 0.18f;

    private Coroutine routine;
    private Vector3 originalScale;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        originalScale = transform.localScale;

        if (rootGroup == null)
            rootGroup = GetComponent<CanvasGroup>();

        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayHitFeedback()
    {
        if (!isActiveAndEnabled)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(PlayRoutine());
    }

    public void HideImmediate()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        SetLayerAlpha(redVignetteImage, 0f);
        SetLayerAlpha(bloodBorderImage, 0f);
        SetLayerAlpha(centerFlashImage, 0f);

        if (rootGroup != null)
        {
            rootGroup.alpha = 1f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }

        transform.localScale = originalScale == Vector3.zero ? Vector3.one : originalScale;
    }

    private IEnumerator PlayRoutine()
    {
        float timer = 0f;
        float popTime = Mathf.Max(0.001f, popInDuration);
        float fadeTime = Mathf.Max(0.001f, fadeOutDuration);
        Vector3 baseScale = originalScale == Vector3.zero ? Vector3.one : originalScale;
        Vector3 pulseScale = baseScale * (1f + pulseScaleAmount);

        // Pop in fast.
        while (timer < popTime)
        {
            timer += DeltaTime();
            float t = Mathf.Clamp01(timer / popTime);
            float eased = EaseOut(t);

            SetLayerAlpha(redVignetteImage, Mathf.Lerp(0f, vignettePeakAlpha, eased));
            SetLayerAlpha(bloodBorderImage, Mathf.Lerp(0f, bloodPeakAlpha, eased));

            if (useCenterFlash)
                SetLayerAlpha(centerFlashImage, Mathf.Lerp(0f, centerFlashPeakAlpha, eased));

            transform.localScale = Vector3.Lerp(baseScale, pulseScale, eased);
            yield return null;
        }

        SetLayerAlpha(redVignetteImage, vignettePeakAlpha);
        SetLayerAlpha(bloodBorderImage, bloodPeakAlpha);

        if (useCenterFlash)
            SetLayerAlpha(centerFlashImage, centerFlashPeakAlpha);

        transform.localScale = pulseScale;

        if (holdDuration > 0f)
            yield return Wait(holdDuration);

        // Fade away smoothly.
        timer = 0f;
        while (timer < fadeTime)
        {
            timer += DeltaTime();
            float t = Mathf.Clamp01(timer / fadeTime);
            float eased = EaseIn(t);

            SetLayerAlpha(redVignetteImage, Mathf.Lerp(vignettePeakAlpha, 0f, eased));
            SetLayerAlpha(bloodBorderImage, Mathf.Lerp(bloodPeakAlpha, 0f, eased));

            if (useCenterFlash)
                SetLayerAlpha(centerFlashImage, Mathf.Lerp(centerFlashPeakAlpha, 0f, eased));

            transform.localScale = Vector3.Lerp(pulseScale, baseScale, eased);
            yield return null;
        }

        HideImmediate();
    }

    private float DeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private YieldInstruction Wait(float duration)
    {
        return new WaitForSeconds(duration);
    }

    private static float EaseOut(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private static float EaseIn(float t)
    {
        return t * t;
    }

    private static void SetLayerAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color c = image.color;
        c.a = Mathf.Clamp01(alpha);
        image.color = c;
    }
}
