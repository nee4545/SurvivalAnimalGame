using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Slider healthSlider;     // assign or auto-find
    [SerializeField] private GameObject barRoot;      // which GO to show/hide (defaults to slider GO)

    [Header("Visibility")]
    [Tooltip("If true, the health bar is always visible and won't auto-hide (still hides on death).")]
    [SerializeField] private bool alwaysVisible = false;

    [Header("Behavior")]
    [SerializeField] private float autoHideDelay = 2.0f; // seconds visible after last hit (ignored if alwaysVisible)

    private Health health;
    private Coroutine hideCo;
    private bool bound;

    void Awake()
    {
        if (!healthSlider) healthSlider = GetComponentInChildren<Slider>(true);
        if (!barRoot) barRoot = healthSlider ? healthSlider.gameObject : gameObject;

        // Initial visibility
        if (barRoot) barRoot.SetActive(alwaysVisible);
    }

    void OnEnable()
    {
        // (Re)bind
        if (!health) health = GetComponentInParent<Health>();
        if (health && !bound)
        {
            health.onHealthChanged += UpdateHealthBar;
            health.onDamageTaken.AddListener(OnDamageTaken);
            health.onDeath.AddListener(HideImmediate);
            bound = true;

            // initialize values
            UpdateHealthBar(health.CurrentHealth, health.MaxHealth);
        }

        // Ensure visible if configured
        if (alwaysVisible) Show();
    }

    void OnDisable()
    {
        // Unbind (important for pooling)
        if (health && bound)
        {
            health.onHealthChanged -= UpdateHealthBar;
            health.onDamageTaken.RemoveListener(OnDamageTaken);
            health.onDeath.RemoveListener(HideImmediate);
        }
        bound = false;

        if (hideCo != null) { StopCoroutine(hideCo); hideCo = null; }
    }

    private void OnDamageTaken(float dmg)
    {
        // Show on any damage
        Show();

        // refresh value
        if (health) UpdateHealthBar(health.CurrentHealth, health.MaxHealth);

        // Only auto-hide if not always visible
        if (!alwaysVisible) RestartHideTimer();
    }

    private void RestartHideTimer()
    {
        if (alwaysVisible) return;
        if (hideCo != null) StopCoroutine(hideCo);
        hideCo = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(autoHideDelay);
        HideImmediate();
        hideCo = null;
    }

    private void Show()
    {
        if (barRoot && !barRoot.activeSelf) barRoot.SetActive(true);
    }

    private void HideImmediate()
    {
        if (hideCo != null) { StopCoroutine(hideCo); hideCo = null; }
        if (barRoot && barRoot.activeSelf) barRoot.SetActive(false);
    }

    private void UpdateHealthBar(float current, float max)
    {
        if (!healthSlider) return;
        healthSlider.maxValue = max;
        healthSlider.value = current;

        // If set to alwaysVisible, ensure it's shown (useful after pooling)
        if (alwaysVisible) Show();
    }
}
