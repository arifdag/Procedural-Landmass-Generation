using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    [SerializeField] private Renderer planeRenderer;
    [SerializeField] private MeshFilter _meshFilter;
    [SerializeField] private MeshRenderer _meshRenderer;

    public void DrawTexture(Texture2D texture2D)
    {
        planeRenderer.sharedMaterial.mainTexture = texture2D;
        planeRenderer.transform.localScale = new Vector3(texture2D.width, 1, texture2D.height);
    }

    public void DrawMesh(MeshData meshData, Texture2D texture2D)
    {
        _meshFilter.sharedMesh = meshData.CreateMesh();
        _meshRenderer.sharedMaterial.mainTexture = texture2D;
    }
}