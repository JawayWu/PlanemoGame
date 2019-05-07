using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public class ItemData : MonoBehaviour {
    public static SliderDataCustomizer[] data;

    private string activeScene;

    private static Dictionary<Chunk.TileType, Chunk.TileType> anomalies;
    private static Dictionary<Chunk.TileType, Chunk.TileType> smelting;
    private static Dictionary<string, int> nameToID;
    private static Dictionary<Chunk.TileType, int> tiletypeToID;

    public void Awake() {
        activeScene = SceneManager.GetActiveScene().name;
        data = Resources.LoadAll<SliderDataCustomizer>("Scriptable Objects/" + activeScene);

        anomalies = new Dictionary<Chunk.TileType, Chunk.TileType>();
        smelting = new Dictionary<Chunk.TileType, Chunk.TileType>();
        nameToID = new Dictionary<string, int>();
        tiletypeToID = new Dictionary<Chunk.TileType, int>();

        //List of tiles that do not break into themselves
        anomalies.Add(Chunk.TileType.DIRT_GRASS, Chunk.TileType.DIRT);
        anomalies.Add(Chunk.TileType.LEAF, Chunk.TileType.STICK);
        anomalies.Add(Chunk.TileType.LEAFAPPLE, Chunk.TileType.STICK);
        anomalies.Add(Chunk.TileType.LEAFORANGE, Chunk.TileType.STICK);
        anomalies.Add(Chunk.TileType.COAL_ORE, Chunk.TileType.COAL);

        //Smelting recipes
        smelting.Add(Chunk.TileType.IRON_ORE, Chunk.TileType.IRON);
        smelting.Add(Chunk.TileType.COPPER_ORE, Chunk.TileType.COPPER);
        smelting.Add(Chunk.TileType.COAL_ORE, Chunk.TileType.COAL);
        smelting.Add(Chunk.TileType.SAND, Chunk.TileType.GLASS);
        smelting.Add(Chunk.TileType.IRON, Chunk.TileType.STEEL);

        for (int i = 0; i < data.Length; i++) {
            nameToID.Add(data[i].itemName, i);
            tiletypeToID.Add(data[i].itemType, i);
        }
    }

    //Returns the ID that the tiletype passed in breaks into
    public static int GetBreakID(Chunk.TileType tt) {
        Chunk.TileType tiletype = anomalies.ContainsKey(tt) ? anomalies[tt] : tt;
        return GetID(tiletype);
    }

    //Returns the ID that the tiletype passed in smelts into
    public static int GetSmeltID(Chunk.TileType tt) {
        Chunk.TileType tiletype = smelting.ContainsKey(tt) ? smelting[tt] : Chunk.TileType.AIR;
        return GetID(tiletype);
    }

    //Returns the int ID of the tiletype passed in.
    public static int GetID(Chunk.TileType tt) {
        return tiletypeToID.ContainsKey(tt) ? tiletypeToID[tt] : 0;
    }

    //Returns the int ID that corresponds to the item name passed in.
    public static int GetID(string name) {
        return nameToID.ContainsKey(name) ? nameToID[name] : 0;
    }

    //Checks if given ID is within array bounds
    private static bool CheckID(int id) {
        bool valid = 0 <= id && id < data.Length;
        if (valid) return true;
        Debug.Log("You attempted to access a tile whose id tag is off the array.");
        return false;
    }

    //Returns the tile that corresponds to the item ID passed in.
    public static TileBase GetTile(int id)
    {
        return CheckID(id) ? data[id].itemTile : null;
    }

    //Returns the tile sprite of the item ID passed in.
    public static Sprite GetTileSprite(int id)
    {
        return CheckID(id) ? data[id].gridSprite : null;
    }

    //Returns the TileType of the item ID passed in.
    public static Chunk.TileType GetTileType(int id) {
        return CheckID(id) ? data[id].itemType : Chunk.TileType.AIR;
    }

    //Returns the name of the item ID passed in.
    public static string GetTileName(int id) {
        return CheckID(id) ? data[id].itemName : "";
    }

    //Return break duration of a given item ID.
    public static float GetBreakDuration(int id) {
        return data[id].breakDuration;
    }

    //Return if an item ID is a light source.
    public static bool IsLightSource(int id) {
        return data[id].lightSource;
    }
}
