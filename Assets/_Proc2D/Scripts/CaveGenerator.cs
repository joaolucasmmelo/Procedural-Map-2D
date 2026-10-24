using System.Collections.Generic;
using UnityEngine;

// Caverna por autômato celular. true = parede, false = chão.
public static class CaveGenerator
{
    public static bool[,] Generate(int seed, int w, int h, float fill = 0.45f, int iterations = 5, int minRegion = 15)
    {
        var rng = new System.Random(seed);   // mesma semente = mesma caverna
        var wall = new bool[w, h];

        // 1. preencher
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                wall[x, y] = IsBorder(x, y, w, h) || rng.NextDouble() < fill;

        // 2. suavizar
        for (int i = 0; i < iterations; i++)
            wall = Smooth(wall, w, h);

        // 3 e 4. achar as salas e apagar as pequenas
        foreach (var region in Regions(wall, w, h))
            if (region.Count < minRegion)
                foreach (var c in region) wall[c.x, c.y] = true;

        // 5. conectar até sobrar uma sala só
        var regions = Regions(wall, w, h);
        while (regions.Count > 1)
        {
            ConnectClosest(wall, regions[0], regions[1], w, h);
            regions = Regions(wall, w, h);
        }
        return wall;
    }

    static bool IsBorder(int x, int y, int w, int h) =>
        x == 0 || y == 0 || x == w - 1 || y == h - 1;

    static bool[,] Smooth(bool[,] g, int w, int h)
    {
        var next = new bool[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                int walls = 0;   // vizinhança de Moore: as 8 em volta
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h || g[nx, ny]) walls++;
                    }
                if (IsBorder(x, y, w, h) || walls > 4) next[x, y] = true;
                else if (walls < 4) next[x, y] = false;
                else next[x, y] = g[x, y];   // exatamente 4: fica como está
            }
        return next;
    }

    // Flood fill: devolve a lista de salas (cada sala = lista de células de chão)
    static List<List<Vector2Int>> Regions(bool[,] wall, int w, int h)
    {
        var regions = new List<List<Vector2Int>>();
        var seen = new bool[w, h];
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                if (wall[x, y] || seen[x, y]) continue;
                var region = new List<Vector2Int>();
                var queue = new Queue<Vector2Int>();
                queue.Enqueue(new Vector2Int(x, y));
                seen[x, y] = true;
                while (queue.Count > 0)
                {
                    var c = queue.Dequeue();
                    region.Add(c);
                    foreach (var d in dirs)
                    {
                        var n = c + d;
                        if (n.x < 0 || n.y < 0 || n.x >= w || n.y >= h) continue;
                        if (wall[n.x, n.y] || seen[n.x, n.y]) continue;
                        seen[n.x, n.y] = true;
                        queue.Enqueue(n);
                    }
                }
                regions.Add(region);
            }
        return regions;
    }

    // Acha o par de células mais próximo entre duas salas e cava um túnel reto
    static void ConnectClosest(bool[,] wall, List<Vector2Int> a, List<Vector2Int> b, int w, int h)
    {
        Vector2Int bestA = a[0], bestB = b[0];
        int best = int.MaxValue;
        foreach (var ca in a)
            foreach (var cb in b)
            {
                int d = (ca - cb).sqrMagnitude;
                if (d < best) { best = d; bestA = ca; bestB = cb; }
            }
        int steps = Mathf.Max(Mathf.Abs(bestB.x - bestA.x), Mathf.Abs(bestB.y - bestA.y));
        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 0 : (float)i / steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(bestA.x, bestB.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(bestA.y, bestB.y, t));
            for (int dx = -1; dx <= 1; dx++)      // túnel com 3 de largura
                for (int dy = -1; dy <= 1; dy++)
                    if (!IsBorder(x + dx, y + dy, w, h) && x + dx > 0 && y + dy > 0 && x + dx < w && y + dy < h)
                        wall[x + dx, y + dy] = false;
        }
    }
}