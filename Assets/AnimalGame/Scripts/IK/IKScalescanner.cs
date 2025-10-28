using UnityEngine;

public class IKScaleScanner : MonoBehaviour
{
    public Transform root; // hip/upper leg
    public Transform mid;  // knee
    public Transform tip;  // foot/ankle

    void Start()
    {
        Debug.Log("=== SCALE SCAN (root→mid→tip, including parents) ===");
        DumpChain("ROOT", root);
        DumpChain("MID ", mid);
        DumpChain("TIP ", tip);
    }

    void DumpChain(string tag, Transform t)
    {
        int hops = 0;
        for (var p = t; p != null && hops < 16; p = p.parent, hops++)
        {
            var s = p.lossyScale;
            string uni = Mathf.Abs(s.x - s.y) < 0.0001f && Mathf.Abs(s.y - s.z) < 0.0001f ? "uniform" : "NON-UNIFORM";
            string sign = (s.x > 0 && s.y > 0 && s.z > 0) ? "pos" : "NEGATIVE";
            Debug.Log($"{tag} [{hops}] {p.name}  lossy=({s.x:F3},{s.y:F3},{s.z:F3})  {uni}  {sign}");
        }
    }
}
