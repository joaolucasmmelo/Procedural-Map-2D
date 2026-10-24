using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Estado do mundo: o que NÃO dá para recalcular a partir da semente.
// Semente, chaves coletadas, inventário e posição global do jogador. É o único que vai para o save.
public class WorldState : MonoBehaviour
{
    public int seed = 7;
    public HashSet<string> collected = new HashSet<string>();
    public Inventory inventory = new Inventory();
    public Vector3 playerPosition;                    // posição VERDADEIRA (cena + origem do mundo)

    public event Action<string> Message;              // avisos para a UI

    public void Collect(Item item)
    {
        if (!inventory.Add(item.id))
        {
            Message?.Invoke("Inventário cheio! O item ficou no chão.");
            return;                                   // NÃO entra em coletados
        }
        collected.Add(item.key);
        Message?.Invoke($"+1 {item.id}");
        Destroy(item.gameObject);
    }

    public void Say(string msg) => Message?.Invoke(msg);

    // ---------- Salvar / carregar (JSON) ----------

    [Serializable]
    class SaveData
    {
        public int semente;
        public List<string> coletados;                // JsonUtility não salva HashSet: vira lista
        public List<InventorySlot> inventario;
        public Vector3 jogador;
    }

    public static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public void Save()
    {
        var data = new SaveData
        {
            semente = seed,
            coletados = new List<string>(collected),
            inventario = inventory.slots,
            jogador = playerPosition,
        };
        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }

    public bool Load()
    {
        if (!File.Exists(SavePath)) return false;
        var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        seed = data.semente;
        collected = new HashSet<string>(data.coletados);
        inventory.SetSlots(data.inventario);
        playerPosition = data.jogador;
        return true;
    }

    public void NewWorld(int newSeed)
    {
        seed = newSeed;
        collected.Clear();
        inventory.SetSlots(new List<InventorySlot>());
    }
}
