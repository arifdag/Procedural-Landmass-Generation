using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
    [SerializeField] private const float MaxViewDistance = 300;
    [SerializeField] private Transform viewTransform;
    public static Vector2 viewerPosition;

    private int chunkSize;
    private int chunkVisibleInViewDistance;

    private Dictionary<Vector2, TerrainChunk> _terrainChunks = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> terrainChunkVisibleLastUpdate = new List<TerrainChunk>();
    private void Start()
    {
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunkVisibleInViewDistance = Mathf.RoundToInt(MaxViewDistance / chunkSize);
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewTransform.position.x, viewTransform.position.z);
        UpdateVisibleChunks();
    }

    void UpdateVisibleChunks()
    {
        for(int i=0;i<terrainChunkVisibleLastUpdate.Count;i++)
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
                    _terrainChunks[viewedChunkCord].UpdateTerrainMesh();
                    
                }
                else
                {
                    _terrainChunks.Add(viewedChunkCord,new TerrainChunk(viewedChunkCord,chunkSize,transform));
                }
                if(_terrainChunks[viewedChunkCord].IsVisible())
                    terrainChunkVisibleLastUpdate.Add(_terrainChunks[viewedChunkCord]);
            }
        }
    }

    public class TerrainChunk
    {
        private GameObject meshGameObject;
        private Vector2 position;
        private Bounds bounds;
        public TerrainChunk(Vector2 coordinates, int size, Transform parent)
        {
            position = coordinates * size;
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);
            bounds = new Bounds(position, Vector2.one * size);
            
            
            meshGameObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshGameObject.transform.position = positionV3;
            meshGameObject.transform.localScale = Vector3.one * size / 10f;
            meshGameObject.transform.parent = parent;
            SetVisible(false);
        }

        public void UpdateTerrainMesh()
        {
            float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDstFromNearestEdge <= MaxViewDistance;
            SetVisible(visible);
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
}