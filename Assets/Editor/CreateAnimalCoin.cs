using UnityEngine;
using UnityEditor;

public static class CreateAnimalCoin
{
    [MenuItem("Tools/Create Animal Coin")]
    public static void CreateCoin()
    {
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coin.name = "Animal Coin";

        // Make it coin-shaped
        coin.transform.localScale = new Vector3(1.5f, 0.12f, 1.5f);

        // Rotate so the face looks forward in the scene
        coin.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Add material
        Renderer renderer = coin.GetComponent<Renderer>();

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = "Animal Coin Material";

        mat.SetColor("_BaseColor", new Color(1f, 0.65f, 0.18f));
        mat.SetFloat("_Metallic", 0.4f);
        mat.SetFloat("_Smoothness", 0.55f);

        renderer.sharedMaterial = mat;

        // Optional: select the coin after creation
        Selection.activeGameObject = coin;
    }
}