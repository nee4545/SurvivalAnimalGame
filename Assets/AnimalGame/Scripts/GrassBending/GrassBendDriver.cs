using UnityEngine;

[DefaultExecutionOrder(-50)]
public class GrassBendDriver : MonoBehaviour
{
    [Header("Bend Shape")]
    public float baseRadius = 1.2f;
    public float baseStrength = 0.35f;

    [Header("Speed Boost (optional)")]
    public float speedRadiusBoost = 0.8f;
    public float speedStrengthBoost = 0.2f;

    [Header("Tip Weighting")]
    // 0..1: how much the tip bends vs. the base
    public float tipWeight = 1.0f;
    // Approx local height of your grass mesh (tune until it feels right)
    public float maxTipHeight = 1.0f;

    Vector3 _lastPos;

    void OnEnable() { _lastPos = transform.position; }
    void LateUpdate()
    {
        var pos = transform.position;
        var vel = (pos - _lastPos) / Mathf.Max(Time.deltaTime, 1e-5f);
        float speed = vel.magnitude;

        Shader.SetGlobalVector("_BendOrigin", pos);
        Shader.SetGlobalFloat("_BendRadius", baseRadius + speed * speedRadiusBoost);
        Shader.SetGlobalFloat("_BendStrength", baseStrength + speed * speedStrengthBoost);
        Shader.SetGlobalFloat("_BendTipWeight", tipWeight);
        Shader.SetGlobalFloat("_BendMaxTipHeight", Mathf.Max(0.001f, maxTipHeight));

        _lastPos = pos;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, baseRadius);
    }
}
