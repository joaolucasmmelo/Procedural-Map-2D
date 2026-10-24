using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu Proc2D > Configurar cena: adiciona GameManager e InventoryUI no World e liga as referências.
public static class Proc2DSetup
{
    [MenuItem("Proc2D/Configurar cena")]
    public static void Setup()
    {
        GameObject world = GameObject.Find("World");
        if (world == null) { Debug.LogError("Objeto 'World' não encontrado na cena."); return; }

        var state = world.GetComponent<WorldState>();
        var gen = world.GetComponent<ChunkGenerator>();
        var chunks = world.GetComponent<ChunkManager>();
        var cave = world.GetComponent<CaveManager>();

        var gm = world.GetComponent<GameManager>() ?? world.AddComponent<GameManager>();
        gm.state = state;
        gm.generator = gen;
        gm.chunks = chunks;
        gm.cave = cave;

        var ui = world.GetComponent<InventoryUI>() ?? world.AddComponent<InventoryUI>();
        ui.state = state;
        ui.chunks = chunks;

        EditorUtility.SetDirty(world);
        EditorSceneManager.MarkSceneDirty(world.scene);
        EditorSceneManager.SaveScene(world.scene);
        Debug.Log("[Proc2D] Cena configurada. Aperte Play.");
    }
}
