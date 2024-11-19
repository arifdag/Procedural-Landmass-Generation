using System.Collections.Concurrent;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlacementManager : MonoBehaviour
{
    private static readonly ConcurrentQueue<InstantiateRequest> InstantiateQueue = new();

    public static void StartPlacingObjects(PlacementSettings.PlacementData[] placementDatas, MeshData meshData,
        HeightMap heightMap, float meshScale, Transform parent, Vector3 worldPosition)
    {
        foreach (var placementData in placementDatas)
        {
            PlaceObjects(meshData, heightMap, meshScale, placementData, parent, worldPosition);
        }
    }

    private static void PlaceObjects(MeshData meshData, HeightMap heightMap, float meshScale,
        PlacementSettings.PlacementData placementData, Transform parent, Vector3 worldPosition)
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
                float normalizedHeight =
                    Mathf.InverseLerp(heightMap.minValue, heightMap.maxValue, heightMap.values[x, z]*meshScale);

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

                    GameObject prefab = placementData.prefabs[threadSafeRandom.Next(placementData.prefabs.Length)];
                    
                    InstantiateQueue.Enqueue(new InstantiateRequest(prefab, worldPos, rotation, parent));
                }
            }
        }
    }

    private static float Fitness(MeshData meshData, float normalizedHeight,
        PlacementSettings.PlacementData placementData, int x, int z)
    {   
        float fitness = normalizedHeight * placementData.heightWeight;

        float steepness = meshData.GetSteepness(x, z);
        if (steepness < placementData.minSteepness || steepness > placementData.maxSteepness)
            fitness -= 0.5f;

        if (normalizedHeight < placementData.minHeight || normalizedHeight > placementData.maxHeight)
            return 0f;

        int chunkSizeX = meshData._width;
        Vector3 normal = meshData.normals[z * chunkSizeX + x];
        float angle = Quaternion.Angle(Quaternion.FromToRotation(Vector3.up, normal), Quaternion.identity);

        if (Mathf.Abs(angle) > 30)
            fitness -= 0.5f;

        float noise = Mathf.PerlinNoise(x * placementData.noiseScale, z * placementData.noiseScale);
        fitness += noise * placementData.noiseWeight;

        return Mathf.Clamp01(fitness);
    }

    private void Update()
    {
        while (InstantiateQueue.TryDequeue(out var request))
        {
            Instantiate(request.Prefab, request.Position, request.Rotation, request.Parent);
        }
    }

    private class InstantiateRequest
    {
        public GameObject Prefab { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Transform Parent { get; }

        public InstantiateRequest(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            Prefab = prefab;
            Position = position;
            Rotation = rotation;
            Parent = parent;
        }
    }
}