using UnityEngine;


[CreateAssetMenu]
public class MeshSettings : UpdatableData
{
    public const int NumSupportedLods = 5;
    private const int NumOfSupportedChunkSizes = 8;
    private const int NumOfSupportedFlatShadedChunkSizes = 2;
    private static readonly int[] SupportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

    [SerializeField] public float meshScale = 2.5f;
    [SerializeField] public bool useFlatShadding;

    [Range(0, NumOfSupportedChunkSizes)] [SerializeField]
    private int chunkSizeIndex;

    [Range(0, NumOfSupportedFlatShadedChunkSizes)] [SerializeField]
    private int flatShadedChunkSizeIndex;


    public int NumVerticesPerLine =>
        SupportedChunkSizes[(useFlatShadding) ? flatShadedChunkSizeIndex : chunkSizeIndex] + 1;

    public float MeshWorldSize => (NumVerticesPerLine - 1) * meshScale;
}