using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Interface: só MOSTRA o estado. O inventário é redesenhado quando Inventory.Changed
// dispara (nada de buscar a cada frame). Monta o próprio Canvas por código.
// O texto de debug (semente, posição, chunk) é o único lido por frame: é instrumento para o vídeo.
public class InventoryUI : MonoBehaviour
{
    public WorldState state;
    public ChunkManager chunks;

    Text slotsText, messageText, debugText;
    GameObject panel;

    void Start()
    {
        BuildCanvas();
        state.inventory.Changed += Refresh;   // assina UMA vez
        state.Message += ShowMessage;
        Refresh();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.tabKey.wasPressedThisFrame) panel.SetActive(!panel.activeSelf);

        Vector3 t = chunks.PlayerTruePosition;
        int s = chunks.generator.size;
        debugText.text =
            $"Semente: {state.seed}\n" +
            $"Posição verdadeira: {t.x:F1}, {t.y:F1}\n" +
            $"Posição na cena: {chunks.player.position.x:F1}, {chunks.player.position.y:F1}\n" +
            $"Origem do mundo: {chunks.worldOrigin.x:F0}, {chunks.worldOrigin.y:F0}\n" +
            $"Chunk: {Mathf.FloorToInt(t.x / s)}, {Mathf.FloorToInt(t.y / s)}";
    }

    void Refresh()
    {
        var inv = state.inventory;
        string txt = $"Inventário ({inv.slots.Count}/{inv.capacity})\n";
        foreach (var slot in inv.slots)
            txt += $"{slot.id}: {slot.amount}/{Inventory.MaxStack(slot.id)}\n";
        slotsText.text = txt;
    }

    void ShowMessage(string msg)
    {
        messageText.text = msg;
        CancelInvoke(nameof(ClearMessage));
        Invoke(nameof(ClearMessage), 2f);      // some depois de 2 s
    }

    void ClearMessage() => messageText.text = "";

    // ---------- montagem da UI ----------

    void BuildCanvas()
    {
        var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var root = (RectTransform)canvasGo.transform;

        debugText = MakeText(root, 24, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(20, -20), new Vector2(700, 200), Color.white);

        panel = new GameObject("Inventario", typeof(RectTransform), typeof(Image));
        var prt = (RectTransform)panel.transform;
        prt.SetParent(root, false);
        Place(prt, new Vector2(1, 1), new Vector2(-20, -20), new Vector2(360, 200));
        panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);
        slotsText = MakeText(prt, 30, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(15, -10), new Vector2(330, 180), Color.white);

        messageText = MakeText(root, 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(1200, 60), Color.yellow);
        MakeText(root, 22, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(20, 20), new Vector2(1200, 60), Color.white).text =
            "WASD andar · Shift correr · E entrar/sair da caverna · Tab inventário\nF5 salvar · F9 carregar · N mundo novo · R regenerar";
    }

    Text MakeText(RectTransform parent, int size, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 box, Color color)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(Text), typeof(Shadow));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        Place(rt, anchor, pos, box);
        var t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        return t;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
