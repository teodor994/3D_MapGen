using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class MapGenerator : MonoBehaviour
{
    public int mapWidth;
    public int mapHeight;

    public float noiseScale;

    public int octaves;
    public float persistance;
    public float lacunarity;

    public bool autoUpdate;

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, noiseScale, octaves, persistance, lacunarity);

        MapDisplay display = FindFirstObjectByType<MapDisplay>();
        display.DrawNoiseMap(noiseMap);
    }
}
