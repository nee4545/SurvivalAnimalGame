#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SlowMotionHuntSpeedometerPrefabCreator
{
    private const string PrefabPath = "Assets/SlowMotionHunt/Prefabs/SlowMotionHuntSpeedometer.prefab";

    [MenuItem("Tools/Wild Paws/Create Slow Motion Hunt Speedometer Prefab")]
    public static void CreatePrefab()
    {
        EnsureFolders();

        GameObject root = new GameObject("SlowMotionHuntSpeedometer", typeof(RectTransform), typeof(CanvasGroup), typeof(SlowMotionHuntUI));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0f);
        rootRect.anchorMax = new Vector2(0.5f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.anchoredPosition = new Vector2(0f, 150f);
        rootRect.sizeDelta = new Vector2(620f, 120f);

        GameObject panel = CreateUIObject("Panel", root.transform, new Vector2(620f, 120f));
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.02f, 0.025f, 0.03f, 0.76f);
        panelImage.raycastTarget = false;

        GameObject track = CreateUIObject("Track", panel.transform, new Vector2(520f, 26f));
        RectTransform trackRect = track.GetComponent<RectTransform>();
        trackRect.anchoredPosition = new Vector2(0f, 0f);
        Image trackImage = track.AddComponent<Image>();
        trackImage.color = new Color(0.85f, 0.88f, 0.92f, 0.42f);
        trackImage.raycastTarget = false;

        GameObject greenZone = CreateUIObject("GreenZone", track.transform, new Vector2(105f, 38f));
        RectTransform greenRect = greenZone.GetComponent<RectTransform>();
        greenRect.anchoredPosition = Vector2.zero;
        Image greenImage = greenZone.AddComponent<Image>();
        greenImage.color = new Color(0.20f, 1f, 0.36f, 0.88f);
        greenImage.raycastTarget = false;

        GameObject centerLine = CreateUIObject("CenterLine", greenZone.transform, new Vector2(4f, 48f));
        Image centerImage = centerLine.AddComponent<Image>();
        centerImage.color = new Color(1f, 1f, 1f, 0.75f);
        centerImage.raycastTarget = false;

        GameObject arrow = CreateUIObject("Arrow", track.transform, new Vector2(34f, 70f));
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        arrowRect.anchoredPosition = new Vector2(-260f, 0f);
        Image arrowImage = arrow.AddComponent<Image>();
        arrowImage.color = new Color(1f, 0.92f, 0.25f, 1f);
        arrowImage.raycastTarget = false;

        // Make the arrow look like a tall needle by adding a small cap.
        GameObject arrowNeedle = CreateUIObject("Needle", arrow.transform, new Vector2(8f, 70f));
        Image needleImage = arrowNeedle.AddComponent<Image>();
        needleImage.color = new Color(1f, 0.92f, 0.25f, 1f);
        needleImage.raycastTarget = false;

        GameObject leftLabel = CreateText("MISS", panel.transform, new Vector2(-260f, -42f), 16, new Color(1f, 1f, 1f, 0.58f));
        GameObject hitLabel = CreateText("CRITICAL", panel.transform, new Vector2(0f, 42f), 19, new Color(0.55f, 1f, 0.58f, 1f));
        GameObject rightLabel = CreateText("MISS", panel.transform, new Vector2(260f, -42f), 16, new Color(1f, 1f, 1f, 0.58f));

        SlowMotionHuntUI ui = root.GetComponent<SlowMotionHuntUI>();
        ui.root = root;
        ui.track = trackRect;
        ui.greenZone = greenRect;
        ui.arrow = arrowRect;
        ui.arrowImage = arrowImage;
        ui.greenZoneImage = greenImage;
        ui.arrowSpeed = 1.8f;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Slow Motion Hunt Speedometer Created",
            "Prefab created at:\n" + PrefabPath + "\n\nPlace it under your Canvas and assign it to SlowMotionHuntController > Hunt UI.",
            "Done"
        );

        Debug.Log("Created Slow Motion Hunt Speedometer prefab at " + PrefabPath);
    }

    private static GameObject CreateUIObject(string name, Transform parent, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        return go;
    }

    private static GameObject CreateText(string text, Transform parent, Vector2 position, int fontSize, Color color)
    {
        GameObject go = CreateUIObject(text + " Label", parent, new Vector2(180f, 32f));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;

        Text label = go.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.raycastTarget = false;

        return go;
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/SlowMotionHunt"))
            AssetDatabase.CreateFolder("Assets", "SlowMotionHunt");

        if (!AssetDatabase.IsValidFolder("Assets/SlowMotionHunt/Scripts"))
            AssetDatabase.CreateFolder("Assets/SlowMotionHunt", "Scripts");

        if (!AssetDatabase.IsValidFolder("Assets/SlowMotionHunt/Editor"))
            AssetDatabase.CreateFolder("Assets/SlowMotionHunt", "Editor");

        if (!AssetDatabase.IsValidFolder("Assets/SlowMotionHunt/Prefabs"))
            AssetDatabase.CreateFolder("Assets/SlowMotionHunt", "Prefabs");
    }
}
#endif
