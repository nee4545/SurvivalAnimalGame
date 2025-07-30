using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public Slider healthSlider;
    private Health health;

    void Start()
    {
        // Find Health component on parent or nearby
        health = GetComponentInParent<Health>();
        if (health != null)
        {
            // Hook to health update
            health.onHealthChanged += UpdateHealthBar;
            health.onDeath.AddListener(Hide);
        }

        // Initialize value
        UpdateHealthBar(health.CurrentHealth, health.MaxHealth);
    }

    void UpdateHealthBar(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (health != null)
            health.onHealthChanged -= UpdateHealthBar;
    }
}
