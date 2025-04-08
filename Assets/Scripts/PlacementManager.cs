using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlacementManager : MonoBehaviour
{
    private static readonly ConcurrentQueue<InstantiateRequest> InstantiateQueue = new();
    private static readonly ConcurrentQueue<GameObject> ReturnToPoolQueue = new();
    private static ConcurrentQueue<InstantiateRequest> GPUBatch = new();
    private static GPUInstancing gpuInstancing;

    // Object pools for each prefab type
    private static readonly ConcurrentDictionary<GameObject, ConcurrentBag<GameObject>> ObjectPools = new();

    // Track active objects per chunk
    private static readonly ConcurrentDictionary<Vector2, HashSet<GameObject>> ActiveObjectsByChunk = new();
    private static readonly object poolLock = new object();

    private void Start()
    {
        gpuInstancing = FindObjectOfType<GPUInstancing>();
    }

    public static void StartPlacingObjects(PlacementSettings.PlacementData[] placementDatas, MeshData meshData,
        HeightMap heightMap, float meshScale, Transform parent, Vector2 chunkCoord, Vector3 worldPosition)
    {
        // Queue previous objects in this chunk for deactivation if they exist
        if (ActiveObjectsByChunk.TryGetValue(chunkCoord, out var activeObjects))
        {
            foreach (var obj in activeObjects)
            {
                ReturnToPoolQueue.Enqueue(obj);
            }

            activeObjects.Clear();
        }

        foreach (var placementData in placementDatas)
        {
            PlaceObjects(meshData, heightMap, meshScale, placementData, parent, chunkCoord, worldPosition);
        }
    }

    private static void ReturnToPool(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);
        var prefab = obj.GetComponent<PooledObject>()?.Prefab;
        if (prefab != null && ObjectPools.TryGetValue(prefab, out var pool))
        {
            pool.Add(obj);
        }
    }

    private static GameObject GetFromPool(GameObject prefab, Transform parent)
    {
        if (!ObjectPools.TryGetValue(prefab, out var pool))
        {
            lock (poolLock)
            {
                pool = ObjectPools.GetOrAdd(prefab, new ConcurrentBag<GameObject>());
            }
        }

        if (pool.TryTake(out var obj))
        {
            obj.SetActive(true);
            return obj;
        }

        obj = Instantiate(prefab, parent);
        var pooledObj = obj.AddComponent<PooledObject>();
        pooledObj.Prefab = prefab;
        return obj;
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
                    // Calculate base position with smaller random offset
                    float randomXOffset = (float)(threadSafeRandom.NextDouble() - 0.5) * 0.5f;
                    float randomZOffset = (float)(threadSafeRandom.NextDouble() - 0.5) * 0.5f;

                    // Get the vertex index for this position
                    int vertexIndex = z * chunkSizeX + x;
                    Vector3 vertexPosition = meshData.vertices[vertexIndex];
                    Vector3 normal = meshData.normals[vertexIndex];

                    // Calculate world position
                    Vector3 worldPos = vertexPosition + worldPosition;

                    // Add small random offset in the direction of the normal
                    float randomOffset = (float)(threadSafeRandom.NextDouble() - 0.5) * 0.2f;
                    worldPos += normal * randomOffset;

                    if (placementData.GPUInstancing)
                    {
                        // For GPU instancing, we'll use the normal to adjust the position
                        float offset = 0.5f;
                        worldPos += normal * offset;
                        Quaternion rt = Quaternion.FromToRotation(Vector3.up, normal);
                        GPUBatch.Enqueue(new InstantiateRequest(worldPos, rt, placementData.scale));
                        continue;
                    }

                    // For regular objects, we'll use the calculated position and normal
                    GameObject prefab = placementData.prefabs[threadSafeRandom.Next(placementData.prefabs.Length)];
                    Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
                    InstantiateQueue.Enqueue(new InstantiateRequest(prefab, worldPos, rotation, parent, chunkCoord));
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
        // Handle object returns on main thread
        while (ReturnToPoolQueue.TryDequeue(out var obj))
        {
            ReturnToPool(obj);
        }

        // Handle instantiation requests
        while (InstantiateQueue.TryDequeue(out var request))
        {
            _batch.Add(request);
            if (_batch.Count >= 100)
            {
                InstantiateBatch(_batch);
                _batch.Clear();
            }
        }

        if (_batch.Count > 0)
            InstantiateBatch(_batch);
    }

    public class InstantiateRequest
    {
        public GameObject Prefab { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public int Scale { get; }
        public Transform Parent { get; }
        public Vector2 ChunkCoord { get; }

        public InstantiateRequest(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent,
            Vector2 chunkCoord)
        {
            Prefab = prefab;
            Position = position;
            Rotation = rotation;
            Parent = parent;
            ChunkCoord = chunkCoord;
        }

        public InstantiateRequest(Vector3 position, Quaternion rotation, int scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }
    }

    private class PooledObject : MonoBehaviour
    {
        public GameObject Prefab { get; set; }
    }

    private void InstantiateBatch(List<InstantiateRequest> batch)
    {
        GameObject gameObject;
        foreach (var request in batch)
        {
            gameObject = GetFromPool(request.Prefab, request.Parent);
            gameObject.transform.position = request.Position;
            gameObject.transform.rotation = request.Rotation;
            gameObject.transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);

            if (!ActiveObjectsByChunk.TryGetValue(request.ChunkCoord, out var activeObjects))
            {
                lock (poolLock)
                {
                    activeObjects = ActiveObjectsByChunk.GetOrAdd(request.ChunkCoord, new HashSet<GameObject>());
                }
            }

            lock (activeObjects)
            {
                activeObjects.Add(gameObject);
            }
        }
    }
}