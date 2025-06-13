using System;
using UnityEngine;

[CreateAssetMenu]
public class PlacementSettings : UpdatableData
{
    public PlacementData[] placementDatas;

    [Serializable]
    public struct PlacementData
    {
        public bool GPUInstancing;
        public GameObject[] prefabs;
        public Mesh mesh;
        public Material[] materials;

        [Header("Scale Options")] public bool useRandomScale;

        [Tooltip("Uniform scale if not random")]
        public int scale;

        [Tooltip("Min uniform scale if random")]
        public int minScale;

        [Tooltip("Max uniform scale if random")]
        public int maxScale;

        [Range(0, 1)] public float density;
        public float minHeight;
        public float maxHeight;
        public float minSteepness;
        public float maxSteepness;

        public float heightWeight;
        public float noiseWeight;
        public float noiseScale;
    }
}