using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;
        float halfHeight = height / 2f;
        float halfWidth = width / 2f;


        MeshData meshData = new MeshData(width, height);
        int vertexIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                meshData.vertices[vertexIndex] = new Vector3(x - halfWidth, heightMap[x,y] * heightMultiplier, y - halfHeight);
                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);
                if (x < width - 1 && y < height - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + width, vertexIndex + width + 1);
                    meshData.AddTriangle(vertexIndex, vertexIndex + width + 1, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }
        return meshData;
    }
}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    public int index;

    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshHeight * meshWidth]; // vertices = height * width;
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshHeight - 1) * (meshWidth - 1) * 6]; // Each square consist of 6 vertices
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[index] = a;
        triangles[index+1] = b;
        triangles[index+2] = c;
        index += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}