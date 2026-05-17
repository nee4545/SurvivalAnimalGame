using UnityEngine;

public class EnvironmentEffectsController : MonoBehaviour
{
    public ParticleSystem rainEffect;
    public ParticleSystem snowEffect;
    public ParticleSystem windEffect;

    public void SetClear()
    {
        StopEffect(rainEffect);
        StopEffect(snowEffect);
        StopEffect(windEffect);
    }

    public void SetRain()
    {
        PlayEffect(rainEffect);
        StopEffect(snowEffect);
        StopEffect(windEffect);
    }

    public void SetSnow()
    {
        StopEffect(rainEffect);
        PlayEffect(snowEffect);
        StopEffect(windEffect);
    }

    public void SetWind()
    {
        StopEffect(rainEffect);
        StopEffect(snowEffect);
        PlayEffect(windEffect);
    }

    public void SetRainAndWind()
    {
        PlayEffect(rainEffect);
        StopEffect(snowEffect);
        PlayEffect(windEffect);
    }

    public void SetSnowAndWind()
    {
        StopEffect(rainEffect);
        PlayEffect(snowEffect);
        PlayEffect(windEffect);
    }

    private void PlayEffect(ParticleSystem effect)
    {
        if (effect == null) return;
        if (!effect.gameObject.activeSelf) effect.gameObject.SetActive(true);
        if (!effect.isPlaying) effect.Play(true);
    }

    private void StopEffect(ParticleSystem effect)
    {
        if (effect == null) return;
        if (effect.isPlaying) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
