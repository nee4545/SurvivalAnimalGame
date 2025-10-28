using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class HungerBarUI : MonoBehaviour
{
    [Header("Source")]
    public CCActor source;                  // drag your player here (or leave null to auto-find)

    [Header("UI")]
    public Slider slider;                   // auto-fills from this GameObject if left null
    public Image fillImage;                 // optional: assign the Fill image of the slider
    public Gradient fillGradient;           // optional: color over [0..1]

    [Header("Update")]
    [Tooltip("0 = instant, higher = smoother")]
    public float lerpSpeed = 12f;

    [Header("Warning (optional)")]
    [Range(0f, 1f)] public float warningThreshold = 0.15f;
    public bool pulseWhenLow = true;        // subtle attention when starving
    public float pulseSpeed = 6f;           // how fast the pulse is
    public float pulseScale = 1.05f;        // how large the pulse scales to

    Vector3 _baseScale;

    void Awake()
    {
        if (!slider) slider = GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        _baseScale = transform.localScale;
    }

    void Start()
    {
        if (!source)
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged) source = tagged.GetComponent<CCActor>();
            if (!source) source = FindObjectOfType<CCActor>();
        }

        float v = GetHunger01();
        slider.value = v;
        if (fillImage && fillGradient.colorKeys.Length > 0)
            fillImage.color = fillGradient.Evaluate(v);
    }

    void Update()
    {
        float target = GetHunger01();
        if (lerpSpeed > 0f)
            slider.value = Mathf.Lerp(slider.value, target, Time.deltaTime * lerpSpeed);
        else
            slider.value = target;

        if (fillImage && fillGradient.colorKeys.Length > 0)
            fillImage.color = fillGradient.Evaluate(slider.value);

        // Optional: pulse when critically low
        if (pulseWhenLow && slider.value <= warningThreshold)
        {
            float s = 1f + (pulseScale - 1f) * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed));
            transform.localScale = _baseScale * s;
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, Time.deltaTime * 10f);
        }
    }

    float GetHunger01()
    {
        if (!source) return 0f;
        return source.Hunger01; // from your CCActor
    }
}
