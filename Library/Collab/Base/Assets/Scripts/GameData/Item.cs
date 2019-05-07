using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class Item : MonoBehaviour {

    public int id;
    [HideInInspector]
    public int quantity = 1;
    public TileBase tile; //may be null
    [SerializeField]
    [Tooltip("Blank item for initialization")]
    private bool blank;

    private SpriteRenderer sr;
    private BoxCollider2D bc;
    private CircleCollider2D cc;
    private Rigidbody2D rb;

    #region instantiation
    public static Item prefab;
    public static Item Create(int id, int quantity = 0) {
        Item item = Instantiate(prefab);

        item.id = id;
        item.quantity = quantity;
        item.sr.sprite = ItemData.GetTileSprite(id);
        item.tile = ItemData.GetTile(id);

        //Hide item
        item.sr = item.GetComponent<SpriteRenderer>();
        item.bc = item.GetComponent<BoxCollider2D>();
        item.cc = item.GetComponentInChildren<CircleCollider2D>();
        item.rb = item.GetComponent<Rigidbody2D>();
        item.Hide();

        return item;
    }
    public static Item Create(Item other) {
        return Create(other.id, other.quantity);
    }

    //Method to create an Item before we have added everything regarding ItemData.
    public static Item Spawn(Chunk.TileType tiletype, Vector3 position, int quantity = 0)
    {
        //Spawn the prefab at the location we pass in with a locked rotation.
        Item toReturn = Instantiate(prefab, position + new Vector3(0.5f, 0.5f), Quaternion.identity);

        int id = ItemData.GetBreakID(tiletype);
        toReturn.id = id;
        toReturn.quantity = quantity;
        toReturn.sr.sprite = ItemData.GetTileSprite(id);
        toReturn.tile = ItemData.GetTile(id);

        return toReturn;
    }
    #endregion

    #region Unity_functions
    public void Awake() {
        prefab = Resources.Load<Item>("Prefabs/Item");

        if (blank) return;

        //Getting components
        sr = GetComponent<SpriteRenderer>();
        bc = GetComponent<BoxCollider2D>();
        cc = GetComponentInChildren<CircleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    //Check for collision with player
    //If yes, attempt to put item in inventory
    public void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            StartCoroutine(AddToInventory(collision.GetComponent<PlayerMovement>()));
        }
    }
    #endregion

    //Move towards player, then add to inventory
    private IEnumerator AddToInventory(PlayerMovement pm) {
        bc.enabled = cc.enabled = false;
        Vector2 orig = transform.position;
        float elapsedTime = 0, duration = Vector2.Distance(orig, pm.transform.position) / 10;
        while (elapsedTime < duration) {
            elapsedTime += Time.deltaTime;
            transform.position = Vector2.Lerp(orig, pm.transform.position, Mathf.Pow(elapsedTime / duration, 2));
            yield return null;
        }
        pm.inventory.Add(this);
    }

    //Attempt to place item at given position
    public void Place(Chunk ch, Vector3Int pos, Chunk.TilemapType tt) {
        //Make sure item is placeable
        if (tile == null) return;

        //Check if chest
        if (ItemData.GetTileType(id) == Chunk.TileType.CHEST) {
            ItemStorage.AddChest(pos, tt);
        }
        //Check if furnace
        if (ItemData.GetTileType(id) == Chunk.TileType.FURNACE) {
            ItemStorage.AddFurnace(pos, tt);
        }

        ch.SetChunkTile(pos, tt, tile);
        if (tt.Equals(Chunk.TilemapType.BACK_BLOCKS))
        {
            ch.SetChunkTileColor(pos, new Color(0.6f, 0.6f, 0.6f), true);
        }
        ch.SetChunkTileType(pos, tt, ItemData.GetTileType(id));
        quantity--;
        if (quantity == 0) {
            Destroy(gameObject);
        }
    }

    public void Hide() {
        sr.enabled = bc.enabled = cc.enabled = false;
        rb.Sleep();
    }
    
    //Re-display as an entity
    public void Show(Vector3 pos) {
        transform.position = pos;
        transform.localScale = new Vector3(0.5f, 0.5f);
        sr.enabled = bc.enabled = cc.enabled = true;
        rb.WakeUp();
    }

    public void Copy(Item i) {
        id = i.id;
        //quantity = i.quantity;
        tile = i.tile;
    }

    public void Display(Vector3 pos, Vector3 scale, Image image) {
        Transform t = image.transform;
        RectTransform rt = t.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        t.localScale = scale;
        image.GetComponent<Canvas>().sortingOrder = 1;
        image.enabled = true;

        //item quantity
        Text text = image.GetComponentInChildren<Text>();
        if (quantity > 1) {
            text.text = quantity + "";
            text.enabled = true;
        } else {
            text.enabled = false;
        }
    }
}
