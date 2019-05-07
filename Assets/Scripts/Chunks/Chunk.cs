using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Describes a chunk in the world that can load and unload itself.
/// </summary>
public class Chunk : MonoBehaviour
{
    public bool loading { get; private set; }
    public Vector3Int Position
    {
        get;
        private set;
    }
    public Vector3Int chunkPosition
    {
        get;
        private set;
    }
    public List<LightSource> AmbientLightSources { get; set; }
    public List<LightSource> PlacedLightSources { get; set; }
    public Tilemap frontBlocks, backBlocks;
    public Color[,] tileColors;
    public TileType[,] typeFrontBlocks, typeBackBlocks;
    public enum TileType
    {
        AIR,
        COAL,
        COPPER,
        DIRT,
        DIRT_GRASS,
        IRON,
        STONE,
        BEDROCK,
        LOG,
        WOOD,
        STICK,
        TORCH,
        WORKBENCH,
        LEAF,
        LEAFAPPLE,
        LEAFORANGE,
        SAND,
        CACTUS,
        FURNACE,
        CHEST,
        COAL_ORE,
        IRON_ORE,
        COPPER_ORE,
        WIRE,
        STEEL,
        STEEL_PLATE,
        ENGINE,
        GLASS,
        LANDING_GEAR,
        HATCH,
        WING,
        CONTROL_MODULE,
        BLADE,
        TURBINE,
        SNOW,
        ICE,
        LAVATOP,
        LAVA
    }
    public enum TilemapType
    {
        FRONT_BLOCKS,
        BACK_BLOCKS,
        ENUM_END
    }

    private BoxCollider2D chunkCollider;
    private bool unloading = false;

    private void Awake()
    {
        Position = new Vector3Int((int)transform.position.x, (int)transform.position.y, 0);
        chunkPosition = new Vector3Int(
            Position.x / GenerationManager.Instance.chunkSize,
            Position.y / GenerationManager.Instance.chunkSize, 0);
        AmbientLightSources = new List<LightSource>();
        PlacedLightSources = new List<LightSource>();
        // Check if chunk already exists
        int modX = (((Position.x % GenerationManager.Instance.worldWidth) + GenerationManager.Instance.worldWidth)
            % GenerationManager.Instance.worldWidth) / GenerationManager.Instance.chunkSize;
        Tuple<int, int> tuplePos = new Tuple<int, int>(modX, chunkPosition.y);
        if (ChunkData.frontTiletypes.ContainsKey(tuplePos))
        {
            // Retreieve data from ChunkData
            typeFrontBlocks = ChunkData.frontTiletypes[tuplePos];
            typeBackBlocks = ChunkData.backTiletypes[tuplePos];
            // Use just one color map for whichever block layer is in front.
            tileColors = new Color[
                GenerationManager.Instance.chunkSize,
                GenerationManager.Instance.chunkSize];
            // Set offset and size of chunk's box collider relative to chunkSize.
            chunkCollider = GetComponent<BoxCollider2D>();
            chunkCollider.size = new Vector2(GenerationManager.Instance.chunkSize, GenerationManager.Instance.chunkSize);
            chunkCollider.offset = new Vector2(GenerationManager.Instance.chunkSize / 2, GenerationManager.Instance.chunkSize / 2);

            LoadChunk();
        }
        else
        {
            // Stores block types for each tilemap
            typeFrontBlocks = new TileType[
                GenerationManager.Instance.chunkSize,
                GenerationManager.Instance.chunkSize];
            typeBackBlocks = new TileType[
                GenerationManager.Instance.chunkSize,
                GenerationManager.Instance.chunkSize];

            // Use just one color map for whichever block layer is in front.
            tileColors = new Color[
                GenerationManager.Instance.chunkSize,
                GenerationManager.Instance.chunkSize];

            // Set offset and size of chunk's box collider relative to chunkSize.
            chunkCollider = GetComponent<BoxCollider2D>();
            chunkCollider.size = new Vector2(GenerationManager.Instance.chunkSize, GenerationManager.Instance.chunkSize);
            chunkCollider.offset = new Vector2(GenerationManager.Instance.chunkSize / 2, GenerationManager.Instance.chunkSize / 2);

            LoadChunk();
        }
    }

    /// Generates chunk
    public IEnumerator LoadChunk()
    {
        loading = true;
        yield return StartCoroutine(GenerationManager.Instance.GenerateChunk(this));
        loading = false;
    }

    /// Deletes chunk
    /// Takes time to unload
    public void UnloadChunk()
    {
        // Remove all chunk light objects
        foreach (LightSource light in AmbientLightSources)
            if (light != null)
                LightingManager.Instance.RemoveLightSource(light, false);
        foreach (LightSource light in PlacedLightSources)
            if (light != null)
                LightingManager.Instance.RemoveLightSource(light, false);
        Destroy(gameObject);
        unloading = true;
    }
    /// <summary>
    /// Changes the color of every tile in this chunk to white, making them fully visible.
    /// Only changes visuals on the Tilemaps. The color map still retains and updates correct lighting in the background.
    /// </summary>
    public void DisableLighting()
    {
        for (int h = 0; h < GenerationManager.Instance.chunkSize; h++)
            for (int v = 0; v < GenerationManager.Instance.chunkSize; v++)
                SetChunkTileColor(new Vector3Int(Position.x + h, Position.y + v, 0), Color.white, true);
    }
    /// <summary>
    /// Changes the color of every tile in this chunk to its color map equivalent, effectively restoring their lighting.
    /// Only changes visuals on the Tilemaps. The color map is used to restore the Tile colors and is not affected further at all.   
    /// </summary>
    public void EnableLighting()
    {
        for (int h = 0; h < GenerationManager.Instance.chunkSize; h++)
            for (int v = 0; v < GenerationManager.Instance.chunkSize; v++)
                SetChunkTileColor(new Vector3Int(Position.x + h, Position.y + v, 0), tileColors[h, v], true);
    }
    /// Returns matching tilemap for the tilemap type 
    private Tilemap GetMap(TilemapType mapType)
    {
        if (unloading)
            return null;

        switch (mapType)
        {
            case TilemapType.FRONT_BLOCKS:
                return frontBlocks;
            case TilemapType.BACK_BLOCKS:
                return backBlocks;
            default:
                return null;
        }
    }

    /// Returns the right TileType array for the given TilemapType constant for this chunk.
    /// These TileType arrays hold the block types for all chunk blocks.
    private TileType[,] GetTypeMap(TilemapType mapType)
    {
        if (unloading)
            return null;

        switch (mapType)
        {
            case TilemapType.FRONT_BLOCKS:
                return typeFrontBlocks;
            case TilemapType.BACK_BLOCKS:
                return typeBackBlocks;
            default:
                return null;
        }
    }
    /// <summary>
    /// Returns the right color map array for the given TilemapType constant for this chunk.
    /// The color maps store the color per tile in the chunk.
    /// </summary>
    /// <param name="mapType"></param>
    /// <returns></returns>
    private Color[,] GetColorMap(TilemapType mapType)
    {
        if (unloading)
            return null;

        return tileColors;
    }

    /// <summary>
    /// Returns the tile stored in the given Tilemap, on the given position.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="mapType"></param>
    /// <returns></returns>
    public TileBase GetChunkTile(Vector3Int position, TilemapType mapType)
    {
        if (unloading)
            return null;

        Tilemap targetMap = GetMap(mapType);
        if (targetMap == null)
            return null;

        Vector3Int relativePosition = position - Position;
        return targetMap.GetTile(relativePosition);
    }


    /// <summary>
    /// Fills the given position in the given Tilemap with the given Tile.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="mapType"></param>
    /// <param name="tile"></param>
    public void SetChunkTile(Vector3Int position, TilemapType mapType, TileBase tile)
    {
        if (unloading)
            return;

        Tilemap targetMap = GetMap(mapType);
        if (targetMap == null)
            return;

        Vector3Int relativePosition = position - Position;
        targetMap.SetTile(relativePosition, tile);
    }
    /// <summary>
    /// Returns the color of the tile at the given position.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="mapType"></param>
    /// <returns></returns>
    public Color GetChunkTileColor(Vector3Int position)
    {
        if (unloading)
            return Color.clear;

        Vector3Int relativePosition = position - Position;
        if (relativePosition.x < 0f || relativePosition.x >= GenerationManager.Instance.chunkSize ||
            relativePosition.y < 0f || relativePosition.y >= GenerationManager.Instance.chunkSize)
            return Color.clear;

        return tileColors[relativePosition.x, relativePosition.y];
    }

    /// <summary>
    /// Sets the color of the tile in the given Tilemap at the given position.
    /// 
    /// NOTE: Tiles by default have TileFlags that prevent color changes, rotation
    /// lock etc. Make sure you unlock these tiles by changing its TileFlags or go 
    /// into the inspector and visit your tiles, turn on debug mode and change it there,
    /// before using this function.    
    /// </summary>
    /// <param name="position"></param>
    /// <param name="color"></param>
    /// <param name="mapType"></param>
    public void SetChunkTileColor(Vector3Int position, Color color, bool visualsOnly = false)
    {
        if (unloading)
            return;

        Vector3Int relativePosition = position - Position;
        if (relativePosition.x < 0f || relativePosition.x >= GenerationManager.Instance.chunkSize ||
            relativePosition.y < 0f || relativePosition.y >= GenerationManager.Instance.chunkSize)
            return;

        if (!visualsOnly)
            tileColors[relativePosition.x, relativePosition.y] = color;

        if (!LightingManager.Instance.LightingIsActive)
            color = Color.white;

        frontBlocks.SetColor(relativePosition, color);
        backBlocks.SetColor(relativePosition, new Color(
            color.r * LightingManager.Instance.BackLayerShadowFactor,
            color.g * LightingManager.Instance.BackLayerShadowFactor,
            color.b * LightingManager.Instance.BackLayerShadowFactor));
    }


    /// <summary>
    /// Returns the tile type of a block in this chunk, in the given Tilemap.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="mapType"></param>
    /// <returns></returns>
    public TileType GetChunkTileType(Vector3Int position, TilemapType mapType)
    {
        if (unloading)
            return TileType.AIR;

        TileType[,] data = GetTypeMap(mapType);
        if (data == null)
            return TileType.AIR;

        Vector3Int relativePosition = position - Position;
        if (relativePosition.x < 0f || relativePosition.x >= GenerationManager.Instance.chunkSize ||
            relativePosition.y < 0f || relativePosition.y >= GenerationManager.Instance.chunkSize)
            return TileType.AIR;
        return data[relativePosition.x, relativePosition.y];
    }


    /// <summary>
    /// Sets the tile type of a block in this chunk, in the given Tilemap.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="mapType"></param>
    /// <param name="type"></param>
    public void SetChunkTileType(Vector3Int position, TilemapType mapType, TileType type)
    {
        if (unloading)
            return;

        TileType[,] data = GetTypeMap(mapType);
        if (data == null)
            return;

        Vector3Int relativePosition = position - Position;
        if (relativePosition.x < 0f || relativePosition.x >= GenerationManager.Instance.chunkSize ||
            relativePosition.y < 0f || relativePosition.y >= GenerationManager.Instance.chunkSize)
            return;
        data[relativePosition.x, relativePosition.y] = type;
    }


    /// <summary>
    /// Sets the rotation of a given tile at the given position in the given Tilemap.
    /// 
    /// NOTE: Tiles by default have TileFlags that prevent color changes, rotation
    /// lock etc. Make sure you unlock these tiles by changing its TileFlags or go 
    /// into the inspector and visit your tiles, turn on debug mode and change it there,
    /// before using this function.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="mapType"></param>
    /// <param name="rotation"></param>
    public void SetChunkTileRotation(Vector3Int position, TilemapType mapType, Vector3 rotation)
    {
        if (unloading)
            return;

        Tilemap targetMap = GetMap(mapType);
        if (targetMap == null)
            return;

        Vector3Int relativePosition = position - Position;
        Quaternion matrixRotation = Quaternion.Euler(rotation);
        Matrix4x4 matrix = Matrix4x4.Rotate(matrixRotation);
        targetMap.SetTransformMatrix(relativePosition, matrix);
    }
}