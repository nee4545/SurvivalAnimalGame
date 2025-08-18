using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class StaminaBarUI : MonoBehaviour
{
    [Header("Source")]
    public CCActor source;                          // drag your player here (or leave null to auto-find)

    [Header("UI")]
    public Slider slider;                           // auto-fills from this GameObject if left null
    public Image fillImage;                         // optional: assign the Fill image of the slider
    public Gradient fillGradient;                   // optional: color over [0..1]

    [Header("Update")]
    [Tooltip("0 = instant, higher = smoother")]
    public float lerpSpeed = 12f;

    void Awake()
    {
        if (!slider) slider = GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    void Start()
    {
        if (!source)
        {
            // Try common ways to find the player
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged) source = tagged.GetComponent<CCActor>();
            if (!source) source = FindObjectOfType<CCActor>();
        }

        // Initialize UI
        float v = GetStamina01();
        slider.value = v;
        if (fillImage && fillGradient.colorKeys.Length > 0)
            fillImage.color = fillGradient.Evaluate(v);
    }

    void Update()
    {
        float target = GetStamina01();
        if (lerpSpeed > 0f)
            slider.value = Mathf.Lerp(slider.value, target, Time.deltaTime * lerpSpeed);
        else
            slider.value = target;

        if (fillImage && fillGradient.colorKeys.Length > 0)
            fillImage.color = fillGradient.Evaluate(slider.value);
    }

    float GetStamina01()
    {
        if (!source) return 0f;
        return source.Stamina01; // uses your CCActor property
    }
}

