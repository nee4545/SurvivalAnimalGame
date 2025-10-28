using UnityEngine;

public class IKLegDoctor : MonoBehaviour
{
    public Transform root, mid, tip, target, hint;
    [Range(0.01f, 0.2f)] public float gizmoSize = 0.04f;
    public bool logEveryFrame = false;

    float upperLen, lowerLen;

    void OnValidate() { CacheLengths(); }
    void Start() { CacheLengths(); Validate("Start"); }
    void Update() { if (logEveryFrame) Validate("Tick"); }

    void CacheLengths()
    {
        if (root && mid) upperLen = Vector3.Distance(root.position, mid.position);
        if (mid && tip) lowerLen = Vector3.Distance(mid.position, tip.position);
    }

    void Validate(string tag)
    {
        if (!root || !mid || !tip || !target || !hint) { Report(tag, "Assign all refs"); return; }

        bool chainOK = (mid.parent == root) && (tip.parent == mid);

        Vector3 sR = root.lossyScale, sM = mid.lossyScale, sT = tip.lossyScale;
        bool scaleOK =
          NearlyOne(sR) && NearlyOne(sM) && NearlyOne(sT) &&
          sR.x > 0 && sR.y > 0 && sR.z > 0 && sM.x > 0 && sM.y > 0 && sM.z > 0 && sT.x > 0 && sT.y > 0 && sT.z > 0;

        float maxReach = Mathf.Max(0.0001f, upperLen + lowerLen);
        float dist = Vector3.Distance(root.position, target.position);
        float overshoot = Mathf.Max(0, dist - maxReach);

        float hintDist = Vector3.Distance(hint.position, mid.position);
        Vector3 a = (root.position - mid.position).normalized;
        Vector3 b = (tip.position - mid.position).normalized;
        Vector3 planeNormal = Vector3.Cross(a, b).normalized;
        float hintPlaneAngle = Vector3.Angle((hint.position - mid.position).normalized, planeNormal);

        string msg =
          $"ChainOK:{chainOK}  ScaleOK:{scaleOK}  Reach:{dist:F3}/{maxReach:F3}  Overshoot:{overshoot:F3}  " +
          $"HintDist:{hintDist:F3}  HintPlaneAngle:{hintPlaneAngle:F1}°";

        if (!chainOK) msg += "  [WARN chain not contiguous]";
        if (!scaleOK) msg += "  [WARN non-uniform/negative scale]";
        if (overshoot > 0.005f) msg += "  [CLAMP target or raise body]";
        if (hintDist < 0.03f) msg += "  [MOVE hint outward]";
        if (hintPlaneAngle < 15f) msg += "  [MOVE hint more to side/front/back]";

        Report(tag, msg);
    }

    static bool NearlyOne(Vector3 s)
    {
        return Mathf.Abs(s.x - 1f) < 0.01f && Mathf.Abs(s.y - 1f) < 0.01f && Mathf.Abs(s.z - 1f) < 0.01f;
    }

    void Report(string tag, string m) { Debug.Log($"[LegDoctor:{name}:{tag}] {m}"); }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        if (root && mid) Gizmos.DrawLine(root.position, mid.position);
        if (mid && tip) Gizmos.DrawLine(mid.position, tip.position);
        if (target) { Gizmos.color = Color.green; Gizmos.DrawSphere(target.position, gizmoSize); }
        if (hint) { Gizmos.color = Color.yellow; Gizmos.DrawSphere(hint.position, gizmoSize); }
        if (root) { Gizmos.color = Color.white; Gizmos.DrawWireSphere(root.position, gizmoSize * 0.8f); }
        if (mid) { Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(mid.position, gizmoSize * 0.8f); }
        if (tip) { Gizmos.color = Color.blue; Gizmos.DrawWireSphere(tip.position, gizmoSize * 0.8f); }
    }
}
