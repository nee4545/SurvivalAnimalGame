using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Slider healthSlider;     // assign or auto-find
    [SerializeField] private GameObject barRoot;      // which GO to show/hide (defaults to slider GO)

    [Header("Behavior")]
    [SerializeField] private float autoHideDelay = 2.0f; // seconds visible after last hit

    private Health health;
    private Coroutine hideCo;
    private bool bound;

    void Awake()
    {
        if (!healthSlider) healthSlider = GetComponentInChildren<Slider>(true);
        if (!barRoot) barRoot = healthSlider ? healthSlider.gameObject : gameObject;

        // start hidden
        if (barRoot) barRoot.SetActive(false);
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
        // show on any damage, refresh value, and restart hide timer
        Show();
        if (health) UpdateHealthBar(health.CurrentHealth, health.MaxHealth);
        RestartHideTimer();
    }

    private void RestartHideTimer()
    {
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
    }
}
