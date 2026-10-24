using System;
using System.Collections.Generic;

[Serializable]
public class InventorySlot
{
    public string id;
    public int amount;
}

// Modelo de dados do inventário. Não sabe nada de tela: avisa pelo evento Changed.
public class Inventory
{
    public event Action Changed;                     // a UI escuta isso
    public List<InventorySlot> slots = new List<InventorySlot>();
    public int capacity = 3;                         // número de slots

    public static int MaxStack(string id) => id == "moeda" ? 5 : 2;

    public bool CanAdd(string id)
    {
        if (slots.Count < capacity) return true;     // tem slot vazio
        foreach (var s in slots)
            if (s.id == id && s.amount < MaxStack(id)) return true;   // cabe numa pilha
        return false;
    }

    public bool Add(string id)
    {
        if (!CanAdd(id)) return false;
        foreach (var s in slots)
            if (s.id == id && s.amount < MaxStack(id)) { s.amount++; Changed?.Invoke(); return true; }
        slots.Add(new InventorySlot { id = id, amount = 1 });
        Changed?.Invoke();
        return true;
    }

    // Usado pelo carregar: troca tudo e avisa a UI uma vez.
    public void SetSlots(List<InventorySlot> newSlots)
    {
        slots = new List<InventorySlot>();
        foreach (var s in newSlots) slots.Add(new InventorySlot { id = s.id, amount = s.amount });
        Changed?.Invoke();
    }
}
