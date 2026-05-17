using UnityEngine;

/// <summary>
/// Controls a lightweight dust trail ParticleSystem based on player movement speed.
/// Attach this to the DustTrail prefab/root. Assign the player as Target.
/// </summary>
public class DustTrailController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 localOffset = new Vector3(0f, 0.08f, -0.35f);

    [Header("Movement Detection")]
    public float minMoveSpeed = 0.15f;
    public float maxMoveSpeed = 5f;
    public bool rotateWithTarget = true;

    [Header("Emission")]
    public ParticleSystem dustParticles;
    public float minEmission = 0f;
    public float maxEmission = 45f;
    public float emissionSmooth = 10f;

    private Vector3 lastTargetPosition;
    private float currentEmission;

    private void Awake()
    {
        if (dustParticles == null)
            dustParticles = GetComponentInChildren<ParticleSystem>();
    }

    private void Start()
    {
        if (target != null)
            lastTargetPosition = target.position;

        SetEmission(0f);
    }

    private void LateUpdate()
    {
        if (target == null || dustParticles == null)
            return;

        float speed = (target.position - lastTargetPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastTargetPosition = target.position;

        if (rotateWithTarget)
        {
            transform.position = target.TransformPoint(localOffset);
            transform.rotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
        }
        else
        {
            transform.position = target.position + localOffset;
        }

        float t = Mathf.InverseLerp(minMoveSpeed, maxMoveSpeed, speed);
        float desiredEmission = Mathf.Lerp(minEmission, maxEmission, t);

        if (speed < minMoveSpeed)
            desiredEmission = 0f;

        currentEmission = Mathf.Lerp(currentEmission, desiredEmission, Time.deltaTime * emissionSmooth);
        SetEmission(currentEmission);

        if (currentEmission > 1f && !dustParticles.isPlaying)
            dustParticles.Play();
        else if (currentEmission <= 0.2f && dustParticles.isPlaying)
            dustParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }

    private void SetEmission(float rate)
    {
        if (dustParticles == null)
            return;

        var emission = dustParticles.emission;
        emission.rateOverTime = rate;
    }
}
