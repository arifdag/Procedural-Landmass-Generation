using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = System.Random;

public static class Noise
{
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale, int numOfOctaves, float persistance,
        float lacunarity, int seed)
    {
        if (scale == 0)
            scale = 0.33f;

        Random rnd = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[numOfOctaves];
        for (int i = 0; i < numOfOctaves; i++)
            octaveOffsets[i] = new Vector2(rnd.Next(-100000, 100000), rnd.Next(-100000, 100000));
            
        float[,] noiseMap = new float[mapWidth, mapHeight];
        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2;
        float halfHeight = mapHeight / 2;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)  
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < numOfOctaves; i++)  
                {
                    float noiseValue = Mathf.PerlinNoise((x-halfWidth) / scale * frequency + octaveOffsets[i].x, (y-halfHeight) / scale * frequency + octaveOffsets[i].y) * 2 - 1;
                    noiseHeight += noiseValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                    maxNoiseHeight = noiseHeight;
                else if (noiseHeight < minNoiseHeight)
                    minNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                noiseMap[x,y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x,y]);
            }
        }

        return noiseMap;
    }
}