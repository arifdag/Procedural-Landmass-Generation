// Put this in an Editor folder
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class NoiseTextureGenerator
{
    [MenuItem("Assets/Create/3D Noise Texture")]
    public static void CreateNoiseTexture()
    {
        int size = 64; // Or 128, 256. Larger = more detail but bigger texture
        TextureFormat format = TextureFormat.R8; // Single channel (red) for density
        TextureWrapMode wrapMode = TextureWrapMode.Repeat;

        Texture3D tex3D = new Texture3D(size, size, size, format, false);
        tex3D.wrapMode = wrapMode;

        Color[] colors = new Color[size * size * size];
        float frequency = 5.0f / size; // Adjust for noise frequency

        for (int z = 0; z < size; z++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Using Unity's PerlinNoise (2D) multiple times for a pseudo-3D effect.
                    // For true 3D noise, you'd implement 3D Perlin/Simplex or use a library.
                    float cVal = 0;
                    float amp = 0.5f;
                    float frq = frequency;
                    for(int i=0; i<4; i++) // 4 octaves
                    {
                        float n1 = Mathf.PerlinNoise((x + 0.5f) * frq, (y + 0.5f) * frq);
                        float n2 = Mathf.PerlinNoise((y + 0.5f) * frq, (z + 0.5f) * frq);
                        float n3 = Mathf.PerlinNoise((x + 0.5f) * frq, (z + 0.5f) * frq);
                        cVal += (n1 + n2 + n3) / 3.0f * amp;
                        amp *= 0.5f;
                        frq *= 2.0f;
                    }
                    
                    colors[x + y * size + z * size * size] = new Color(cVal, cVal, cVal, cVal);
                }
            }
        }

        tex3D.SetPixels(colors);
        tex3D.Apply();

        AssetDatabase.CreateAsset(tex3D, $"Assets/NoiseTexture3D_{size}.asset");
        AssetDatabase.SaveAssets();
        Debug.Log($"3D Noise Texture saved to Assets/NoiseTexture3D_{size}.asset");
    }
}
#endif