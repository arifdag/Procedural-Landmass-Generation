using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlacementManager : MonoBehaviour
{
    private static readonly ConcurrentQueue<InstantiateRequest> InstantiateQueue = new();

    private static readonly ConcurrentDictionary<PlacementSettings.PlacementData, (List<GameObject>, int)> ObjectPools =
        new();

    private static readonly ConcurrentQueue<PlacementSettings.PlacementData> DictonaryQueue = new();
    private static ConcurrentQueue<InstantiateRequest> GPUBatch = new();
    private static GPUInstancing gpuInstancing;

    private void Start()
    {
        gpuInstancing = FindObjectOfType<GPUInstancing>();
    }

    public static void StartPlacingObjects(PlacementSettings.PlacementData[] placementDatas, MeshData meshData,
        HeightMap heightMap, float meshScale, Transform parent, Vector2 chunkCoord, Vector3 worldPosition)
    {
        foreach (var placementData in placementDatas)
        {
            DictonaryQueue.Enqueue(placementData);
            PlaceObjects(meshData, heightMap, meshScale, placementData, parent, chunkCoord, worldPosition);
        }
    }

    private static void PlaceObjects(MeshData meshData, HeightMap heightMap, float meshScale,
        PlacementSettings.PlacementData placementData, Transform parent, Vector2 chunkCoord, Vector3 worldPosition)
    {
        int chunkSizeX = meshData._width;
        int chunkSizeZ = meshData._height;
        int halfSizeX = chunkSizeX / 2;
        int halfSizeZ = chunkSizeZ / 2;

        System.Random threadSafeRandom = new System.Random();

        for (int x = 0; x < chunkSizeX; x++)
        {
            for (int z = 0; z < chunkSizeZ; z++)
            {
                if (x < 0 || x >= heightMap.values.GetLength(0) || z < 0 || z >= heightMap.values.GetLength(1))
                {
                    continue;
                }

                float normalizedHeight =
                    Mathf.InverseLerp(heightMap.minValue, heightMap.maxValue, heightMap.values[x, z] * meshScale);

                if (Fitness(meshData, normalizedHeight, placementData, x, z) > 1 - placementData.density)
                {
                    float randomXOffset = (float)(threadSafeRandom.NextDouble() - 0.5);
                    float randomZOffset = (float)(threadSafeRandom.NextDouble() - 0.5);

                    Vector3 localPos = new Vector3(
                        (x - halfSizeX + randomXOffset) * meshScale,
                        heightMap.values[x, z] * meshScale,
                        (z - halfSizeZ + randomZOffset) * meshScale
                    );

                    Vector3 worldPos = localPos + worldPosition;
                    Vector3 normal = meshData.normals[z * chunkSizeX + x];
                    Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);

                    if (placementData.GPUInstancing)
                    {
                        GPUBatch.Enqueue(new InstantiateRequest(worldPos, rotation, placementData.scale));
                        continue;
                    }

                    GameObject prefab = placementData.prefabs[threadSafeRandom.Next(placementData.prefabs.Length)];
                    InstantiateQueue.Enqueue(new InstantiateRequest(placementData, prefab, worldPos, rotation, parent));
                }
            }
        }

        if (placementData.GPUInstancing)
        {
            gpuInstancing.AddBatch(chunkCoord, GPUBatch);
            GPUBatch = new ConcurrentQueue<InstantiateRequest>();
        }
    }

    private static float Fitness(MeshData meshData, float normalizedHeight,
        PlacementSettings.PlacementData placementData, int x, int z)
    {
        if (normalizedHeight < placementData.minHeight || normalizedHeight > placementData.maxHeight)
            return 0f;

        float fitness = normalizedHeight * placementData.heightWeight;

        float steepness = meshData.GetSteepness(x, z);
        if (steepness < placementData.minSteepness || steepness > placementData.maxSteepness)
            return 0f;

        int chunkSizeX = meshData._width;
        Vector3 normal = meshData.normals[z * chunkSizeX + x];
        float angle = Vector3.Angle(normal, Vector3.up);

        if (angle > 30)
            return 0f;

        float noise = Mathf.PerlinNoise(x * placementData.noiseScale, z * placementData.noiseScale);
        fitness += noise * placementData.noiseWeight;

        return Mathf.Clamp01(fitness);
    }


    readonly List<InstantiateRequest> _batch = new List<InstantiateRequest>();

    private void Update()
    {
        while (InstantiateQueue.TryDequeue(out var request))
        {
            _batch.Add(request);
            if (_batch.Count >= 100)
            {
                InstantiateBatch(_batch);
                _batch.Clear();
            }
        }

        while (DictonaryQueue.TryDequeue(out var placementData))
        {
            SetDictionary(placementData);
        }

        if (_batch.Count > 0)
            InstantiateBatch(_batch);
    }

    void SetDictionary(PlacementSettings.PlacementData placementData)
    {
        if (!ObjectPools.ContainsKey(placementData))
        {
            ObjectPools[placementData] = (new List<GameObject>(), 0);
        }
        else
        {
            ObjectPools[placementData] = (ObjectPools[placementData].Item1, 0);
        }
    }

    public class InstantiateRequest
    {
        public PlacementSettings.PlacementData PlacementData;
        public GameObject Prefab { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public int Scale { get; }
        public Transform Parent { get; }

        public InstantiateRequest(PlacementSettings.PlacementData placementData, GameObject prefab, Vector3 position,
            Quaternion rotation, Transform parent)
        {
            PlacementData = placementData;
            Prefab = prefab;
            Position = position;
            Rotation = rotation;
            Parent = parent;
        }

        public InstantiateRequest(Vector3 position, Quaternion rotation, int scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }
    }

    private void InstantiateBatch(List<InstantiateRequest> batch)
    {
        List<GameObject> pool;
        int poolIndex;
        GameObject gameObject;
        foreach (var request in batch)
        {
            if(!ObjectPools.ContainsKey(request.PlacementData))
                continue;
            var objectPoolData = ObjectPools[request.PlacementData];
            pool = objectPoolData.Item1;
            poolIndex = objectPoolData.Item2;
            if (poolIndex < pool.Count)
            {
                GameObject obj = pool[poolIndex];
                obj.transform.position = request.Position;
                obj.transform.rotation = request.Rotation;
                obj.SetActive(true);
            }
            else
            {
                gameObject = Instantiate(request.Prefab, request.Position, request.Rotation, request.Parent);
                gameObject.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
                pool.Add(gameObject);
            }

            poolIndex++;
            ObjectPools[request.PlacementData] = (pool, poolIndex);
        }

        foreach (var entry in ObjectPools)
        {
            pool = entry.Value.Item1;
            poolIndex = entry.Value.Item2;
            for (int i = poolIndex; i < pool.Count; i++)
            {
                pool[i].SetActive(false);
            }
        }
    }
}