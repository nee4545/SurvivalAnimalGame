using System.IO;
using UnityEditor;
using UnityEngine;

public class BatchAnimalGameViewCapture : EditorWindow
{
    private string animalsRootName = "Animals";
    private string outputFolder = "Assets/AnimalSprites";
    private int resolution = 512;

    [MenuItem("Tools/Batch Capture Animals (Game View)")]
    static void Open()
    {
        GetWindow<BatchAnimalGameViewCapture>("Batch Animal Capture");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Batch Animal Game View Capture", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Captures EXACTLY what the camera sees.\n" +
            "All animals must be children of a single GameObject.\n" +
            "Each animal is captured one-by-one using its GameObject name.",
            MessageType.Info
        );

        animalsRootName = EditorGUILayout.TextField("Animals Root Name", animalsRootName);
        resolution = EditorGUILayout.IntPopup(
            "Resolution",
            resolution,
            new[] { "256", "512", "1024" },
            new[] { 256, 512, 1024 }
        );
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        EditorGUILayout.Space(12);

        if (GUILayout.Button("Capture All Animals", GUILayout.Height(40)))
        {
            CaptureAll();
        }
    }

    private void CaptureAll()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("No Camera.main found in the scene.");
            return;
        }

        GameObject animalsRoot = GameObject.Find(animalsRootName);
        if (animalsRoot == null)
        {
            Debug.LogError($"No GameObject named '{animalsRootName}' found in the scene.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
        }

        RenderTexture rt = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);

        RenderTexture prevRT = cam.targetTexture;

        try
        {
            int count = animalsRoot.transform.childCount;

            for (int i = 0; i < count; i++)
            {
                Transform child = animalsRoot.transform.GetChild(i);
                if (!child.gameObject.activeSelf)
                    child.gameObject.SetActive(true);

                // Disable all others
                for (int j = 0; j < count; j++)
                    animalsRoot.transform.GetChild(j).gameObject.SetActive(j == i);

                EditorUtility.DisplayProgressBar(
                    "Capturing Animals",
                    child.name,
                    (float)i / count
                );

                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
                tex.Apply();

                string safeName = MakeFileSafe(child.name);
                string path = $"{outputFolder}/{safeName}.png";
                File.WriteAllBytes(path, tex.EncodeToPNG());

                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
        }
        finally
        {
            cam.targetTexture = prevRT;
            RenderTexture.active = null;
            DestroyImmediate(rt);
            DestroyImmediate(tex);
            EditorUtility.ClearProgressBar();

            // Re-enable all animals
            foreach (Transform t in animalsRoot.transform)
                t.gameObject.SetActive(true);

            AssetDatabase.Refresh();
        }

        Debug.Log("Batch animal capture complete.");
    }

    private static string MakeFileSafe(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
