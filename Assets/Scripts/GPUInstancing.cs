using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class GPUInstancing : MonoBehaviour
{
    private Vector3 position, rotation, scale;
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material[] _materials;

    [SerializeField] int instances;


    private ConcurrentDictionary<Vector2, List<List<Matrix4x4>>> batchTable = new();
    private ConcurrentDictionary<Vector2, int> batchCount = new();
    private ConcurrentDictionary<Vector2, (List<List<Matrix4x4>>, int)> notActiveBatch = new();

    public void AddBatch(Vector2 chunkCoord, ConcurrentQueue<PlacementManager.InstantiateRequest> batch)
    {
        Debug.Log($"Adding batch for chunkCoord: {chunkCoord}");
        lock (batchTable) 
        {
            if (!batchCount.ContainsKey(chunkCoord))
            {
                batchCount[chunkCoord] = 0;
                batchTable[chunkCoord] = new List<List<Matrix4x4>>();
            }
        }

        int addedMatricies = batchCount[chunkCoord];
        List<List<Matrix4x4>> batches = batchTable[chunkCoord];

        PlacementManager.InstantiateRequest request;
        while (batch.TryDequeue(out request))
        {
            if (addedMatricies < 1000)
            {
                if (batches.Count == 0)
                    batches.Add(new List<Matrix4x4>());

                batches[^1].Add(GetMatrix4X4(request.Position, request.Rotation, Vector3.one * request.Scale));
                addedMatricies++;
            }
            else
            {
                batches.Add(new List<Matrix4x4>());
                addedMatricies = 0;
            }
        }

        batchCount[chunkCoord] = addedMatricies; // Update the count
    }

    public void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
    {
        Vector2 coord = chunk.coordinate;
        if (isVisible && notActiveBatch.ContainsKey(coord))
        {
            batchTable[coord] = notActiveBatch[coord].Item1;
            batchCount[coord] = notActiveBatch[coord].Item2;

            notActiveBatch.TryRemove(coord, out _);
        }
        else if (batchTable.ContainsKey(coord))
        {
            notActiveBatch[coord] = (batchTable[coord], batchCount[coord]);

            batchTable.TryRemove(coord, out _);
            batchCount.TryRemove(coord, out _);
        }
    }
/*
    void Start()
    {
        int addedMatricies = 0;

        for (int i = 0; i < instances; i++)
        {
            if (addedMatricies < 1000 && batches.Count != 0)
            {
                batches[^1]
                    .Add(GetMatrix4X4(new Vector3(i, 20, 20),
                        Random.rotation,
                        Vector3.one * 50)
                    );
                addedMatricies++;
            }
            else
            {
                batches.Add(new List<Matrix4x4>());
                addedMatricies = 0;
            }
        }
    }*/

    // Update is called once per frame
    void Update()
    {
        RenderBatches();
    }

    void RenderBatches()
    {
        foreach (var entry in batchTable)
        {
            List<List<Matrix4x4>> batches = entry.Value;

            foreach (List<Matrix4x4> batch in batches)
            {
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    if (i < _materials.Length)
                    {
                        Graphics.DrawMeshInstanced(mesh, i, _materials[i], batch);
                    }
                }
            }
        }
    }

    Matrix4x4 GetMatrix4X4(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        return Matrix4x4.TRS(position, rotation, scale);
    }
}