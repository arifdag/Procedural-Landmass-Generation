using UnityEngine;
using Random = UnityEngine.Random;

public class PlacementManager : MonoBehaviour
{
    private static Transform placedObjectsParent;

    public static void StartPlacingObjects(PlacementSettings.PlacementData[] placementDatas, MeshData meshData, HeightMap heightMap)
    {
        // Destroy the previous parent if it exists to clear previously placed objects
        if (placedObjectsParent != null)
        {
            DestroyImmediate(placedObjectsParent.gameObject);
        }
        
        placedObjectsParent = new GameObject("Placed Objects").transform;

        for (int i = 0; i < placementDatas.Length; i++)
        {
            PlaceObjects(meshData, heightMap, placementDatas[i], placedObjectsParent);
        }
    }

    private static void PlaceObjects(MeshData meshData, HeightMap heightMap, PlacementSettings.PlacementData placementData, Transform parent)
    {
        int chunkSizeX = heightMap.values.GetLength(0);
        int chunkSizeZ = heightMap.values.GetLength(1);

        int halfSizeX = chunkSizeX / 2;
        int halfSizeZ = chunkSizeZ / 2;

        for (int x = 0; x < chunkSizeX; x++)
        {
            for (int z = 0; z < chunkSizeZ; z++)
            {
                float normalizedHeight = Mathf.InverseLerp(heightMap.minValue, heightMap.maxValue, heightMap.values[x, z]);

                if (Fitness(meshData, normalizedHeight, placementData, x, z) > 1 - placementData.density)
                {
                    Vector3 pos = new Vector3(x - halfSizeX + Random.Range(-0.5f, 0.5f), 0, z - halfSizeZ + Random.Range(-0.5f, 0.5f));
                    pos.y = heightMap.values[x, z]-1;

                    GameObject prefab = placementData.prefabs[Random.Range(0, placementData.prefabs.Length)];
                    Instantiate(prefab, pos, Quaternion.identity, parent);
                }
            }
        }
    }

    private static float Fitness(MeshData meshData, float normalizedHeight, PlacementSettings.PlacementData placementData, int x, int z)
    {
        // Base fitness on the height factor, but introduce Perlin noise for randomness
        float fitness = normalizedHeight * placementData.heightWeight;
        if(fitness>1)
            Debug.Log("1=> " + fitness);

       
        float steepness = meshData.GetSteepness(x, z);
        if (steepness < placementData.minSteepness || steepness > placementData.maxSteepness)
            fitness -= 0.5f; 
        
        
        if (normalizedHeight < placementData.minHeight || normalizedHeight > placementData.maxHeight)
            fitness -= 0.5f;
        
        
        float noise = Mathf.PerlinNoise(x * placementData.noiseScale, z * placementData.noiseScale);
        fitness += noise * placementData.noiseWeight;
        if(fitness>1)
            Debug.Log("3=> " + fitness);

        // Ensure fitness is clamped between 0 and 1
        fitness = Mathf.Clamp01(fitness);

        return fitness;
    }
}

