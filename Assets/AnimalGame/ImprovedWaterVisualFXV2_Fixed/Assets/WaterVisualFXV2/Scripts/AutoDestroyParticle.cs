using UnityEngine;

public class AutoDestroyParticle : MonoBehaviour
{
    [SerializeField] private float fallbackLifetime = 2f;

    private void Start()
    {
        ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
        if (systems == null || systems.Length == 0)
        {
            Destroy(gameObject, fallbackLifetime);
            return;
        }

        float maxLifetime = 0f;

        foreach (ParticleSystem system in systems)
        {
            var main = system.main;
            float startLifetime = main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
                ? Mathf.Max(main.startLifetime.constantMin, main.startLifetime.constantMax)
                : main.startLifetime.constant;

            maxLifetime = Mathf.Max(maxLifetime, main.duration + startLifetime);
        }

        Destroy(gameObject, Mathf.Max(maxLifetime + 0.35f, fallbackLifetime));
    }
}