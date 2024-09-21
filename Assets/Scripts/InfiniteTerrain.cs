using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
    private const float chunkUpdateThreshold = 25f;
    private const float sqrChunkUpdateThreshold = chunkUpdateThreshold * chunkUpdateThreshold;
    private const float scale = 5;
    
    public LODInfo[] detailLevels;
    private static float MaxViewDistance;

    [SerializeField] private Transform viewTransform;
    public static Vector2 viewerPosition;
    private Vector2 lastPosition;
    
    

    private int chunkSize;
    private int chunkVisibleInViewDistance;

    private Dictionary<Vector2, TerrainChunk> _terrainChunks = new Dictionary<Vector2, TerrainChunk>();
    private static  List<TerrainChunk> terrainChunkVisibleLastUpdate = new List<TerrainChunk>();

    private static MapGenerator _mapGenerator;
    [SerializeField] private Material mapMaterial;

    private void Start()
    {
        _mapGenerator = FindObjectOfType<MapGenerator>();

        MaxViewDistance = detailLevels[detailLevels.Length - 1].visibleDSTThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunkVisibleInViewDistance = Mathf.RoundToInt(MaxViewDistance / chunkSize);
        
        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewTransform.position.x, viewTransform.position.z) / scale;
        if ((lastPosition - viewerPosition).sqrMagnitude > sqrChunkUpdateThreshold)
        {
            lastPosition = viewerPosition;
            UpdateVisibleChunks();
        }
        
    }

    void UpdateVisibleChunks()
    {
        for (int i = 0; i < terrainChunkVisibleLastUpdate.Count; i++)
            terrainChunkVisibleLastUpdate[i].SetVisible(false);
        terrainChunkVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunkVisibleInViewDistance; yOffset <= chunkVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -chunkVisibleInViewDistance; xOffset <= chunkVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (_terrainChunks.ContainsKey(viewedChunkCord))
                {
                    _terrainChunks[viewedChunkCord].UpdateTerrainChunk();
                }
                else
                {
                    _terrainChunks.Add(viewedChunkCord,
                        new TerrainChunk(viewedChunkCord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk
    {
        private GameObject meshGameObject;
        private Vector2 position;
        private Bounds bounds;
        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;
        private LODInfo[] lodInfos;
        private LODMesh[] _lodMeshes;
        private MapData _mapData;
        private bool mapDataRecived;
        private int previousLODIndex=-1;

        public TerrainChunk(Vector2 coordinates, int size, LODInfo[] lodInfos, Transform parent, Material material)
        {
            this.lodInfos = lodInfos;

            position = coordinates * size;
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);
            bounds = new Bounds(position, Vector2.one * size);


            meshGameObject = new GameObject("TerrainChunk");
            _meshRenderer = meshGameObject.AddComponent<MeshRenderer>();
            _meshFilter = meshGameObject.AddComponent<MeshFilter>();
            _meshRenderer.material = material;

            meshGameObject.transform.position = positionV3 * scale;
            meshGameObject.transform.parent = parent;
            meshGameObject.transform.localScale = Vector3.one * scale;
            SetVisible(false);
            
            _lodMeshes = new LODMesh[this.lodInfos.Length];
            for (int i = 0; i < this.lodInfos.Length; i++)
            {
                _lodMeshes[i] = new LODMesh(lodInfos[i].lod,UpdateTerrainChunk);
            }

            _mapGenerator.RequestMapData(position,OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            _mapData = mapData;
            mapDataRecived = true;
            Texture2D texture2D = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize,
                MapGenerator.mapChunkSize);
            _meshRenderer.material.mainTexture = texture2D;
            
            UpdateTerrainChunk();
        }


        public void UpdateTerrainChunk()
        { if (mapDataRecived)
            {
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDstFromNearestEdge <= MaxViewDistance;
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
                        else if (!lodMesh.hasRequestMesh)
                        {
                            lodMesh.RequestMesh(_mapData);
                        }
                    }
                    terrainChunkVisibleLastUpdate.Add(this);
                }

                SetVisible(visible);
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
        public bool hasRequestMesh;
        public bool hasMesh;
        private int lod;
        private Action updateCallback;

        public LODMesh(int lod,Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestMesh = true;
            _mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;
            updateCallback();
        }
    }

    [Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDSTThreshold;
    }
}