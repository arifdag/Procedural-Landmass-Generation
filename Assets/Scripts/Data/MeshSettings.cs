using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu]
public class MeshSettings : UpdatableData
{
    public const int numSupportedLods = 5;
    public const int numOfSupportedChunkSizes = 8;
    public const int numOfSupportedFlatShadedChunkSizes = 2;
    public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };
    
    [SerializeField] public float meshScale = 2.5f;
    [SerializeField] public bool useFlatShadding;
    
    [Range(0, numOfSupportedChunkSizes)] [SerializeField]
    private int chunkSizeIndex;

    [Range(0, numOfSupportedFlatShadedChunkSizes)] [SerializeField]
    private int flatShadedChunkSizeIndex;
    
    
    public int numVerticesPerLine
    {
        get
        {
            return supportedChunkSizes[(useFlatShadding) ? flatShadedChunkSizeIndex : chunkSizeIndex] + 1;
        }
    }

    public float meshWorldSize
    {
        get
        {
            return (numVerticesPerLine - 1) * meshScale;
        }
    }
}
