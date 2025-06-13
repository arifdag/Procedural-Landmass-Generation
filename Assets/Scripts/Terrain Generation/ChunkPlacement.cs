using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkPlacement
{
    private bool placed;

    public void PlaceObjects(PlacementSettings.PlacementData[] placementDatas, MeshData meshData,
        HeightMap heightMap, float meshScale, Transform parent, Vector2 chunkCoord, Vector3 worldPosition)
    {
        ThreadedDataRequester.RequestThread(
            () => PlacementManager.StartPlacingObjects(placementDatas, meshData, heightMap, meshScale, parent, chunkCoord,
                worldPosition), OnObjcetsPlaced);
    }

    void OnObjcetsPlaced()
    {
        placed = true;
    }
}