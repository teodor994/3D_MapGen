using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class MapGenerator : MonoBehaviour
{
    public int mapWidth;
    public int mapHeight;

    public float noiseScale;

    public int octaves;
    [Range(0, 1)] //-> makes the persistance a slider in editor -> easy access
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public bool autoUpdate;

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistance, lacunarity, offset);

        MapDisplay display = FindFirstObjectByType<MapDisplay>();
        display.DrawNoiseMap(noiseMap);
    }

    void OnValidate() //checks if variables are changed accordingly
    {
        if(mapWidth < 1)
            mapWidth = 1;
        if(mapHeight < 1)
            mapHeight = 1;
        if(lacunarity < 1)
            lacunarity = 1;
        if(octaves < 0) 
            octaves = 0;
    }
}
