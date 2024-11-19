using UnityEngine;
using UnityEngine.Serialization;


public class MapPreview : MonoBehaviour
{
    /*  If you use 32-mesh this value can be any number up to 65535.
     However, using a larger number is not recommended as it would significantly increase computation without noticeable improvement.*/
    public static readonly int[] LodIncrements = new[] { 1, 2, 4, 6, 8 };

    [Range(0, MeshSettings.NumSupportedLods - 1)] [SerializeField]
    private int editorLOD;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;
    public PlacementSettings placementSettings;

    [SerializeField] private Material terrainMaterial;
    [SerializeField] private Renderer textureRenderer;

    [FormerlySerializedAs("_meshFilter")] [SerializeField]
    private MeshFilter meshFilter;

    private float[,] _falloffMap;

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
        textureData.UpdateMeshHeight(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);
        HeightMap heightMap =
            HeightMapGenerator.GenerateHeightMap(meshSettings.NumVerticesPerLine, meshSettings.NumVerticesPerLine,
                heightMapSettings, Vector2.zero);
        float[,] noiseMap = heightMap.values;


        if (drawMode == DrawMode.NoiseMap)
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
        else if (drawMode == DrawMode.DrawMesh)
        {
            MeshData meshData = MeshGenerator.GenerateTerrainMesh(noiseMap, meshSettings, editorLOD);
            DrawMesh(meshData);
            /*PlacementManager.StartPlacingObjects(placementSettings.placementDatas, meshData,
                heightMap, meshSettings.meshScale); */
        }
        else if (drawMode == DrawMode.Falloff)
        {
            DrawTexture(
                TextureGenerator.TextureFromHeightMap(new HeightMap(
                    FalloffGenerator.GenerateFalloffMap(meshSettings.NumVerticesPerLine,
                        heightMapSettings.falloffCurve), 0, 1)));
        }
    }


    private void DrawTexture(Texture2D texture2D)
    {
        textureRenderer.sharedMaterial.mainTexture = texture2D;
        textureRenderer.transform.localScale = new Vector3(texture2D.width, 1, texture2D.height) / 10f;

        textureRenderer.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    private void DrawMesh(MeshData meshData)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();

        textureRenderer.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
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