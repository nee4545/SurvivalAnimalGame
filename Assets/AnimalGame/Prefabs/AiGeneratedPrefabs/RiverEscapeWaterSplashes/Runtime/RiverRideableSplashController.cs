using UnityEngine;

/// <summary>
/// Attach this to a river rideable if you want easy play/stop control for continuous and landing splash particles.
/// </summary>
public class RiverRideableSplashController : MonoBehaviour
{
    [Header("Particles")]
    public ParticleSystem[] continuousSplashes;
    public RiverSplashBurst landingBurst;

    [Header("Water Height")]
    public bool autoApplyWaterHeight = true;
    public float waterY = 0f;
    public float heightOffset = 0.04f;

    [Header("Playback")]
    public bool playContinuousOnEnable = true;
    public bool stopContinuousOnDisable = true;

    private RiverSplashLockToWater[] waterLocks;

    private void Awake()
    {
        CacheWaterLocks();
    }

    private void OnEnable()
    {
        CacheWaterLocks();
        ApplyWaterHeightToLocks();

        if (playContinuousOnEnable)
            PlayContinuous();
        else
            StopContinuousImmediate();
    }

    public void StopContinuousImmediate()
    {
        if (continuousSplashes == null || continuousSplashes.Length == 0)
            continuousSplashes = GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < continuousSplashes.Length; i++)
        {
            ParticleSystem particle = continuousSplashes[i];

            if (particle == null)
                continue;

            ParticleSystem.MainModule main = particle.main;

            if (!main.loop)
                continue;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnDisable()
    {
        if (stopContinuousOnDisable)
            StopContinuous();
    }

    private void LateUpdate()
    {
        ApplyWaterHeightToLocks();
    }

    private void CacheWaterLocks()
    {
        waterLocks = GetComponentsInChildren<RiverSplashLockToWater>(true);
    }

    private void ApplyWaterHeightToLocks()
    {
        if (!autoApplyWaterHeight)
            return;

        if (waterLocks == null || waterLocks.Length == 0)
            CacheWaterLocks();

        for (int i = 0; i < waterLocks.Length; i++)
        {
            RiverSplashLockToWater waterLock = waterLocks[i];

            if (waterLock == null)
                continue;

            waterLock.waterY = waterY;
            waterLock.heightOffset = heightOffset;

            if (waterLock.followTarget == null)
                waterLock.followTarget = transform;
        }
    }

    public void PlayContinuous()
    {
        if (continuousSplashes == null || continuousSplashes.Length == 0)
            continuousSplashes = GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < continuousSplashes.Length; i++)
        {
            ParticleSystem particle = continuousSplashes[i];

            if (particle == null)
                continue;

            ParticleSystem.MainModule main = particle.main;

            if (!main.loop)
                continue;

            if (!particle.isPlaying)
                particle.Play(true);
        }
    }

    public void StopContinuous()
    {
        if (continuousSplashes == null)
            return;

        for (int i = 0; i < continuousSplashes.Length; i++)
        {
            ParticleSystem particle = continuousSplashes[i];

            if (particle == null)
                continue;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public void PlayLandingBurst()
    {
        if (landingBurst != null)
            landingBurst.PlayBurst();
    }
}
