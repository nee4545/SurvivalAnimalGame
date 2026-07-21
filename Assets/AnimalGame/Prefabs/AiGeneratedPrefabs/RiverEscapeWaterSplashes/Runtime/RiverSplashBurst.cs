using UnityEngine;

/// <summary>
/// Simple helper for one-shot splash bursts. Useful when the player lands on a rideable.
/// </summary>
public class RiverSplashBurst : MonoBehaviour
{
    public ParticleSystem[] burstParticles;
    public int defaultBurstCount = 22;

    private void Awake()
    {
        if (burstParticles == null || burstParticles.Length == 0)
            burstParticles = GetComponentsInChildren<ParticleSystem>(true);
    }

    public void PlayBurst()
    {
        PlayBurst(defaultBurstCount);
    }

    public void PlayBurst(int count)
    {
        if (burstParticles == null || burstParticles.Length == 0)
            burstParticles = GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < burstParticles.Length; i++)
        {
            ParticleSystem particle = burstParticles[i];

            if (particle == null)
                continue;

            particle.Clear(true);
            particle.Emit(Mathf.Max(1, count));
        }
    }
}
