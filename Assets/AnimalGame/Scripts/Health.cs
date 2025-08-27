using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    // Backing
    private float currentHealth;

    [Header("Events")]
    public UnityEvent onDeath;
    public UnityEvent<float> onDamageTaken;        // emits damage amount
    public event Action<Transform /*victim*/, Transform /*attacker*/> Damaged;

    // (current, max)
    public System.Action<float, float> onHealthChanged;

    // Exposed props
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    // Now a real property with a private setter so other scripts can read it,
    // and this class controls when it flips.
    public bool IsDead { get; private set; }

    void Awake()
    {
        currentHealth = maxHealth;
        IsDead = false;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float damage, Transform attacker = null)
    {
        if (IsDead) return;
        if (damage <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        onDamageTaken?.Invoke(damage);
        Damaged?.Invoke(transform, attacker);
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f && !IsDead)
        {
            IsDead = true;
            Die();
        }
    }

    /// <summary>Restore to full and clear death state.</summary>
    public void ResetHealth()
    {
        IsDead = false;
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>Revive to a specific HP (defaults to full).</summary>
    public void Revive(float hp = -1f)
    {
        IsDead = false;
        currentHealth = (hp < 0f) ? maxHealth : Mathf.Clamp(hp, 0f, maxHealth);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>Force death now (useful for debug or scripted kills).</summary>
    public void Kill()
    {
        if (IsDead) return;
        currentHealth = 0f;
        IsDead = true;
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        Die();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        if (amount <= 0f) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        onDeath?.Invoke();
    }

    public float GetHealthPercent()
    {
        return maxHealth <= 0f ? 0f : (currentHealth / maxHealth);
    }
}
