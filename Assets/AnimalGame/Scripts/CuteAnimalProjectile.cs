using UnityEngine;

public class CuteAnimalProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private int damage;
    private float lifetime;

    public void Init(Vector3 dir, float projectileSpeed, int projectileDamage, float life)
    {
        direction = dir.normalized;
        speed = projectileSpeed;
        damage = projectileDamage;
        lifetime = life;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        Health health = other.GetComponent<Health>();

        if (health != null)
        {
            health.TakeDamage(damage);

            PlayerEffects effects = other.GetComponent<PlayerEffects>();
            if (effects != null)
                effects.PlayBloodVFX();

            Destroy(gameObject);
        }

        Destroy(gameObject);
    }
}