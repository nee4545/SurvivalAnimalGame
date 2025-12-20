using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEffects : MonoBehaviour
{
    public ParticleSystem bloodSplatterVFX;  // one-shot
    public ParticleSystem bloodFlowVFX;      // looping
    public ParticleSystem DeathVFX;      
    float bloodFlowDelay = 1.5f;
    
    private GameObject vfxObject = null;

    private void OnEnable()
    {
        bloodSplatterVFX.gameObject.SetActive(false);
        bloodFlowVFX.gameObject.SetActive(false);
        DeathVFX.gameObject.SetActive(false);

        GetReferences();
    }

    private void OnDisable()
    {
        bloodSplatterVFX.gameObject.SetActive(false);
        bloodFlowVFX.gameObject.SetActive(false);
        DeathVFX.gameObject.SetActive(false);
    }

    public void GetReferences()
    {
        vfxObject = transform.Find("Vfx").gameObject;

        bloodSplatterVFX = vfxObject.transform.GetChild(0).GetComponent<ParticleSystem>();
        bloodFlowVFX = vfxObject.transform.GetChild(2).GetComponent<ParticleSystem>();
        DeathVFX = vfxObject.transform.GetChild(3).GetComponent<ParticleSystem>();
    }

    private void Awake()
    {
        GetReferences();
    }


    public void PlayBloodVFX()
    {
        bloodSplatterVFX.gameObject.SetActive(true);
        bloodFlowVFX.gameObject.SetActive(true);

        if (bloodSplatterVFX != null)
        {
            bloodSplatterVFX.Play();  // Play one-shot splatter
        }

        if (bloodFlowVFX != null)
        {
            bloodFlowVFX.Play();      // Start looping flow
            StartCoroutine(StopBloodFlowAfterDelay(bloodFlowDelay));  // Adjust delay
        }
    }

    public void PlayDeadVFX()
    {
        DeathVFX.gameObject.SetActive(true);
        DeathVFX.Play();
    }

    private IEnumerator StopBloodFlowAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (bloodFlowVFX != null)
        {
            bloodFlowVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
