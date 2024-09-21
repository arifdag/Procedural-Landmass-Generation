using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = System.Random;

public static class Noise
{
    public enum NormalizeMode
    {
        Local,
        Global
    }

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale, int numOfOctaves,
        float persistance,
        float lacunarity, int seed, Vector2 offset, NormalizeMode normalizeMode)
    {
        if (scale == 0)
            scale = 0.33f;

        float amplitude = 1;
        float frequency = 1;
        float maxPossibleHeight = 0;


        Random rnd = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[numOfOctaves];

        for (int i = 0; i < numOfOctaves; i++)
        {
            octaveOffsets[i] = new Vector2(rnd.Next(-100000, 100000) + offset.x, rnd.Next(-100000, 100000) + offset.y);
            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        float[,] noiseMap = new float[mapWidth, mapHeight];
        float localMaxNoiseHeight = float.MinValue;
        float localMinNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2;
        float halfHeight = mapHeight / 2;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < numOfOctaves; i++)
                {
                    float noiseValue = Mathf.PerlinNoise((x - halfWidth + octaveOffsets[i].x) / scale * frequency,
                        (y - halfHeight + octaveOffsets[i].y) / scale * frequency) * 2 - 1;
                    noiseHeight += noiseValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > localMaxNoiseHeight)
                    localMaxNoiseHeight = noiseHeight;
                else if (noiseHeight < localMinNoiseHeight)
                    localMinNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (NormalizeMode.Local == normalizeMode)
                    noiseMap[x, y] = Mathf.InverseLerp(localMinNoiseHeight, localMaxNoiseHeight, noiseMap[x, y]);
                else
                { 
                    float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, float.MaxValue); 
                    
                    // For more consistent heights (could be useful when implementing biomes (???))
                    // Potential performance issues
                    /*
                    float intensity = 1.5f;
                    noiseMap[x,y] = 1 / (1 + Mathf.Exp(noiseMap[x,y] * intensity)); */
                }
            }
        }

        return noiseMap;
    }
}