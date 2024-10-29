using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

public static class Noise
{
    public enum NormalizeMode
    {
        Local,
        Global
    }

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCenter)
    {
        float amplitude = 1;
        float frequency = 1;
        float maxPossibleHeight = 0;


        Random rnd = new System.Random(settings.seed);
        Vector2[] octaveOffsets = new Vector2[settings.numOfOctaves];

        for (int i = 0; i < settings.numOfOctaves; i++)
        {
            octaveOffsets[i] = new Vector2(rnd.Next(-100000, 100000) + settings.offset.x + sampleCenter.x,
                rnd.Next(-100000, 100000) + settings.offset.y + sampleCenter.y);
            maxPossibleHeight += amplitude;
            amplitude *= settings.persistance;
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

                for (int i = 0; i < settings.numOfOctaves; i++)
                {
                    float noiseValue = Mathf.PerlinNoise(
                        (x - halfWidth + octaveOffsets[i].x) / settings.scale * frequency,
                        (y - halfHeight + octaveOffsets[i].y) / settings.scale * frequency) * 2 - 1;
                    noiseHeight += noiseValue * amplitude;

                    amplitude *= settings.persistance;
                    frequency *= settings.lacunarity;
                }

                if (noiseHeight > localMaxNoiseHeight)
                    localMaxNoiseHeight = noiseHeight;
                if (noiseHeight < localMinNoiseHeight)
                    localMinNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
                
                if(settings.normalizeMode == NormalizeMode.Global)
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

        if (NormalizeMode.Local == settings.normalizeMode)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(localMinNoiseHeight, localMaxNoiseHeight, noiseMap[x, y]);
                }
            }
        }

        return noiseMap;
    }
}

[Serializable]
public class NoiseSettings
{
    public Noise.NormalizeMode normalizeMode;

    public float scale = 50;
    public int numOfOctaves = 6;
    [Range(0, 1)] [SerializeField] public float persistance = .6f;
    public float lacunarity = 2;
    public int seed;
    public Vector2 offset;

    public void ValidateValues()
    {
        scale = Mathf.Max(scale, 0.01f);
        numOfOctaves = Mathf.Max(numOfOctaves, 1);
        lacunarity = Mathf.Max(lacunarity, 1);
        persistance = Mathf.Clamp01(persistance);
    }
}