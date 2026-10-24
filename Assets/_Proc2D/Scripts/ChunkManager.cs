using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public Transform player;
    public ChunkGenerator generator;
    public CaveManager cave;
    public Material material;
    public bool useFloatingOrigin = true;
    public float originLimit = 1000f;
    public Vector3 worldOrigin;   // soma de todos os deslocamentos
    public WorldState state;
    public Item itemPrefab;

    readonly Dictionary<Vector2Int, GameObject> alive = new Dictionary<Vector2Int, GameObject>();
    Vector2Int current = new Vector2Int(int.MaxValue, int.MaxValue);

    // Primeiro ponto em terra a partir do (0, 0), em posição verdadeira.
    public Vector3 SpawnPoint()
    {
        for (int x = 0; x < 5000; x++)
            if (generator.IsWalkable(x, 0)) return new Vector3(x + 0.5f, 0.5f, 0);
        return Vector3.zero;
    }

    public Vector3 PlayerTruePosition => player.position + worldOrigin;

    // Apaga todos os chunks e põe o jogador numa posição verdadeira. Os 9 chunks
    // são recriados no próximo Update (mundo novo, regenerar ou carregar save).
    public void Rebuild(Vector3 playerTruePos)
    {
        foreach (var go in alive.Values)
        {
            Destroy(go.GetComponent<MeshFilter>().sharedMesh);
            Destroy(go);
        }
        alive.Clear();
        current = new Vector2Int(int.MaxValue, int.MaxValue);
        // origem perto do jogador, para ele aparecer perto do (0, 0) da cena
        worldOrigin = new Vector3(Mathf.Round(playerTruePos.x), Mathf.Round(playerTruePos.y), 0);
        player.position = playerTruePos - worldOrigin;
    }

    public void SetChunksVisible(bool visible)
    {
        foreach (var go in alive.Values) go.SetActive(visible);
    }
    public bool IsWalkable(Vector3 scenePos)
    {
        if (cave.InCave) return cave.IsFloor(scenePos);
        Vector3 p = scenePos + worldOrigin;   // posição verdadeira
        return generator.IsWalkable(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
    }

    void Update()
    {
        if (cave.InCave) return;   // dentro da caverna, o mapa fica parado
        int s = generator.size;
        Vector3 p = player.position + worldOrigin;
        var c = new Vector2Int(Mathf.FloorToInt(p.x / s), Mathf.FloorToInt(p.y / s));
        if (c == current) return;   // continua no mesmo chunk: nada a fazer
        current = c;

        // 1. apaga quem saiu da grade 3x3
        var remove = new List<Vector2Int>();
        foreach (var key in alive.Keys)
            if (Mathf.Abs(key.x - c.x) > 1 || Mathf.Abs(key.y - c.y) > 1)
                remove.Add(key);
        foreach (var key in remove)
        {
            Destroy(alive[key].GetComponent<MeshFilter>().sharedMesh);
            Destroy(alive[key]);
            alive.Remove(key);
        }

        // 2. cria quem falta
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                var key = new Vector2Int(c.x + dx, c.y + dy);
                if (!alive.ContainsKey(key)) alive[key] = CreateChunk(key);
            }
    }

    void LateUpdate()
    {
        Vector3 p = player.position;
        if (!useFloatingOrigin) return;
        if (Mathf.Abs(p.x) < originLimit && Mathf.Abs(p.y) < originLimit) return;

        var shift = new Vector3(Mathf.Round(p.x), Mathf.Round(p.y), 0);
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            root.transform.position -= shift;   // chunks, jogador, tudo
        worldOrigin += shift;
        Debug.Log($"Floating origin: deslocou {shift}, origem agora {worldOrigin}");
    }

    GameObject CreateChunk(Vector2Int c)
    {
        var go = new GameObject($"Chunk ({c.x}, {c.y})");
        go.transform.position = new Vector3(c.x * generator.size, c.y * generator.size, 0) - worldOrigin;
        go.AddComponent<MeshFilter>().mesh = generator.BuildMesh(c.x, c.y);
        go.AddComponent<MeshRenderer>().material = material;
        foreach (var it in generator.GetItems(c.x, c.y, state.collected))
        {
            Item item = Instantiate(itemPrefab, go.transform);
            item.transform.position = new Vector3(it.cell.x + 0.5f, it.cell.y + 0.5f, 0) - worldOrigin;
            item.Setup(it.key, it.id);
        }
        return go;
    }
}