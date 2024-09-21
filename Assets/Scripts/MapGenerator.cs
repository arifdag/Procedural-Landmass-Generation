using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    /* // If you use 32-mesh this value can be any number up to 65535.
     However, using a larger number is not recommended as it would significantly increase computation without noticeable improvement.*/
    [SerializeField] private Noise.NormalizeMode _normalizeMode;
    [SerializeField] private bool useFallof;
   
    public const int mapChunkSize = 241;

    public static readonly int[] LodIncrements = new[] { 1, 2, 4, 6, 8, 10, 12, 16, 20, 24, 30 };
    [Range(0, 10)] [SerializeField] private int editorLOD;


    [SerializeField] private float noiseScale;
    [SerializeField] private int numOfOctaves;
    [SerializeField] private float persistance;
    [Range(0, 1)] [SerializeField] private float lacunarity;
    [SerializeField] private int seed;
    [SerializeField] private Vector2 offset;


    [SerializeField] private float meshHeightMultiplier;
    [SerializeField] private AnimationCurve meshHeightCurve;

    [SerializeField] private TerrainType[] regions;

    private ConcurrentQueue<MapThreadInfo<MapData>> _mapDataThreadInfos = new ConcurrentQueue<MapThreadInfo<MapData>>();
    private ConcurrentQueue<MapThreadInfo<MeshData>> _meshDataThreadInfos =
        new ConcurrentQueue<MapThreadInfo<MeshData>>();

    private float[,] falloffMap;

    private void Awake()
    {
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    public enum DrawMode
    {
        NoiseMap,
        ColorMap,
        Falloff,
        DrawMesh
    }

    public DrawMode drawMode;

    private void Update()
    {
        if (_mapDataThreadInfos.Count > 0)
        {
            for (int i = 0; i < _mapDataThreadInfos.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = new MapThreadInfo<MapData>();
                _mapDataThreadInfos.TryDequeue(out threadInfo);
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (_meshDataThreadInfos.Count > 0)
        {
            for (int i = 0; i < _meshDataThreadInfos.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = new MapThreadInfo<MeshData>();
                _meshDataThreadInfos.TryDequeue(out threadInfo);
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate { MapDataThread(center, callback); };
        Thread thread = new Thread(threadStart);
        thread.Start();
    }

    void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapdData(center);
        _mapDataThreadInfos.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate { MeshDataThread(mapData, lod, callback); };
        Thread thread = new Thread(threadStart);
        thread.Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData =
            MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        _meshDataThreadInfos.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
    }

    private MapData GenerateMapdData(Vector2 center)
    {
        float[,] noiseMap =
            Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, noiseScale, numOfOctaves, persistance, lacunarity, seed,
                center + offset, _normalizeMode);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                if (useFallof)
                {
                    float noiseValue = noiseMap[x, y] - falloffMap[x, y];
                    noiseMap[x, y] = Mathf.Clamp01(noiseValue);
                }
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight >= regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                    }
                    else
                        break;
                }
            }
        }

        return new MapData(noiseMap, colorMap);
    }

    public void DrawMapOnEditor()
    {
        MapData mapData = GenerateMapdData(Vector2.zero);
        float[,] noiseMap = mapData.heightMap;
        Color[] colorMap = mapData.colorMap;

        MapDisplay mapDisplay = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
            mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        else if (drawMode == DrawMode.ColorMap)
            mapDisplay.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.DrawMesh)
            mapDisplay.DrawMesh(
                MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve, editorLOD),
                TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.Falloff)
        {
            mapDisplay.DrawTexture(
                TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
        }
    }

    private void OnValidate()
    {
        if (lacunarity < 1)
            lacunarity = 1;
        if (numOfOctaves < 0)
            numOfOctaves = 0;
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}