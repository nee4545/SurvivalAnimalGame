
using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections.Generic;

/// Run before most scripts so Update happens well before RigBuilder's LateUpdate.
[DefaultExecutionOrder(-80)]
public class IKAutoTunerV2 : MonoBehaviour
{
    [Header("Run Mode")]
    public bool runOnStart = true;
    public bool continuous = true;

    [Header("Character Frame")]
    [Tooltip("Pelvis/Spine1 (used as body center). If null, uses this transform.")]
    public Transform bodyCenter;
    [Tooltip("Which LOCAL axis of bodyCenter points forward along the animal.")]
    public Vector3 bodyForwardLocal = Vector3.forward;
    [Tooltip("Which LOCAL axis of bodyCenter points up.")]
    public Vector3 bodyUpLocal = Vector3.up;

    [Header("Clamp / Bend")]
    [Range(0.80f, 1.00f)] public float reachSafety = 0.98f;
    public float bendSlack = 0.02f;

    [Header("Hint Placement")]
    [Range(0.0f, 0.6f)] public float hintOutward = 0.15f;
    [Range(0.0f, 0.4f)] public float hintForeBias = 0.05f;
    public float minHintDist = 0.06f;

    [Header("Weights")]
    [Range(0f, 1f)] public float targetPosWeight = 1.0f;
    [Range(0f, 1f)] public float targetRotWeight = 0.0f;
    [Range(0f, 1f)] public float hintWeight = 0.55f;
    public bool maintainTargetPosOffset = true;
    public bool maintainTargetRotOffset = true;

    [Header("Debug")]
    public bool logOnce = true;

    RigBuilder _builder;
    readonly List<TwoBoneIKConstraint> _legs = new();

    void Awake()
    {
        _builder = GetComponentInChildren<RigBuilder>(true);
        if (_builder) _builder.GetComponentsInChildren(true, _legs);
        if (!bodyCenter) bodyCenter = transform;
    }

    void Start()
    {
        if (runOnStart) FixAll(true);
    }

    void Update()
    {
        if (continuous) FixAll(false);
    }

    [ContextMenu("Fix Now")]
    public void FixNowMenu() => FixAll(true);

    void FixAll(bool verbose)
    {
        if (_legs.Count == 0)
        {
            if (verbose) Debug.LogWarning("[IKAutoTuner] No TwoBoneIKConstraint found.");
            return;
        }

        // Character axes in world space
        Vector3 bodyFwd = bodyCenter.TransformDirection(bodyForwardLocal).normalized;
        Vector3 bodyUp  = bodyCenter.TransformDirection(bodyUpLocal).normalized;

        foreach (var ik in _legs)
        {
            var d = ik.data;
            if (!d.root || !d.mid || !d.tip || !d.target) continue;

            // --- segment lengths
            float upper = Vector3.Distance(d.root.position, d.mid.position);
            float lower = Vector3.Distance(d.mid.position,  d.tip.position);
            float maxReach = Mathf.Max(0.001f, (upper + lower) * reachSafety) - bendSlack;
            if (maxReach < 0.01f) maxReach = 0.01f;

            // --- clamp target within reach (pre-rig)
            Vector3 desired = d.target.position;
            Vector3 RtoT = desired - d.root.position;
            float dist = RtoT.magnitude;
            if (dist > maxReach)
                desired = d.root.position + RtoT * (maxReach / dist);
            d.target.position = desired;

            // --- detect hind vs fore via body forward
            bool isHind = Vector3.Dot(d.root.position - bodyCenter.position, bodyFwd) < 0f;

            // --- outward = left/right relative to body
            Vector3 inward  = (d.mid.position - bodyCenter.position); inward.y = 0f;
            if (inward.sqrMagnitude < 1e-6f) inward = bodyCenter.right;
            Vector3 outward = Vector3.Cross(bodyUp, inward.normalized).normalized;

            // --- along = ground-projected root→target
            Vector3 along = (desired - d.root.position); along.y = 0f;
            if (along.sqrMagnitude < 1e-6f) along = bodyFwd;
            along.Normalize();

            // --- hint placement
            if (d.hint)
            {
                float outDist = Mathf.Max(minHintDist, hintOutward * upper);
                float fore    = hintForeBias * upper * (isHind ? -1f : 1f);

                Vector3 hintPos = d.mid.position + outward * outDist + along * fore;

                // enforce minimum separation from knee
                Vector3 delta = hintPos - d.mid.position;
                float hd = delta.magnitude;
                if (hd < minHintDist)
                    hintPos = d.mid.position + (hd < 1e-6f ? outward : delta.normalized) * minHintDist;

                d.hint.position = hintPos;
            }

            // --- apply weights/flags
            d.targetPositionWeight = targetPosWeight;
            d.targetRotationWeight = targetRotWeight;
            d.hintWeight           = hintWeight;
            d.maintainTargetPositionOffset = maintainTargetPosOffset;
            d.maintainTargetRotationOffset = maintainTargetRotOffset;

            // push back into the constraint
            ik.data = d;

            if (verbose && logOnce)
            {
                float now = Vector3.Distance(d.root.position, d.target.position);
                Debug.Log($"[IKAutoTunerV2:{ik.name}] Reach {now:F3}/{(upper+lower):F3}  Hind:{isHind}  HintDist:{(d.hint?Vector3.Distance(d.mid.position,d.hint.position):0f):F3}");
            }
        }

        logOnce = false; // only once unless you toggle it again
    }
}
