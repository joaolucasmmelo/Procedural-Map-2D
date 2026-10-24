using UnityEngine;
using UnityEngine.InputSystem;

public class CaveManager : MonoBehaviour
{
    public ChunkManager chunks;
    public ChunkGenerator generator;
    public Transform player;
    public Material material;
    public int width = 48, height = 32;
    public WorldState state;
    public Item itemPrefab;

    public bool InCave { get; private set; }

    bool[,] wall;
    GameObject caveObj;
    Vector3 caveCorner;       // posição na cena do canto (0,0) da caverna
    Vector2Int exitCell;      // célula amarela: entrada/saída
    Vector3 returnTruePos;    // onde voltar na superfície (posição verdadeira)

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.eKey.wasPressedThisFrame) return;
        if (InCave) TryExit(); else TryEnter();
    }

    void TryEnter()
    {
        Vector3 p = player.position + chunks.worldOrigin;
        var cell = new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
        int s = generator.size;
        var chunk = new Vector2Int(Mathf.FloorToInt((float)cell.x / s), Mathf.FloorToInt((float)cell.y / s));
        if (!generator.TryGetHole(chunk.x, chunk.y, out Vector2Int hole)) return;
        if (Mathf.Abs(cell.x - hole.x) > 1 || Mathf.Abs(cell.y - hole.y) > 1) return;   // longe do buraco

        // semente própria da caverna: mundo + posição do buraco
        int caveSeed = generator.seed * 31 + hole.x * 7919 + hole.y * 104729;
        wall = CaveGenerator.Generate(caveSeed, width, height);
        exitCell = FloorClosestToCenter();

        returnTruePos = p;
        // posiciona a caverna para a saída ficar exatamente onde o jogador está
        caveCorner = new Vector3(Mathf.Floor(player.position.x) - exitCell.x,
                                 Mathf.Floor(player.position.y) - exitCell.y, 0);
        caveObj = new GameObject($"Caverna {hole}");
        caveObj.transform.position = caveCorner;
        caveObj.AddComponent<MeshFilter>().mesh = BuildMesh();
        caveObj.AddComponent<MeshRenderer>().material = material;

        int i = 0;
        for (int y = 0; y < height; y += 3)
            for (int x = 0; x < width; x += 3)
            {
                float d = Noise.Fbm(x + hole.x * 100, y + hole.y * 100, 0.15f, 3, 0.5f, 2f, new Vector2(9000.5f, 9000.5f));
                if (wall[x, y] || d < 0.55f) continue;
                string key = $"{generator.seed}:cav:{hole.x}:{hole.y}:{i}";
                i++;
                if (state.collected.Contains(key)) continue;
                Item item = Instantiate(itemPrefab, caveObj.transform);
                item.transform.position = caveCorner + new Vector3(x + 0.5f, y + 0.5f, 0);
                item.Setup(key, "cristal");
            }

        InCave = true;
        chunks.SetChunksVisible(false);
        player.position = caveCorner + new Vector3(exitCell.x + 0.5f, exitCell.y + 0.5f, 0);
    }

    void TryExit()
    {
        Vector3 local = player.position - caveCorner;
        if (Mathf.FloorToInt(local.x) != exitCell.x || Mathf.FloorToInt(local.y) != exitCell.y) return;
        ForceExit();
    }

    // Onde o jogador volta na superfície (posição verdadeira). Usado pelo save dentro da caverna.
    public Vector3 SurfacePosition => returnTruePos;

    // Sai da caverna na hora, de onde estiver (usado antes de carregar/regenerar).
    public void ForceExit()
    {
        if (!InCave) return;
        Destroy(caveObj.GetComponent<MeshFilter>().sharedMesh);
        Destroy(caveObj);
        InCave = false;
        chunks.SetChunksVisible(true);
        player.position = returnTruePos - chunks.worldOrigin;
    }

    public bool IsFloor(Vector3 scenePos)
    {
        Vector3 local = scenePos - caveCorner;
        int x = Mathf.FloorToInt(local.x), y = Mathf.FloorToInt(local.y);
        return x >= 0 && y >= 0 && x < width && y < height && !wall[x, y];
    }

    Vector2Int FloorClosestToCenter()
    {
        var center = new Vector2Int(width / 2, height / 2);
        Vector2Int best = center;
        int bestDist = int.MaxValue;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (!wall[x, y] && (new Vector2Int(x, y) - center).sqrMagnitude < bestDist)
                {
                    bestDist = (new Vector2Int(x, y) - center).sqrMagnitude;
                    best = new Vector2Int(x, y);
                }
        return best;
    }

    Mesh BuildMesh()   // mesmo esquema do CaveTest
    {
        var verts = new Vector3[width * height * 4];
        var colors = new Color[verts.Length];
        var tris = new int[width * height * 6];
        int v = 0, t = 0;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Color c = wall[x, y] ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.6f, 0.45f, 0.3f);
                if (x == exitCell.x && y == exitCell.y) c = Color.yellow;
                verts[v] = new Vector3(x, y); verts[v + 1] = new Vector3(x, y + 1);
                verts[v + 2] = new Vector3(x + 1, y + 1); verts[v + 3] = new Vector3(x + 1, y);
                for (int k = 0; k < 4; k++) colors[v + k] = c;
                tris[t++] = v; tris[t++] = v + 1; tris[t++] = v + 2;
                tris[t++] = v; tris[t++] = v + 2; tris[t++] = v + 3;
                v += 4;
            }
        return new Mesh { vertices = verts, colors = colors, triangles = tris };
    }
}