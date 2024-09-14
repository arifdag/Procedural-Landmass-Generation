using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private int mapWidth;
    [SerializeField] private int mapHeight;
    [SerializeField] private float noiseScale;
    [SerializeField] private int numOfOctaves;
    [SerializeField] private float persistance;
    [Range(0, 1)] [SerializeField] private float lacunarity;
    [SerializeField] private int seed;

    [SerializeField] private float meshHeightMultiplier;
    
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
            Noise.GenerateNoiseMap(mapWidth, mapHeight, noiseScale, numOfOctaves, persistance, lacunarity, seed);

        Color[] colorMap = new Color[mapWidth * mapHeight];
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * mapWidth + x] = regions[i].color;
                        break;
                    }
                }
            }
        }

        MapDisplay mapDisplay = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
            mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        else if(drawMode== DrawMode.ColorMap)
            mapDisplay.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap,mapWidth,mapHeight));
        else if (drawMode == DrawMode.DrawMesh)
            mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap,meshHeightMultiplier),TextureGenerator.TextureFromColorMap(colorMap,mapWidth,mapHeight));
    }

    private void OnValidate()
    {
        if (mapWidth < 1)
            mapWidth = 1;
        if (mapHeight < 1)
            mapHeight = 1;
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