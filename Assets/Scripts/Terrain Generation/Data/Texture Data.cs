using System.Linq;
using UnityEngine;

[CreateAssetMenu]
public class TextureData : UpdatableData
{
    private const int TextureSize = 512;
    private const TextureFormat TextureFormat = UnityEngine.TextureFormat.RGB565;

    public Layer[] layers;
    private float _savedMinHeight;
    private float _savedMaxHeight;

    public void ApplyToMaterial(Material material)
    {
        material.SetInt("layerCount", layers.Length);
        material.SetColorArray("baseColors", layers.Select(x => x.tint).ToArray());
        material.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
        material.SetFloatArray("baseBlends", layers.Select(x => x.blendStrength).ToArray());
        material.SetFloatArray("baseColorStrengths", layers.Select(x => x.tintStrength).ToArray());
        material.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());
        Texture2DArray textureArray = GenerateTextureArray(layers.Select(x => x.Texture).ToArray());
        material.SetTexture("baseTextures", textureArray);

        UpdateMeshHeight(material, _savedMinHeight, _savedMaxHeight);
    }

    public void UpdateMeshHeight(Material material, float minHeight, float maxHeight)
    {
        _savedMaxHeight = maxHeight;
        _savedMinHeight = minHeight;

        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }

    Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        Texture2DArray textureArray =
            new Texture2DArray(TextureSize, TextureSize, textures.Length, TextureFormat, true);
        for (int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }

        textureArray.Apply();
        return textureArray;
    }

    [System.Serializable]
    public class Layer
    {
        public Texture2D Texture;
        public Color tint;
        [Range(0, 1)] public float tintStrength;
        [Range(0, 1)] public float startHeight;
        [Range(0, 1)] public float blendStrength;
        public float textureScale;
    }
}