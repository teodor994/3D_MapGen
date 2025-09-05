using UnityEngine;
using System.Collections;
using Unity.Burst;
using UnityEditor;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve1, int levelOfDetail, bool useFlatShading)
    //heightMultiplier raises up the Mesh in 3D, as the name suggests
    {
        AnimationCurve heightCurve = new AnimationCurve(heightCurve1.keys);
        //new HeightCurve because every thread needs a separate heightCurve, they might use the same one
        //given as a parameter here resulting in odd results

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSimplificationIncrement; //removing the border
        int meshSizeUnsimplified = borderedSize - 2;

        float topLeftX = (meshSize - 1) / -2f;
        float topLeftZ = (meshSize - 1) / 2f;

        
        int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

        MeshData meshdata = new MeshData(verticesPerLine, useFlatShading);

        int borderVertexIndex = -1; 
        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;

        //int vertexIndex = 0;

        for (int y = 0; y < borderedSize; y+= meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x+= meshSimplificationIncrement)
            {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;
                if (isBorderVertex)
                {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        // Avoiding creating vertices from one to one, to avoid crashes on big maps, we multiply
        // by the meshSimplificationIncrement
        for (int y = 0; y < borderedSize; y+= meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x+= meshSimplificationIncrement)
            {
                int vertexIndex = vertexIndicesMap[x, y];
                float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                // heightMap[x, y] => default value
                // meshdata.vertices[vertexIndex] = new Vector3(topLeftX + x, heightMap[x, y] * heightMultiplier, topLeftZ - y);
                Vector2 percent = new Vector2((x - meshSimplificationIncrement)/(float)meshSize, (y - meshSimplificationIncrement)/(float)meshSize);
                // heightCurve.Evaluate(heightMap[x, y]) => value impacted by the position on the curve
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);

                meshdata.AddVertex(vertexPosition, percent, vertexIndex);

                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];
                    meshdata.AddTriangle(a, d, c);
                    meshdata.AddTriangle(d, a, b);
                }
                vertexIndex++;
            }
        }

        meshdata.Finalize();

        return meshdata; //implement threading to not freeze up while loading chunks of the mesh
                            //the reason for not returning directly the mesh
    }
}

public class MeshData
{
    Vector3[] vertices;
    int[] triangles;

    Vector2[] uvs;

    Vector3[] borderVertices;
    int[] borderTriangles;


    int triangleIndex;
    int borderTriangleIndex;

    bool useFlatShading;

    private Vector3[] bakednormals;

    public MeshData(int verticesPerLine, bool useFlatShading)
    {
        this.useFlatShading = useFlatShading;

        vertices = new Vector3[verticesPerLine * verticesPerLine];
        uvs = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[24 * verticesPerLine];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0) //border vertex
        {
            borderVertices[-vertexIndex - 1] = vertexPosition;
        }
        else
        {
            vertices[vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0) //border triangle
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex = borderTriangleIndex + 3;
        }
        else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex = triangleIndex + 3;
        }
    }

    Vector3[] CalculateNormals() //doing it manually because of a bug at which the line between two chunks is not smooth
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        
        int triangleCount = triangles.Length / 3;
        // looping through all the middle triangles
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];
            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;
        // looping through all the border triangles
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];
            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if(vertexIndexA >= 0)
                vertexNormals[vertexIndexA] += triangleNormal;
            if(vertexIndexB >= 0)
                vertexNormals[vertexIndexB] += triangleNormal;
            if(vertexIndexC >= 0)
                vertexNormals[vertexIndexC] += triangleNormal;
        }
        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }
        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA < 0) ? borderVertices[- indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[- indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[- indexC - 1] : vertices[indexC];
        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }


    public void Finalize()
    {
        if (useFlatShading)
        {
            FlatShading();
        }
        else
        {
            BakeNormals();
        }
    }

    void BakeNormals()
    {
        bakednormals = CalculateNormals();
    }
    void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[triangles.Length];
        for (int i = 0; i < triangles.Length; i++)
        {
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUvs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }
        vertices = flatShadedVertices;
        uvs = flatShadedUvs;

    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        if(useFlatShading)
        {
            mesh.RecalculateNormals();
        }
        else
        {
            mesh.normals = bakednormals;
        }
            
        return mesh;
    }
}