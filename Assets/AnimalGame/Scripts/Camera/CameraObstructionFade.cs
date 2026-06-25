using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class CameraObstructionFade : MonoBehaviour
{
    public Transform player;
    public LayerMask terrainLayer;
    public float fadeSpeed = 3f;
    public float minAlpha = 0.25f;

    private class FadeEntry
    {
        public Material[] originalMats;
        public Material[] fadedMats;
        public Renderer renderer;
    }

    private Dictionary<Renderer, FadeEntry> fadedObjects = new();
    private HashSet<Renderer> hitsThisFrame = new();

    void LateUpdate()
    {
        if (!player) return;

        hitsThisFrame.Clear();

        Vector3 from = transform.position;
        Vector3 to = player.position;
        float dist = Vector3.Distance(from, to);

        RaycastHit[] hits = Physics.RaycastAll(from, (to - from).normalized, dist, terrainLayer);
        foreach (var hit in hits)
        {
            if (hit.collider.GetComponentInParent<CameraFadeIgnore>())
                continue;

            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (!rend) continue;

            if (rend.GetComponentInParent<CameraFadeIgnore>())
                continue;

            hitsThisFrame.Add(rend);

            if (!fadedObjects.ContainsKey(rend))
            {
                var entry = new FadeEntry();
                entry.renderer = rend;
                entry.originalMats = rend.sharedMaterials;
                entry.fadedMats = new Material[entry.originalMats.Length];

                for (int i = 0; i < entry.originalMats.Length; i++)
                {
                    Material m = new Material(entry.originalMats[i]);
                    SetMaterialTransparent(m);
                    entry.fadedMats[i] = m;
                }

                rend.materials = entry.fadedMats;
                fadedObjects[rend] = entry;
            }

            foreach (var mat in fadedObjects[rend].fadedMats)
            {
                Color c = mat.color;
                c.a = Mathf.Lerp(c.a, minAlpha, Time.deltaTime * fadeSpeed);
                mat.color = c;
            }
        }

        // Restore fully faded objects that are no longer hit
        List<Renderer> toRestore = new();
        foreach (var kv in fadedObjects)
        {
            if (hitsThisFrame.Contains(kv.Key)) continue;

            bool done = true;
            foreach (var mat in kv.Value.fadedMats)
            {
                Color c = mat.color;
                c.a = Mathf.Lerp(c.a, 1f, Time.deltaTime * fadeSpeed);
                mat.color = c;
                if (c.a < 0.99f) done = false;
            }

            if (done) toRestore.Add(kv.Key);
        }

        foreach (var r in toRestore)
        {
            var entry = fadedObjects[r];
            r.sharedMaterials = entry.originalMats;
            foreach (var m in entry.fadedMats) Destroy(m);
            fadedObjects.Remove(r);
        }
    }

    void SetMaterialTransparent(Material mat)
    {
        mat.SetFloat("_Surface", 1); // Transparent (URP Lit)
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        // Ensure alpha is 1 when starting
        Color c = mat.color;
        c.a = 1f;
        mat.color = c;
    }
}
