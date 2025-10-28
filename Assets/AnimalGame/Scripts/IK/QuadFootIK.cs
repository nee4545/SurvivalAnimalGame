using UnityEngine;

public class QuadFootIK : MonoBehaviour
{
    [Header("Targets & Hints (match your IK constraints)")]
    public Transform frontL_Target;
    public Transform frontR_Target;
    public Transform backL_Target;
    public Transform backR_Target;

    public Transform frontL_Paw;  // the paw (Tip) bone transforms
    public Transform frontR_Paw;
    public Transform backL_Paw;
    public Transform backR_Paw;

    [Header("Grounding")]
    public LayerMask groundMask;
    public float rayUpOffset = 0.5f;   // ray starts this much above paw
    public float rayDown = 1.5f;       // ray length down
    public float footOffset = 0.02f;   // small lift above ground
    public float rotLerp = 20f;        // rotation smoothing

    void LateUpdate()
    {
        ProjectToGround(frontL_Paw, frontL_Target);
        ProjectToGround(frontR_Paw, frontR_Target);
        ProjectToGround(backL_Paw, backL_Target);
        ProjectToGround(backR_Paw, backR_Target);
    }

    void ProjectToGround(Transform paw, Transform target)
    {
        if (!paw || !target) return;

        Vector3 from = paw.position + Vector3.up * rayUpOffset;
        if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, rayUpOffset + rayDown, groundMask, QueryTriggerInteraction.Ignore))
        {
            // Position
            target.position = hit.point + hit.normal * footOffset;

            // Orient target so +up aligns to the ground normal, +forward stays projected
            Vector3 fwd = Vector3.ProjectOnPlane(paw.forward, hit.normal).normalized;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;

            Quaternion goal = Quaternion.LookRotation(fwd, hit.normal);
            target.rotation = Quaternion.Slerp(target.rotation, goal, rotLerp * Time.deltaTime);
        }
        else
        {
            // No ground hit — keep target at animated paw
            target.position = paw.position;
            target.rotation = paw.rotation;
        }
    }
}
