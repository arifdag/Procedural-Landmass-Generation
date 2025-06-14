using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorleyNoiseGenerator))]
public class WorleyNoiseGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        EditorGUILayout.Space();

        // Reference to the generator
        var gen = (WorleyNoiseGenerator)target;

        // Generate button
        if (GUILayout.Button("Generate 3D Noise Texture"))
        {
            Undo.RecordObject(gen, "Generate 3D Noise Texture");
            gen.GenerateTexture();
            EditorUtility.SetDirty(gen);
        }

        // Save-as-asset button
        if (GUILayout.Button("Save Texture as Asset…"))
        {
            // Make sure it's generated
            if (gen.generatedTexture == null)
                gen.GenerateTexture();

            // Convert RT³ → Texture³
            Texture3D tex = gen.CopyRenderTextureToTexture3D(gen.generatedTexture);
            if (tex == null)
            {
                Debug.LogError("Failed to convert RenderTexture to Texture3D.");
                return;
            }

            // Prompt the user for a path
            string path = EditorUtility.SaveFilePanelInProject(
                "Save 3D Noise Texture",
                "NewWorleyNoise",
                "asset",
                "Choose location to save your 3D noise texture."
            );
            if (string.IsNullOrEmpty(path))
                return;

            // Create the asset
            AssetDatabase.CreateAsset(tex, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Highlight it in the Project window
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = tex;
        }
    }
}