using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class PlacementSettings : UpdatableData
{
    public PlacementData[] placementDatas;

    [Serializable]
    public struct PlacementData
    {
        public GameObject[] prefabs;
        [Range(0,1)] public float density;
        public float minHeight;
        public float maxHeight;
        public float minSteepness;
        public float maxSteepness;

        public float heightWeight;
        public float noiseWeight;
        public float noiseScale;
    }
}