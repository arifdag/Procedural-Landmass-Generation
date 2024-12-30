using System;
using UnityEngine;


public class TerrainChunk
{
    public event Action<TerrainChunk, bool> OnVisibilityChanged;

    public Vector2 coordinate;

    private readonly Vector2 _sampleCenter;
    private Bounds _bounds;

    private readonly GameObject _meshGameObject;
    private readonly MeshFilter _meshFilter;
    private readonly MeshCollider _meshCollider;
    private Vector2 position;

    private readonly LODInfo[] _lodInfos;
    private readonly LODMesh[] _lodMeshes;
    private int _previousLODIndex = -1;
    private readonly int _colliderLODIndex;
    private bool _hasSetCollider;
    private const float ColliderGenerationDistanceThreshold = 5f;

    private HeightMap _heightMap;
    private bool _heightMapReceived;

    private readonly HeightMapSettings _heightMapSettings;
    private readonly MeshSettings _meshSettings;
    private readonly PlacementSettings _placementSettings;

    private readonly Transform _viewer;
    private readonly float _maxViewDistance;


    public static object instance;

    public TerrainChunk(Vector2 coordinate, HeightMapSettings heightMapSettings, MeshSettings meshSettings,
        PlacementSettings placementSettings,
        LODInfo[] lodInfos, int colliderLODIndex, Transform parent, Transform viewer,
        Material material)
    {
        this._lodInfos = lodInfos;
        this._colliderLODIndex = colliderLODIndex;
        this.coordinate = coordinate;
        _heightMapSettings = heightMapSettings;
        _meshSettings = meshSettings;
        _placementSettings = placementSettings;
        this._viewer = viewer;
        instance = this;

        _sampleCenter = coordinate * meshSettings.MeshWorldSize / meshSettings.meshScale;
        position = this.coordinate * meshSettings.MeshWorldSize;
        _bounds = new Bounds(position, Vector2.one * meshSettings.MeshWorldSize);


        _meshGameObject = new GameObject("TerrainChunk");
        var meshRenderer = _meshGameObject.AddComponent<MeshRenderer>();
        _meshFilter = _meshGameObject.AddComponent<MeshFilter>();
        _meshCollider = _meshGameObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;

        _meshGameObject.transform.position = new Vector3(position.x, 0, position.y);
        _meshGameObject.transform.parent = parent;
        SetVisible(false);

        _lodMeshes = new LODMesh[this._lodInfos.Length];
        for (int i = 0; i < this._lodInfos.Length; i++)
        {
            _lodMeshes[i] = new LODMesh(lodInfos[i].lod, this);

            _lodMeshes[i].UpdateCallback += UpdateTerrainChunk;
            if (i == colliderLODIndex)
                _lodMeshes[i].UpdateCallback += UpdateCollisionMesh;
        }

        _maxViewDistance = lodInfos[^1].visibleDstThreshold;
    }

    private Vector2 ViewerPosition => new Vector2(_viewer.position.x, _viewer.position.z);


    void OnHeightMapReceived(object heightMapObject)
    {
        _heightMap = (HeightMap)heightMapObject;
        _heightMapReceived = true;

        UpdateTerrainChunk();
    }

    public void Load()
    {
        ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(
            _meshSettings.NumVerticesPerLine, _meshSettings.NumVerticesPerLine,
            _heightMapSettings, _sampleCenter), OnHeightMapReceived);
    }


    public void UpdateTerrainChunk()
    {
        if (_heightMapReceived)
        {
            float viewerDstFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(ViewerPosition));

            bool wasVisible = IsVisible();
            bool visible = viewerDstFromNearestEdge <= _maxViewDistance;
            if (visible)
            {
                int lodIndex = 0;
                for (int i = 0; i < _lodInfos.Length - 1; i++)
                {
                    if (viewerDstFromNearestEdge > _lodInfos[i].visibleDstThreshold)
                        lodIndex = i + 1;
                    else
                        break;
                }

                if (lodIndex != _previousLODIndex)
                {
                    
                    LODMesh lodMesh = _lodMeshes[lodIndex];
                    if (lodMesh.hasMesh)
                    {
                        RequestPlacement(_lodMeshes[lodIndex].meshData);
                        _previousLODIndex = lodIndex;
                        _meshFilter.mesh = lodMesh.mesh;
                    }
                    else if (!lodMesh.hasRequestedMesh)
                    {
                        lodMesh.RequestMesh(_heightMap, _meshSettings);
                    }
                }
            }

            if (wasVisible != visible)
            {
                SetVisible(visible);

                if (OnVisibilityChanged != null)
                    OnVisibilityChanged(this, visible);
            }
        }
    }

    public void UpdateCollisionMesh()
    {
        if (!_hasSetCollider)
        {
            float sqrDistanceFromViewerToEdge = _bounds.SqrDistance(ViewerPosition);

            if (sqrDistanceFromViewerToEdge < _lodInfos[_colliderLODIndex].SqrVisibleDistanceThreshold)
                if (_lodMeshes[_colliderLODIndex].hasRequestedMesh != true)
                    _lodMeshes[_colliderLODIndex].RequestMesh(_heightMap, _meshSettings);

            if (sqrDistanceFromViewerToEdge <
                ColliderGenerationDistanceThreshold * ColliderGenerationDistanceThreshold)
                if (_lodMeshes[_colliderLODIndex].hasMesh)
                {
                    _meshCollider.sharedMesh = _lodMeshes[_colliderLODIndex].mesh;
                    _hasSetCollider = true;
                }
        }
    }

    private void SetVisible(bool visible)
    {
        _meshGameObject.SetActive(visible);
    }

    private bool IsVisible()
    {
        return _meshGameObject.activeSelf;
    }

    public void RequestPlacement(MeshData meshData)
    {
        ChunkPlacement placement = new ChunkPlacement();
        placement.PlaceObjects(
            _placementSettings.placementDatas,
            meshData,
            _heightMap,
            _meshSettings.meshScale,
            _meshGameObject.transform, coordinate,
            new Vector3(position.x, 0, position.y)
        );
    }
}


class LODMesh
{
    public Mesh mesh;
    public MeshData meshData;
    public bool hasRequestedMesh;
    public bool hasMesh;
    private readonly int _lod;
    public event Action UpdateCallback;

    private TerrainChunk _parentChunk;

    public LODMesh(int lod, TerrainChunk parentChunk)
    {
        this._lod = lod;
        this._parentChunk = parentChunk;
    }

    public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
    {
        hasRequestedMesh = true;
        ThreadedDataRequester.RequestData(
            () => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, _lod), OnMeshDataReceived);
    }

    void OnMeshDataReceived(object meshDataObject)
    {
        meshData = (MeshData)meshDataObject;
        mesh = meshData.CreateMesh();
        hasMesh = true;
        _parentChunk.RequestPlacement(meshData);
        UpdateCallback?.Invoke();
    }
}