using UnityEngine;

// Gerador de ruído: recebe coordenada GLOBAL, devolve 0..1. Não sabe o que é chunk.
public static class Noise
{
    public static float Fbm(float x, float y, float scale, int octaves, float persistence, float lacunarity, Vector2 offset)
    {
        float amp = 1, freq = 1, sum = 0, maxAmp = 0;
        for (int i = 0; i < octaves; i++)
        {
            float nx = (x + offset.x) * scale * freq;
            float ny = (y + offset.y) * scale * freq;
            sum += (Mathf.PerlinNoise(nx, ny) * 2 - 1) * amp; // 0..1 -> -1..1
            maxAmp += amp;
            amp *= persistence;
            freq *= lacunarity;
        }
        return Mathf.Clamp01(sum / maxAmp * 0.5f + 0.5f);   // volta para 0..1
    }
}