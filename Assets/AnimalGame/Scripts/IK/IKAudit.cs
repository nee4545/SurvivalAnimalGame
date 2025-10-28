
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class IKAudit : MonoBehaviour
{
    [Header("Run Options")]
    public bool runOnStart = true;
    public bool runEveryXSeconds = false;
    public float interval = 2f;

    float t;

    void Start(){ if (runOnStart) Audit(); }
    void Update(){ if(!runEveryXSeconds) return; t += Time.deltaTime; if(t>=interval){ t=0; Audit(); } }

    [ContextMenu("Run IK Audit Now")]
    public void Audit()
    {
        var builder = GetComponentInChildren<RigBuilder>(true);
        if (!builder){ Debug.LogWarning("[IKAudit] No RigBuilder found under this object."); return; }

        var iks = builder.GetComponentsInChildren<TwoBoneIKConstraint>(true);
        if (iks.Length == 0){ Debug.LogWarning("[IKAudit] No TwoBoneIKConstraint found."); return; }

        Debug.Log($"[IKAudit] Found {iks.Length} TwoBoneIKConstraint(s).");

        // detect duplicate chains
        var seen = new Dictionary<string, List<TwoBoneIKConstraint>>();

        // ensure leg IKs are last in the Rig (ordering)
        var rig = builder.GetComponentInChildren<Rig>(true);
        int legsStartIndex = int.MaxValue;
        for (int i = 0; i < rig.transform.childCount; i++)
        {
            var c = rig.transform.GetChild(i);
            if (c.GetComponentInChildren<TwoBoneIKConstraint>(true) && i < legsStartIndex)
                legsStartIndex = i;
        }

        foreach (var ik in iks)
        {
            var d = ik.data;
            string legName = ik.name;

            // ---- chain checks
            bool chainOK = d.mid && d.root && d.tip && (d.mid.parent == d.root) && (d.tip.parent == d.mid);
            string chain = chainOK ? "OK" : "BAD";

            // ---- scale checks (uniform & positive)
            bool ScaleOK(Transform t){
                if (!t) return false;
                var s = t.lossyScale;
                bool uniform = Mathf.Abs(s.x - s.y) < 0.001f && Mathf.Abs(s.y - s.z) < 0.001f;
                bool positive = s.x>0 && s.y>0 && s.z>0;
                return uniform && positive;
            }
            bool scaleOK = ScaleOK(d.root) && ScaleOK(d.mid) && ScaleOK(d.tip);

            // ---- reach & overshoot
            float upper = d.root && d.mid ? Vector3.Distance(d.root.position, d.mid.position) : 0f;
            float lower = d.mid && d.tip ? Vector3.Distance(d.mid.position, d.tip.position) : 0f;
            float maxReach = upper + lower;
            float dist = (d.target && d.root) ? Vector3.Distance(d.root.position, d.target.position) : 0f;
            float overshoot = Mathf.Max(0, dist - maxReach);

            // ---- knee angle & hint geometry
            float kneeDeg = 0f;
            if (d.root && d.mid && d.tip)
            {
                Vector3 a = (d.root.position - d.mid.position).normalized;
                Vector3 b = (d.tip.position  - d.mid.position).normalized;
                kneeDeg = Vector3.Angle(a, b); // 180 = straight
            }
            float hintDist = (d.hint && d.mid) ? Vector3.Distance(d.hint.position, d.mid.position) : 0f;
            float hintPlaneAngle = 0f;
            if (d.root && d.mid && d.tip && d.hint)
            {
                Vector3 a = (d.root.position - d.mid.position).normalized;
                Vector3 b = (d.tip.position  - d.mid.position).normalized;
                Vector3 planeN = Vector3.Cross(a, b).normalized;   // limb plane normal
                hintPlaneAngle = Vector3.Angle((d.hint.position - d.mid.position).normalized, planeN);
            }

            // ---- parenting sanity (Targets/Hints should not be under bones)
            bool targetUnderBone = d.target && IsChildOf(d.target, d.root);
            bool hintUnderBone   = d.hint   && IsChildOf(d.hint,   d.root);

            // ---- weights & options
            float wPos = d.targetPositionWeight;
            float wRot = d.targetRotationWeight;
            float wHint = d.hintWeight;
            bool keepPos = d.maintainTargetPositionOffset;
            bool keepRot = d.maintainTargetRotationOffset;

            // ---- duplicate chain detection
            string key = $"{(d.root?d.root.name:"null")}->{(d.mid?d.mid.name:"null")}->{(d.tip?d.tip.name:"null")}";
            if (!seen.TryGetValue(key, out var list)) { list = new List<TwoBoneIKConstraint>(); seen[key] = list; }
            list.Add(ik);

            // ---- print
            string msg =
              $"[IKAudit:{legName}] Chain:{chain}  ScaleOK:{scaleOK}  " +
              $"Reach:{dist:F3}/{maxReach:F3} Overshoot:{overshoot:F3}  Knee:{kneeDeg:F1}°  " +
              $"HintDist:{hintDist:F3} HintPlaneAngle:{hintPlaneAngle:F1}°  " +
              $"Weights(Pos/Rot/Hint):{wPos:F2}/{wRot:F2}/{wHint:F2}  " +
              $"Maintain(Pos/Rot):{keepPos}/{keepRot}  " +
              $"TargetChildOfBone:{targetUnderBone} HintChildOfBone:{hintUnderBone}";
            Debug.Log(msg);

            // quick hints
            if (!chainOK) Debug.LogWarning($"[IKAudit:{legName}] ❌ Root→Mid→Tip must be direct parent chain.");
            if (!scaleOK) Debug.LogWarning($"[IKAudit:{legName}] ⚠️ Non-uniform or negative scale in chain parents.");
            if (overshoot > 0.005f) Debug.LogWarning($"[IKAudit:{legName}] ⚠️ Target beyond reach → clamp or raise body.");
            if (kneeDeg < 5f) Debug.LogWarning($"[IKAudit:{legName}] ⚠️ Knee almost straight (flip-risk). Move target closer or bias hint.");
            if (hintDist < 0.03f) Debug.LogWarning($"[IKAudit:{legName}] ⚠️ Hint too close → move outward 5–10cm.");
            if (hintPlaneAngle < 15f) Debug.LogWarning($"[IKAudit:{legName}] ⚠️ Hint in limb plane → move more to side/front/back.");
            if (targetUnderBone || hintUnderBone) Debug.LogWarning($"[IKAudit:{legName}] ❌ Target/Hint parented under bones. Re-parent under the Rig.");
        }

        // duplicates
        foreach (var kv in seen)
            if (kv.Value.Count > 1)
                Debug.LogWarning($"[IKAudit] ❌ Duplicate constraints on chain {kv.Key}: {kv.Value.Count} components.");

        // simple “legs last” rule of thumb:
        Debug.Log("[IKAudit] Tip: place leg IK groups at the bottom of the Rig so feet solve last.");
    }

    static bool IsChildOf(Transform t, Transform ancestor)
    {
        for (var p = t; p != null; p = p.parent) if (p == ancestor) return true;
        return false;
    }
}
