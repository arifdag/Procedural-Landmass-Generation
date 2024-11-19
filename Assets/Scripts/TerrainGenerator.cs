using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class TerrainGenerator : MonoBehaviour
{
    private const float ChunkUpdateThreshold = 25f;
    private const float SqrChunkUpdateThreshold = ChunkUpdateThreshold * ChunkUpdateThreshold;


    [SerializeField] private int colliderLODIndex;
    public LODInfo[] detailLevels;

    [SerializeField] private Transform viewTransform;
    Vector2 _viewerPosition;
    private Vector2 _lastPosition;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureSettings;
    public PlacementSettings placementSettings;

    private float _meshWorldSize;
    private int _chunkVisibleInViewDistance;

    private readonly Dictionary<Vector2, TerrainChunk> _terrainChunks = new Dictionary<Vector2, TerrainChunk>();
    private readonly List<TerrainChunk> _visibleTerrainChunks = new List<TerrainChunk>();

    [SerializeField] private Material mapMaterial;

    private void Start()
    {
        textureSettings.ApplyToMaterial(mapMaterial);
        textureSettings.UpdateMeshHeight(mapMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);

        float maxViewDistance = detailLevels[^1].visibleDstThreshold;
        _meshWorldSize = meshSettings.MeshWorldSize;
        _chunkVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / _meshWorldSize);

        UpdateVisibleChunks();
    }

    private void Update()
    {
        var position = viewTransform.position;
        _viewerPosition = new Vector2(position.x, position.z);
        if (_viewerPosition != _lastPosition)
        {
            foreach (var chunk in _visibleTerrainChunks)
            {
                chunk.UpdateCollisionMesh();
            }
        }

        if ((_lastPosition - _viewerPosition).sqrMagnitude > SqrChunkUpdateThreshold)
        {
            _lastPosition = _viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        HashSet<Vector2> alreadyUpdatedChunkCoord = new HashSet<Vector2>();
        for (int i = _visibleTerrainChunks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCoord.Add(_visibleTerrainChunks[i].coordinate);
            _visibleTerrainChunks[i].UpdateTerrainChunk();
        }

        int currentChunkCoordX = Mathf.RoundToInt(_viewerPosition.x / _meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(_viewerPosition.y / _meshWorldSize);

        for (int yOffset = -_chunkVisibleInViewDistance; yOffset <= _chunkVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -_chunkVisibleInViewDistance; xOffset <= _chunkVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (!alreadyUpdatedChunkCoord.Contains(viewedChunkCord))
                {
                    if (_terrainChunks.TryGetValue(viewedChunkCord, out var chunk))
                    {
                        chunk.UpdateTerrainChunk();
                    }
                    else
                    {
                        TerrainChunk newChunk = new TerrainChunk(viewedChunkCord, heightMapSettings, meshSettings,
                            placementSettings,
                            detailLevels, colliderLODIndex, transform, viewTransform, mapMaterial);
                        _terrainChunks.Add(viewedChunkCord, newChunk);
                        newChunk.OnVisibilityChanged += OnTerrainChunkVisibilityChanged;
                        newChunk.Load();
                    }
                }
            }
        }
    }

    void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
    {
        if (isVisible)
            _visibleTerrainChunks.Add(chunk);
        else
            _visibleTerrainChunks.Remove(chunk);
    }
}

[Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings.NumSupportedLods - 1)]
    public int lod;

    [FormerlySerializedAs("visibleDSTThreshold")]
    public float visibleDstThreshold;

    public float SqrVisibleDistanceThreshold => visibleDstThreshold * visibleDstThreshold;
}