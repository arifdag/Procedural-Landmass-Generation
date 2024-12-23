using UnityEngine;


public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings,
        int levelOfDetail)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float halfHeight = height / 2f;
        float halfWidth = width / 2f;

        int meshSimplificationIncrement = MapPreview.LodIncrements[levelOfDetail];
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine, meshSettings.useFlatShadding);
        int vertexIndex = 0;
        for (int y = 0; y < height; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < width; x += meshSimplificationIncrement)
            {
                meshData.vertices[vertexIndex] = new Vector3((x - halfWidth) * meshSettings.meshScale,
                    heightMap[x, y] * meshSettings.meshScale, (y - halfHeight) * meshSettings.meshScale);


                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);
                if (x < width - 1 && y < height - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine, vertexIndex + verticesPerLine + 1);
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }

        meshData.ProcessMesh();
        meshData.CalculateSteepness();
        return meshData;
    }
}

public class MeshData
{
    public Vector3[] vertices;
    public readonly int[] triangles;
    public Vector2[] uvs;
    public readonly Vector3[] normals;

    private int _index;
    private readonly float[] _steepnessCache;
    public readonly int _width;
    public readonly int _height;
    private readonly bool _useFlatShading;

    public Bounds bounds; // Add bounds property

    public MeshData(int meshWidth, int meshHeight, bool useFlatShading)
    {
        this._useFlatShading = useFlatShading;
        this._width = meshWidth;
        this._height = meshHeight;
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
        normals = new Vector3[meshWidth * meshHeight];
        _steepnessCache = new float[meshWidth * meshHeight];
        bounds = new Bounds(); // Initialize bounds
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[_index] = a;
        triangles[_index + 1] = b;
        triangles[_index + 2] = c;
        _index += 3;
    }

    public void ProcessMesh()
    {
        CalculateBounds(); // Calculate bounds after vertices are populated

        if (_useFlatShading)
            FlatShading();
        else
            CalculateNormals();
    }

    void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUVs = new Vector2[triangles.Length];

        for (int i = 0; i < triangles.Length; i++)
        {
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUVs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }

        vertices = flatShadedVertices;
        uvs = flatShadedUVs;
    }

    private void CalculateBounds()
    {
        if (vertices.Length == 0) return;

        Vector3 min = vertices[0];
        Vector3 max = vertices[0];

        for (int i = 1; i < vertices.Length; i++)
        {
            min = Vector3.Min(min, vertices[i]);
            max = Vector3.Max(max, vertices[i]);
        }

        bounds.SetMinMax(min, max);
    }

    // Calculate normals manually for threading
    private void CalculateNormals()
    {
        Vector3[] triangleNormals = new Vector3[triangles.Length / 3];

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int vertexIndexA = triangles[i];
            int vertexIndexB = triangles[i + 1];
            int vertexIndexC = triangles[i + 2];

            Vector3 pointA = vertices[vertexIndexA];
            Vector3 pointB = vertices[vertexIndexB];
            Vector3 pointC = vertices[vertexIndexC];

            Vector3 sideAB = pointB - pointA;
            Vector3 sideAC = pointC - pointA;
            Vector3 normal = Vector3.Cross(sideAB, sideAC).normalized;

            triangleNormals[i / 3] = normal;

            normals[vertexIndexA] += normal;
            normals[vertexIndexB] += normal;
            normals[vertexIndexC] += normal;
        }

        for (int i = 0; i < normals.Length; i++)
        {
            normals[i].Normalize();
        }
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh
        {
            vertices = vertices,
            triangles = triangles,
            uv = uvs,
            bounds = bounds // Set bounds for the mesh
        };

        if (_useFlatShading)
            mesh.RecalculateNormals();
        else
            mesh.normals = normals; // Set the manually calculated normals.

        return mesh;
    }

    public void CalculateSteepness()
    {
        for (int z = 1; z < _height - 1; z++)
        {
            for (int x = 1; x < _width - 1; x++)
            {
                int index = z * _width + x;

                float leftHeight = vertices[index - 1].y;
                float rightHeight = vertices[index + 1].y;
                float upHeight = vertices[index - _width].y;
                float downHeight = vertices[index + _width].y;

                float dx = rightHeight - leftHeight;
                float dz = upHeight - downHeight;

                float slope = Mathf.Sqrt(dx * dx + dz * dz);
                _steepnessCache[index] = Mathf.Atan(slope) * Mathf.Rad2Deg;
            }
        }
    }

    public float GetSteepness(int x, int z)
    {
        int index = z * _width + x;
        return _steepnessCache[index];
    }
}