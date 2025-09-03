using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode {NoiseMap, ColourMap, Mesh}; // which map do we want to see
    public DrawMode drawMode;

    const int mapChunkSize = 241;
    [Range (0, 6)]
    public int levelOfDetail;

    public float noiseScale;

    public int octaves;
    [Range(0, 1)] //-> makes the persistance a slider in editor -> easy access
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve; // the water is also affected by the heightmultiplier
    //we take this curve to set the impact of the heightmult. to every height level
    // => on water, which is to aproximately 0.3 height the impact should be 0

    public bool autoUpdate;

    public TerrainType[] regions;

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];
                for(int i = 0; i < regions.Length; i++)
                {
                    if(currentHeight <= regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].colour;
                        break;
                    }
                }
            }
        }

        MapDisplay display = FindFirstObjectByType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        }
        else if (drawMode == DrawMode.ColourMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColourMap(colorMap, mapChunkSize, mapChunkSize));
        }
        else if(drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColourMap(colorMap, mapChunkSize, mapChunkSize));
        }
    }

    void OnValidate() //checks if variables are changed accordingly
    {
        if(lacunarity < 1)
            lacunarity = 1;
        if(octaves < 0) 
            octaves = 0;
    }
}

[System.Serializable] //it will show in the inspector
public struct TerrainType
{
    public float height;
    public Color colour;
    public string name;
}