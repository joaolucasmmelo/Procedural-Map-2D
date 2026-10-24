using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public float speed = 8f;
    public float runSpeed = 40f;
    public ChunkManager chunks;
    public WorldState state;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        Vector2 dir = Vector2.zero;
        if (kb.wKey.isPressed) dir.y += 1;
        if (kb.sKey.isPressed) dir.y -= 1;
        if (kb.dKey.isPressed) dir.x += 1;
        if (kb.aKey.isPressed) dir.x -= 1;
        float s = kb.leftShiftKey.isPressed ? runSpeed : speed;
        Vector3 delta = dir.normalized * s * Time.deltaTime;
        TryMove(new Vector3(delta.x, 0, 0));   // X e Y separados: deixa "deslizar" na margem
        TryMove(new Vector3(0, delta.y, 0));
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Item item)) state.Collect(item);
    }

    void TryMove(Vector3 d)
    {
        Vector3 target = transform.position + d;
        if (chunks.IsWalkable(target)) transform.position = target;
    }
}