using UnityEngine;
using System.Collections;
using Unity.Burst;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve)
        //heightMultiplier raises up the Mesh in 3D, as the name suggests
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        MeshData meshdata = new MeshData(width, height);

        int vertexIndex = 0;


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // heightMap[x, y] => default value
                // meshdata.vertices[vertexIndex] = new Vector3(topLeftX + x, heightMap[x, y] * heightMultiplier, topLeftZ - y);
                
                // heightCurve.Evaluate(heightMap[x, y]) => value impacted by the position on the curve
                meshdata.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y);
                meshdata.uvs[vertexIndex] = new Vector2(x/(float)width, y/(float)height);
                if(x < width - 1 && y < height - 1)
                {
                    meshdata.AddTriangle(vertexIndex, vertexIndex + width + 1, vertexIndex + width);
                    meshdata.AddTriangle(vertexIndex + width + 1, vertexIndex, vertexIndex + 1);
                }
                vertexIndex++;
            }
        }

        return meshdata; //implement threading to not freeze up while loading chunks of the mesh
                            //the reason for not returning directly the mesh
    }
}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;

    public Vector2[] uvs;

    int triangleIndex;

    public MeshData(int MeshWidth, int MeshHeight)
    {
        vertices = new Vector3[MeshWidth * MeshHeight];
        uvs = new Vector2[MeshWidth * MeshHeight];
        triangles = new int[(MeshWidth - 1) * (MeshHeight - 1) * 6];
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex+1] = b;
        triangles[triangleIndex+2] = c;
        triangleIndex = triangleIndex + 3;
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