using UnityEngine;

[DefaultExecutionOrder(-50)]
public class GrassBendDriver : MonoBehaviour
{
    public enum BendSpeedMode
    {
        TransformMovement,
        StampedeWorldScroller,
        Manual
    }

    [Header("Mode")]
    public BendSpeedMode speedMode = BendSpeedMode.TransformMovement;

    [Tooltip("Assign this only for stampede mode.")]
    public StampedeWorldScroller stampedeWorldScroller;

    [Tooltip("Used only when Speed Mode is Manual.")]
    public float manualSpeed = 0f;

    [Header("Bend Shape")]
    public float baseRadius = 1.2f;
    public float baseStrength = 0.35f;

    [Header("Speed Boost")]
    public float speedRadiusBoost = 0.8f;
    public float speedStrengthBoost = 0.2f;

    [Header("Stampede Offset")]
    [Tooltip("Offsets the bend origin toward incoming grass/world movement.")]
    public bool useStampedeOriginOffset = true;

    [Tooltip("How far in front of the player the grass starts bending.")]
    public float stampedeOriginOffsetDistance = 0.8f;

    [Header("Tip Weighting")]
    public float tipWeight = 1.0f;
    public float maxTipHeight = 1.0f;

    private Vector3 _lastPos;

    private void OnEnable()
    {
        _lastPos = transform.position;
    }

    private void OnDisable()
    {
        ClearGrassBend();
    }

    private void LateUpdate()
    {
        Vector3 bendOrigin = GetBendOrigin();
        float speed = GetBendSpeed();

        Shader.SetGlobalVector("_BendOrigin", bendOrigin);
        Shader.SetGlobalFloat("_BendRadius", baseRadius + speed * speedRadiusBoost);
        Shader.SetGlobalFloat("_BendStrength", baseStrength + speed * speedStrengthBoost);
        Shader.SetGlobalFloat("_BendTipWeight", tipWeight);
        Shader.SetGlobalFloat("_BendMaxTipHeight", Mathf.Max(0.001f, maxTipHeight));

        _lastPos = transform.position;
    }

    private Vector3 GetBendOrigin()
    {
        Vector3 origin = transform.position;

        if (speedMode != BendSpeedMode.StampedeWorldScroller)
            return origin;

        if (!useStampedeOriginOffset)
            return origin;

        if (stampedeWorldScroller == null)
            return origin;

        Vector3 worldMoveDirection = stampedeWorldScroller.GetCurrentScrollMoveDirection();
        worldMoveDirection.y = 0f;

        if (worldMoveDirection.sqrMagnitude < 0.001f)
            return origin;

        worldMoveDirection.Normalize();

        // Move bend origin slightly toward incoming grass.
        return origin + worldMoveDirection * stampedeOriginOffsetDistance;
    }

    private float GetBendSpeed()
    {
        if (speedMode == BendSpeedMode.Manual)
            return Mathf.Max(0f, manualSpeed);

        if (speedMode == BendSpeedMode.StampedeWorldScroller)
        {
            if (stampedeWorldScroller != null)
                return stampedeWorldScroller.GetCurrentScrollSpeed();

            return 0f;
        }

        Vector3 pos = transform.position;
        Vector3 vel = (pos - _lastPos) / Mathf.Max(Time.deltaTime, 1e-5f);

        return vel.magnitude;
    }

    private void ClearGrassBend()
    {
        Shader.SetGlobalFloat("_BendRadius", 0f);
        Shader.SetGlobalFloat("_BendStrength", 0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, baseRadius);

        if (speedMode == BendSpeedMode.StampedeWorldScroller &&
            stampedeWorldScroller != null &&
            useStampedeOriginOffset)
        {
            Vector3 origin = GetBendOrigin();

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, baseRadius * 0.6f);
            Gizmos.DrawLine(transform.position, origin);
        }
    }
}