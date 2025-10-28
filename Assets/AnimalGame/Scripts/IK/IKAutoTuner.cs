
using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections.Generic;

/// <summary>
/// Auto-fixes common TwoBoneIK issues at runtime:
/// - clamps Target to reach (with safety + bend slack)
/// - relocates Hint outward (and slight front/back bias)
/// - applies sane weights & maintain-offset flags
/// Drop this on the character root that also parents the RigBuilder.
/// </summary>
public class IKAutoTuner : MonoBehaviour
{
    [Header("Run Mode")]
    [Tooltip("Run once on Start()")]
    public bool runOnStart = true;
    [Tooltip("Keep correcting every frame (useful when targets are moved by other code)")]
    public bool continuous = true;

    [Header("Clamp / Bend")]
    [Tooltip("Clamp target to this fraction of (upper+lower) reach.")]
    [Range(0.80f, 1.00f)] public float reachSafety = 0.98f;
    [Tooltip("Keep the leg slightly bent by shortening max reach by this amount (in meters, scaled).")]
    public float bendSlack = 0.02f;

    [Header("Hint Placement")]
    [Tooltip("Outward distance as a fraction of upper-leg length.")]
    [Range(0.0f, 0.6f)] public float hintOutward = 0.15f;
    [Tooltip("Front (forelegs) / back (hindlegs) bias as a fraction of upper-leg length.")]
    [Range(0.0f, 0.4f)] public float hintForeBias = 0.05f;
    [Tooltip("Minimum distance to keep the hint away from the knee.")]
    public float minHintDist = 0.06f;

    [Header("Weights")]
    [Range(0f, 1f)] public float targetPosWeight = 1.0f;
    [Range(0f, 1f)] public float targetRotWeight = 0.0f;
    [Range(0f, 1f)] public float hintWeight = 0.55f;
    public bool maintainTargetPosOffset = true;
    public bool maintainTargetRotOffset = true;

    [Header("Logging")]
    public bool verboseOnceOnStart = true;

    RigBuilder _builder;
    List<TwoBoneIKConstraint> _legs = new();

    void Awake()
    {
        _builder = GetComponentInChildren<RigBuilder>(true);
        if (_builder) _builder.GetComponentsInChildren(true, _legs);
    }

    void Start()
    {
        if (runOnStart) FixAll(verboseOnceOnStart);
    }

    void LateUpdate()
    {
        if (continuous) FixAll(false);
    }

    [ContextMenu("Fix Now")]
    public void FixNowContextMenu()
    {
        FixAll(true);
    }

    void FixAll(bool log)
    {
        if (_legs.Count == 0)
        {
            if (log) Debug.LogWarning("[IKAutoTuner] No TwoBoneIKConstraint found under this object.");
            return;
        }

        // Character frame of reference (forward/up). If your model’s forward is different,
        // change this to whatever represents “body forward”.
        Vector3 bodyFwd = transform.forward;
        Vector3 bodyUp  = transform.up;

        foreach (var ik in _legs)
        {
            var d = ik.data;
            if (!d.root || !d.mid || !d.tip || !d.target) continue;

            // --- lengths/reach
            float upper = Vector3.Distance(d.root.position, d.mid.position);
            float lower = Vector3.Distance(d.mid.position,  d.tip.position);
            float maxReach = Mathf.Max(0.001f, (upper + lower) * reachSafety) - bendSlack;
            if (maxReach < 0.01f) maxReach = 0.01f;

            // --- clamp target
            Vector3 desired = d.target.position;
            Vector3 fromRoot = desired - d.root.position;
            float dist = fromRoot.magnitude;
            if (dist > maxReach)
                desired = d.root.position + fromRoot * (maxReach / dist);

            d.target.position = desired; // apply clamp

            // --- front/hind detection (relative to body forward)
            bool isHind = Vector3.Dot(d.root.position - transform.position, bodyFwd) < 0f;

            // --- outward direction: “away from body center” crossed with up
            Vector3 inward = (d.mid.position - transform.position);
            inward.y = 0f;
            if (inward.sqrMagnitude < 1e-4f) inward = d.mid.right; // fallback
            Vector3 outward = Vector3.Cross(bodyUp, inward.normalized).normalized; // left/right
            if (outward.sqrMagnitude < 1e-4f) outward = transform.right;

            // --- along-the-leg direction (project root->target on ground)
            Vector3 along = (desired - d.root.position);
            along.y = 0f;
            if (along.sqrMagnitude < 1e-4f) along = bodyFwd;
            along.Normalize();

            // --- compute hint position
            if (d.hint != null)
            {
                float outDist = Mathf.Max(minHintDist, hintOutward * upper);
                float fore    = hintForeBias * upper * (isHind ? -1f : 1f);

                Vector3 hintPos = d.mid.position + outward * outDist + along * fore;

                // enforce minimum separation from knee
                Vector3 toHint = hintPos - d.mid.position;
                float hd = toHint.magnitude;
                if (hd < minHintDist && hd > 1e-4f)
                    hintPos = d.mid.position + toHint.normalized * minHintDist;

                d.hint.position = hintPos;
            }

            // --- sane weights & offsets
            d.targetPositionWeight = targetPosWeight;
            d.targetRotationWeight = targetRotWeight;
            d.hintWeight           = hintWeight;
            d.maintainTargetPositionOffset = maintainTargetPosOffset;
            d.maintainTargetRotationOffset = maintainTargetRotOffset;

            // push back
            ik.data = d;

            if (log)
            {
                float newDist = Vector3.Distance(d.root.position, d.target.position);
                Debug.Log($"[IKAutoTuner:{ik.name}] Reach {newDist:F3}/{(upper+lower):F3}  (hind:{isHind})  " +
                          $"Hint:{(d.hint?Vector3.Distance(d.mid.position,d.hint.position):0f):F3}  Weights P/R/H {targetPosWeight}/{targetRotWeight}/{hintWeight}");
            }
        }
    }
}

