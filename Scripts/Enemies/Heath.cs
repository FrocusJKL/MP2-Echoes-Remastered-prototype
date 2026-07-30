using UnityEngine;
using System;
using System.Collections;

public class Health : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public bool shouldRespawn = true;
    public float respawnTime = 5f;

    public float CurrentHealth { get; private set; }
    public event Action OnDeath;
    public event Action<float> OnDamageTaken;

    private bool isDead = false;
    public bool IsDead => isDead;

    private Renderer[] renderers;
    private Collider[] colliders;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();

        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        CurrentHealth -= amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

        OnDamageTaken?.Invoke(amount);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        OnDeath?.Invoke();

        if (shouldRespawn)
        {
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    IEnumerator RespawnRoutine()
    {
        // Ocultar enemigo (pero NO desactivarlo completo)
        SetActiveState(false);

        yield return new WaitForSeconds(respawnTime);

        // Reset
        CurrentHealth = maxHealth;
        isDead = false;

        // Mostrar enemigo
        SetActiveState(true);
    }

    void SetActiveState(bool state)
    {
        foreach (var r in renderers)
            r.enabled = state;

        foreach (var c in colliders)
            c.enabled = state;
    }
}