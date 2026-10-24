using UnityEngine;
using System.Collections.Generic;

public struct ItemSpawn
{
    public string key;
    public string id;
    public Vector2Int cell;   // célula GLOBAL
}

// Gerador de chunk: recebe (cx, cy) e devolve a malha. Não sabe onde o jogador está.
public class ChunkGenerator : MonoBehaviour
{
    public int size = 32;
    public int seed = 7;
    public float scale = 0.05f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public Vector2 offset = new Vector2(1000.5f, 2000.5f);
    public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);

    // A semente vem do WorldState. Mathf.PerlinNoise não tem semente, então ela vira offset
    // (System.Random com a mesma semente sorteia sempre o mesmo offset).
    public void SetSeed(int newSeed)
    {
        seed = newSeed;
        var rng = new System.Random(newSeed);
        offset = new Vector2(rng.Next(-10000, 10000) + 0.5f, rng.Next(-10000, 10000) + 0.5f);
    }

    public float HeightAt(int gx, int gy) =>
        curve.Evaluate(Noise.Fbm(gx, gy, scale, octaves, persistence, lacunarity, offset));

    public bool IsWalkable(int gx, int gy) => HeightAt(gx, gy) >= 0.35f;   // água não

    // Cada chunk tem 50% de chance de ter um buraco de caverna. Usa a semente + coordenada
    // do chunk, então o mesmo chunk sempre tem o mesmo buraco no mesmo lugar.
    public bool TryGetHole(int cx, int cy, out Vector2Int cell)
    {
        var rng = new System.Random(seed * 73856093 ^ cx * 19349663 ^ cy * 83492791);
        cell = new Vector2Int(cx * size + rng.Next(2, size - 2), cy * size + rng.Next(2, size - 2));
        return rng.NextDouble() < 0.5 && IsWalkable(cell.x, cell.y);
    }

    public List<ItemSpawn> GetItems(int cx, int cy, HashSet<string> collected)
    {
        var list = new List<ItemSpawn>();
        int i = 0;
        for (int y = 0; y < size; y += 3)          // testa a cada 3 células
            for (int x = 0; x < size; x += 3)
            {
                int gx = cx * size + x, gy = cy * size + y;
                // outro offset = mapa de densidade independente do terreno
                float d = Noise.Fbm(gx, gy, 0.1f, 3, 0.5f, 2f, offset + new Vector2(5000, 5000));
                if (d < 0.62f || !IsWalkable(gx, gy)) continue;

                string key = $"{seed}:{cx}:{cy}:{i}";
                i++;   // avança MESMO se já foi coletado
                if (collected.Contains(key)) continue;
                list.Add(new ItemSpawn { key = key, id = d > 0.7f ? "gema" : "moeda", cell = new Vector2Int(gx, gy) });
            }
        return list;
    }

    Color BandColor(float h)
    {
        if (h < 0.35f) return new Color(0.2f, 0.4f, 0.8f);   // água
        if (h < 0.45f) return new Color(0.9f, 0.8f, 0.5f);   // areia
        if (h < 0.70f) return new Color(0.3f, 0.65f, 0.25f); // grama
        return new Color(0.85f, 0.95f, 1f);                  // gelo
    }

    public Mesh BuildMesh(int cx, int cy)
    {
        var verts = new Vector3[size * size * 4];
        var colors = new Color[verts.Length];
        var tris = new int[size * size * 6];
        int v = 0, t = 0;
        bool hasHole = TryGetHole(cx, cy, out Vector2Int hole);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int gx = cx * size + x;   // coordenada GLOBAL: é o que evita a costura
                int gy = cy * size + y;
                float n = Noise.Fbm(gx, gy, scale, octaves, persistence, lacunarity, offset);
                Color c = BandColor(curve.Evaluate(n));
                if (hasHole && gx == hole.x && gy == hole.y) c = Color.black;   // buraco
                verts[v] = new Vector3(x, y);
                verts[v + 1] = new Vector3(x, y + 1);
                verts[v + 2] = new Vector3(x + 1, y + 1);
                verts[v + 3] = new Vector3(x + 1, y);
                for (int k = 0; k < 4; k++) colors[v + k] = c;
                tris[t++] = v; tris[t++] = v + 1; tris[t++] = v + 2;
                tris[t++] = v; tris[t++] = v + 2; tris[t++] = v + 3;
                v += 4;
            }
        return new Mesh { vertices = verts, colors = colors, triangles = tris };
    }
}