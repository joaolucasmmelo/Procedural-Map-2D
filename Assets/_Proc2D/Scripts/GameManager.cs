using UnityEngine;
using UnityEngine.InputSystem;

// Liga as partes e trata as teclas: F5 salva, F9 carrega, N mundo novo, R regenera.
public class GameManager : MonoBehaviour
{
    public WorldState state;
    public ChunkGenerator generator;
    public ChunkManager chunks;
    public CaveManager cave;

    void Start()
    {
        generator.SetSeed(state.seed);
        chunks.Rebuild(chunks.SpawnPoint());
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.f5Key.wasPressedThisFrame) Save();
        if (kb.f9Key.wasPressedThisFrame) Load();
        if (kb.nKey.wasPressedThisFrame) NewWorld();
        if (kb.rKey.wasPressedThisFrame) Regenerate();
    }

    void Save()
    {
        // dentro da caverna, salva a posição da entrada na superfície
        state.playerPosition = cave.InCave ? cave.SurfacePosition : chunks.PlayerTruePosition;
        state.Save();
        state.Say("Jogo salvo");
        Debug.Log("Save: " + WorldState.SavePath);
    }

    void Load()
    {
        cave.ForceExit();
        if (!state.Load()) { state.Say("Nenhum save (aperte F5 antes)"); return; }
        generator.SetSeed(state.seed);           // semente diferente = mundo atual descartado
        chunks.Rebuild(state.playerPosition);
        state.Say($"Save carregado (semente {state.seed})");
    }

    void NewWorld()
    {
        cave.ForceExit();
        state.NewWorld(Random.Range(1, 100000));
        generator.SetSeed(state.seed);
        chunks.Rebuild(chunks.SpawnPoint());
        state.Say($"Mundo novo: semente {state.seed}");
    }

    // Mesma semente, parâmetros atuais do Inspector (escala, oitavas, curva...).
    void Regenerate()
    {
        cave.ForceExit();
        Vector3 pos = chunks.PlayerTruePosition;
        chunks.Rebuild(chunks.IsWalkable(pos - chunks.worldOrigin) ? pos : chunks.SpawnPoint());
        state.Say("Terreno regenerado");
    }
}
