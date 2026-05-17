using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CubHealthStatusUI : MonoBehaviour
{
    [Header("References")]
    public Health health;
    public Image sliderFillImage;
    public TextMeshProUGUI statusText;

    [Header("Sprites")]
    public Sprite happySprite;
    public Sprite starvingSprite;

    [Header("Text")]
    public string happyText = "I am Full";
    public string starvingText = "Starving";

    [Header("Threshold")]
    public float starvingHealthThreshold = 45f;

    private void Update()
    {
        if (health == null)
            return;

        bool isStarving = health.CurrentHealth <= starvingHealthThreshold;

        if (sliderFillImage)
            sliderFillImage.sprite = isStarving ? starvingSprite : happySprite;

        if (statusText)
            statusText.text = isStarving ? starvingText : happyText;
    }
}
