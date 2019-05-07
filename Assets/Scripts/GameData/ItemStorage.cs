using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemStorage : MonoBehaviour
{
    public static Dictionary<Vector3Int, Dictionary<Chunk.TilemapType, Chest>> chests;
    public static Dictionary<Vector3Int, Dictionary<Chunk.TilemapType, Furnace>> furnaces;

    public void Awake() {
        chests = new Dictionary<Vector3Int, Dictionary<Chunk.TilemapType, Chest>>();
        furnaces = new Dictionary<Vector3Int, Dictionary<Chunk.TilemapType, Furnace>>();
    }

    #region Chest_methods
    public static Chest GetChest(Vector3Int position, Chunk.TilemapType tt) {
        position = ChunkData.Global(position);
        if (!chests.ContainsKey(position)
            || !chests[position].ContainsKey(tt)) return null;
        return chests[position][tt];
    }
    public static void AddChest(Vector3Int position, Chunk.TilemapType tt) {
        position = ChunkData.Global(position);
        if (chests.ContainsKey(position)
            && chests[position].ContainsKey(tt)) return;
        if (!chests.ContainsKey(position))
            chests.Add(position, new Dictionary<Chunk.TilemapType, Chest>());
        chests[position].Add(tt, new Chest(position, tt));
    }
    public static void RemoveChest(Vector3Int position, Chunk.TilemapType tt) {
        position = ChunkData.Global(position);
        if (!chests.ContainsKey(position)
            || !chests[position].ContainsKey(tt)) return;
        chests[position].Remove(tt);
    }
    #endregion

    #region Furnace_methods
    public static Furnace GetFurnace(Vector3Int position, Chunk.TilemapType tt) {
        position = ChunkData.Global(position);
        if (!furnaces.ContainsKey(position)
            || !furnaces[position].ContainsKey(tt)) return null;
        return furnaces[position][tt];
    }
    public static void AddFurnace(Vector3Int position, Chunk.TilemapType tt) {
        position = ChunkData.Global(position);
        if (furnaces.ContainsKey(position)
            && furnaces[position].ContainsKey(tt)) return;
        if (!furnaces.ContainsKey(position))
            furnaces.Add(position, new Dictionary<Chunk.TilemapType, Furnace>());
        furnaces[position].Add(tt, Furnace.Create(position, tt));
    }
    public static void RemoveFurnace(Vector3Int position, Chunk.TilemapType tt) {
        position = ChunkData.Global(position);
        if (!furnaces.ContainsKey(position)
            || !furnaces[position].ContainsKey(tt)) return;
        furnaces[position].Remove(tt);
    }
    #endregion
}
