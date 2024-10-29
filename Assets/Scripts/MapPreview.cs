using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class MapPreview : MonoBehaviour
{
    /*  If you use 32-mesh this value can be any number up to 65535.
     However, using a larger number is not recommended as it would significantly increase computation without noticeable improvement.*/
    public static readonly int[] LodIncrements = new[] { 1, 2, 4, 6, 8 };

    [Range(0, MeshSettings.numSupportedLods - 1)] [SerializeField]
    private int editorLOD;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;

    [SerializeField] private Material terrainMaterial;
    [SerializeField] private Renderer textureRenderer;
    [SerializeField] private MeshFilter _meshFilter;
    [SerializeField] private MeshRenderer _meshRenderer;

    private float[,] falloffMap;

    public enum DrawMode
    {
        NoiseMap,
        Falloff,
        DrawMesh
    }

    public DrawMode drawMode;


    public void DrawMapOnEditor()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeight(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        HeightMap heightMap =
            HeightMapGenerator.GenerateHeightMap(meshSettings.numVerticesPerLine, meshSettings.numVerticesPerLine,
                heightMapSettings, Vector2.zero);
        float[,] noiseMap = heightMap.values;

        if (drawMode == DrawMode.NoiseMap)
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
        else if (drawMode == DrawMode.DrawMesh)
            DrawMesh(
                MeshGenerator.GenerateTerrainMesh(noiseMap, meshSettings, editorLOD));
        else if (drawMode == DrawMode.Falloff)
        {
            DrawTexture(
                TextureGenerator.TextureFromHeightMap(new HeightMap(
                    FalloffGenerator.GenerateFalloffMap(meshSettings.numVerticesPerLine,
                        heightMapSettings.falloffCurve), 0, 1)));
        }
    }


    public void DrawTexture(Texture2D texture2D)
    {
        textureRenderer.sharedMaterial.mainTexture = texture2D;
        textureRenderer.transform.localScale = new Vector3(texture2D.width, 1, texture2D.height) /10f;

        textureRenderer.gameObject.SetActive(true);
        _meshFilter.gameObject.SetActive(false);
    }

    public void DrawMesh(MeshData meshData)
    {
        _meshFilter.sharedMesh = meshData.CreateMesh();

        textureRenderer.gameObject.SetActive(false);
        _meshFilter.gameObject.SetActive(true);
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
            DrawMapOnEditor();
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    private void OnValidate()
    {
        if (meshSettings != null)
        {
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (heightMapSettings != null)
        {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }
}