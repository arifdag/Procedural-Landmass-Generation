using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    /* // If you use 32-mesh this value can be any number up to 65535.
     However, using a larger number is not recommended as it would significantly increase computation without noticeable improvement.*/
    public const int mapChunkSize = 241;

    public static readonly int[] LodIncrements = new[] {1, 2, 4, 6, 8, 10, 12, 16, 20, 24, 30};
    [Range(0,10)]
    [SerializeField] private int levelOfDetail;


    [SerializeField] private float noiseScale;
    [SerializeField] private int numOfOctaves;
    [SerializeField] private float persistance;
    [Range(0, 1)] [SerializeField] private float lacunarity;
    [SerializeField] private int seed;


    [SerializeField] private float meshHeightMultiplier;
    [SerializeField] private AnimationCurve meshHeightCurve;

    [SerializeField] private TerrainType[] regions;

    public enum DrawMode
    {
        NoiseMap,
        ColorMap,
        DrawMesh
    }

    public DrawMode drawMode;

    public void GenerateMap()
    {
        float[,] noiseMap =
            Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, noiseScale, numOfOctaves, persistance, lacunarity, seed);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        break;
                    }
                }
            }
        }

        MapDisplay mapDisplay = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
            mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        else if (drawMode == DrawMode.ColorMap)
            mapDisplay.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.DrawMesh)
            mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve,levelOfDetail),
                TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
    }

    private void OnValidate()
    {
        if (lacunarity < 1)
            lacunarity = 1;
        if (numOfOctaves < 0)
            numOfOctaves = 0;
    }
}

[Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}