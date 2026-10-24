using UnityEngine;

public class Item : MonoBehaviour
{
    public string key;
    public string id;

    public void Setup(string key, string id)
    {
        this.key = key;
        this.id = id;
        name = "Item " + key;
        GetComponent<SpriteRenderer>().color =
            id == "moeda" ? Color.yellow : id == "gema" ? Color.cyan : Color.magenta;
    }
}