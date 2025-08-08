using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    // smoothing factor for more stable FPS display
    [Tooltip("How quickly the FPS display smooths between updates (0 = instant, 1 = very slow).")]
    [Range(0f, 1f)]
    public float smoothing = 0.1f;

    private float deltaTime = 0.0f;

    void Update()
    {
        // accumulate a smoothed deltaTime
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * smoothing;
    }

    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        // style the label
        GUIStyle style = new GUIStyle();
        Rect rect = new Rect(10, 10, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 100;
        style.normal.textColor = Color.yellow;

        // compute FPS
        float fps = 1.0f / deltaTime;
        string text = string.Format("{0:0.} FPS", fps);

        GUI.Label(rect, text, style);
    }
}
