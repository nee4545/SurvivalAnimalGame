using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HealthBarUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private GameObject barRoot;
    [SerializeField] private TMP_Text healthText; // <-- ADD THIS

    [Header("Visibility")]
    [SerializeField] private bool alwaysVisible = false;

    [Header("Behavior")]
    [SerializeField] private float autoHideDelay = 2.0f;

    private Health health;
    private Coroutine hideCo;
    private bool bound;

    void Awake()
    {
        if (!healthSlider) healthSlider = GetComponentInChildren<Slider>(true);
        //if (!healthText) healthText = GetComponentInChildren<TMP_Text>(true);
        if (!barRoot) barRoot = healthSlider ? healthSlider.gameObject : gameObject;

        if (barRoot) barRoot.SetActive(alwaysVisible);
    }

    void OnEnable()
    {
        if (!health) health = GetComponentInParent<Health>();

        if (health && !bound)
        {
            health.onHealthChanged += UpdateHealthBar;
            health.onDamageTaken.AddListener(OnDamageTaken);
            health.onDeath.AddListener(HideImmediate);
            bound = true;

            UpdateHealthBar(health.CurrentHealth, health.MaxHealth);
        }

        if (alwaysVisible) Show();
    }

    void OnDisable()
    {
        if (health && bound)
        {
            health.onHealthChanged -= UpdateHealthBar;
            health.onDamageTaken.RemoveListener(OnDamageTaken);
            health.onDeath.RemoveListener(HideImmediate);
        }

        bound = false;

        if (hideCo != null)
        {
            StopCoroutine(hideCo);
            hideCo = null;
        }
    }

    private void OnDamageTaken(float dmg)
    {
        Show();

        if (health)
            UpdateHealthBar(health.CurrentHealth, health.MaxHealth);

        if (!alwaysVisible)
            RestartHideTimer();
    }

    private void RestartHideTimer()
    {
        if (alwaysVisible) return;

        if (hideCo != null)
            StopCoroutine(hideCo);

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
        if (barRoot && !barRoot.activeSelf)
            barRoot.SetActive(true);
    }

    private void HideImmediate()
    {
        if (hideCo != null)
        {
            StopCoroutine(hideCo);
            hideCo = null;
        }

        if (barRoot && barRoot.activeSelf)
            barRoot.SetActive(false);
    }

    private void UpdateHealthBar(float current, float max)
    {
        if (!healthSlider) return;

        healthSlider.maxValue = max;
        healthSlider.value = current;

        if (healthText)
            healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";

        if (alwaysVisible) Show();
    }
}
