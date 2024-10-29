using System;
using UnityEngine;


public class TerrainChunk
{
    public event Action<TerrainChunk, bool> onVisiblityChanged;
    
    public Vector2 coordinate;

    private Vector2 sampleCenter;
    private Bounds bounds;

    private GameObject meshGameObject;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;

    private LODInfo[] lodInfos;
    private LODMesh[] _lodMeshes;
    private int previousLODIndex = -1;
    private int colliderLODIndex;
    private bool hasSetCollider;
    private const float colliderGenerationDistanceThreshold = 5f;

    private HeightMap _heightMap;
    private bool heightMapReceived;

    private HeightMapSettings _heightMapSettings;
    private MeshSettings _meshSettings;

    private Transform viewer;
    private float maxViewDistance;

    public TerrainChunk(Vector2 coordinate, HeightMapSettings heightMapSettings, MeshSettings meshSettings,
        LODInfo[] lodInfos, int colliderLODIndex, Transform parent, Transform viewer,
        Material material)
    {
        this.lodInfos = lodInfos;
        this.colliderLODIndex = colliderLODIndex;
        this.coordinate = coordinate;
        _heightMapSettings = heightMapSettings;
        _meshSettings = meshSettings;
        this.viewer = viewer;

        sampleCenter = coordinate * meshSettings.meshWorldSize / meshSettings.meshScale;
        Vector2 position = this.coordinate * meshSettings.meshWorldSize;
        bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);


        meshGameObject = new GameObject("TerrainChunk");
        _meshRenderer = meshGameObject.AddComponent<MeshRenderer>();
        _meshFilter = meshGameObject.AddComponent<MeshFilter>();
        _meshCollider = meshGameObject.AddComponent<MeshCollider>();
        _meshRenderer.material = material;

        meshGameObject.transform.position = new Vector3(position.x, 0, position.y);
        meshGameObject.transform.parent = parent;
        SetVisible(false);

        _lodMeshes = new LODMesh[this.lodInfos.Length];
        for (int i = 0; i < this.lodInfos.Length; i++)
        {
            _lodMeshes[i] = new LODMesh(lodInfos[i].lod);

            _lodMeshes[i].updateCallback += UpdateTerrainChunk;
            if (i == colliderLODIndex)
                _lodMeshes[i].updateCallback += UpdateCollisonMesh;
        }

        maxViewDistance = lodInfos[lodInfos.Length - 1].visibleDSTThreshold;
        
    }

    private Vector2 ViewerPosition => new Vector2(viewer.position.x,viewer.position.z);
    

    void OnHeightMapReceived(object heightMapObject)
    {
        _heightMap = (HeightMap)heightMapObject;
        heightMapReceived = true;

        UpdateTerrainChunk();
    }

    public void Load()
    {
        ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(
        _meshSettings.numVerticesPerLine, _meshSettings.numVerticesPerLine,
        _heightMapSettings, sampleCenter), OnHeightMapReceived);
    }


    public void UpdateTerrainChunk()
    {
        if (heightMapReceived)
        {
            float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(ViewerPosition));

            bool wasVisible = IsVisible();
            bool visible = viewerDstFromNearestEdge <= maxViewDistance;
            if (visible)
            {
                int lodIndex = 0;
                for (int i = 0; i < lodInfos.Length - 1; i++)
                {
                    if (viewerDstFromNearestEdge > lodInfos[i].visibleDSTThreshold)
                        lodIndex = i + 1;
                    else
                        break;
                }

                if (lodIndex != previousLODIndex)
                {
                    LODMesh lodMesh = _lodMeshes[lodIndex];
                    if (lodMesh.hasMesh)
                    {
                        previousLODIndex = lodIndex;
                        _meshFilter.mesh = lodMesh.mesh;
                    }
                    else if (!lodMesh.hasRequestedMesh)
                    {
                        lodMesh.RequestMesh(_heightMap,_meshSettings);
                    }
                }
            }

            if (wasVisible != visible)
            {
                SetVisible(visible);
                
                if (onVisiblityChanged != null)
                    onVisiblityChanged(this, visible);
            }
        }
    }

    public void UpdateCollisonMesh()
    {
        if (!hasSetCollider)
        {
            float sqrDistanceFromViewerToEdge = bounds.SqrDistance(ViewerPosition);

            if (sqrDistanceFromViewerToEdge < lodInfos[colliderLODIndex].sqrVisibleDistanceThreshold)
                if (_lodMeshes[colliderLODIndex].hasRequestedMesh != true)
                    _lodMeshes[colliderLODIndex].RequestMesh(_heightMap,_meshSettings);

            if (sqrDistanceFromViewerToEdge <
                colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
                if (_lodMeshes[colliderLODIndex].hasMesh)
                {
                    _meshCollider.sharedMesh = _lodMeshes[colliderLODIndex].mesh;
                    hasSetCollider = true;
                }
        }
    }

    public void SetVisible(bool visible)
    {
        meshGameObject.SetActive(visible);
    }

    public bool IsVisible()
    {
        return meshGameObject.activeSelf;
    }
}

class LODMesh
{
    public Mesh mesh;
    public bool hasRequestedMesh;
    public bool hasMesh;
    private int lod;
    public event Action updateCallback;

    public LODMesh(int lod)
    {
        this.lod = lod;
    }

    public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
    {
        hasRequestedMesh = true;
        ThreadedDataRequester.RequestData(
            () => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod), OnMeshDataReceived);
    }

    void OnMeshDataReceived(object meshDataObject)
    {
        mesh = ((MeshData)meshDataObject).CreateMesh();
        hasMesh = true;
        updateCallback();
    }
}