using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    private const float chunkUpdateThreshold = 25f;
    private const float sqrChunkUpdateThreshold = chunkUpdateThreshold * chunkUpdateThreshold;


    [SerializeField] private int colliderLODIndex;
    public LODInfo[] detailLevels;

    [SerializeField] private Transform viewTransform;
    Vector2 viewerPosition;
    private Vector2 lastPosition;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureSettings;

    private float meshWorldSize;
    private int chunkVisibleInViewDistance;

    private Dictionary<Vector2, TerrainChunk> _terrainChunks = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    [SerializeField] private Material mapMaterial;

    private void Start()
    {
        textureSettings.ApplyToMaterial(mapMaterial);
        textureSettings.UpdateMeshHeight(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        
        float MaxViewDistance = detailLevels[detailLevels.Length - 1].visibleDSTThreshold;
        meshWorldSize = meshSettings.meshWorldSize;
        chunkVisibleInViewDistance = Mathf.RoundToInt(MaxViewDistance / meshWorldSize);

        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewTransform.position.x, viewTransform.position.z);
        if (viewerPosition != lastPosition)
        {
            foreach (var chunk in visibleTerrainChunks)
            {
                chunk.UpdateCollisonMesh();
            }
        }

        if ((lastPosition - viewerPosition).sqrMagnitude > sqrChunkUpdateThreshold)
        {
            lastPosition = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        HashSet<Vector2> alreadyUpdatedChunkCoord = new HashSet<Vector2>();
        for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCoord.Add(visibleTerrainChunks[i].coordinate);
            visibleTerrainChunks[i].UpdateTerrainChunk();
        }

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

        for (int yOffset = -chunkVisibleInViewDistance; yOffset <= chunkVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -chunkVisibleInViewDistance; xOffset <= chunkVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (!alreadyUpdatedChunkCoord.Contains(viewedChunkCord))
                {
                    if (_terrainChunks.ContainsKey(viewedChunkCord))
                    {
                        _terrainChunks[viewedChunkCord].UpdateTerrainChunk();
                    }
                    else
                    {
                        TerrainChunk newChunk = new TerrainChunk(viewedChunkCord, heightMapSettings, meshSettings,
                            detailLevels, colliderLODIndex, transform, viewTransform, mapMaterial);
                        _terrainChunks.Add(viewedChunkCord, newChunk);
                        newChunk.onVisiblityChanged += OnTerrainChunkVisiblityChanged;
                        newChunk.Load();
                    }
                }
            }
        }
    }

    void OnTerrainChunkVisiblityChanged(TerrainChunk chunk, bool isVisible)
    {
        if (isVisible)
            visibleTerrainChunks.Add(chunk);
        else
            visibleTerrainChunks.Remove(chunk);
    }
}

[Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings.numSupportedLods - 1)]
    public int lod;

    public float visibleDSTThreshold;

    public float sqrVisibleDistanceThreshold
    {
        get { return visibleDSTThreshold * visibleDSTThreshold; }
    }
}