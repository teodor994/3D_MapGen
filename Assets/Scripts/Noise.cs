using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public static class Noise
{

    public enum NormalizeMode { Local, Global };
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random rand = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[(int)octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;


        for (int i = 0; i < octaves; i++)
        {
            float offsetX = rand.Next(-10000, 10000) + offset.x;
            float offsetY = rand.Next(-10000, 10000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2;
        float halfHeight = mapHeight / 2; // when changing noisescale, zooming to the center

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (float)(x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (float)(y - halfHeight + octaveOffsets[i].y) / scale * frequency;
                    float noiseValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; //to have negative noise
                    //noiseMap[x, y] = noiseValue; -> default generation -> not useful

                    noiseHeight += noiseValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }
                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }
                noiseMap[x, y] = noiseHeight;
            }
        }
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {   
                if (normalizeMode == NormalizeMode.Local) {
                    // Normalizing the noise map -> to only have values in [0, 1]
                    //if a point is == to minNoise => equals to 0 => equals to 1 if is equal to maxNoise
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }

        return noiseMap;
    }
}
