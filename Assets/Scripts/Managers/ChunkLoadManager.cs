using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles the loading and unloading of chunks in the game.
/// </summary>
public class ChunkLoadManager : MonoBehaviour//Singleton<ChunkLoadManager>
{
    public static ChunkLoadManager Instance;

    [Header("Chunks")]
    public SliderData chunkData;
    public GameObject chunkPrefab;
    public GameObject chunkRoot;
    public LayerMask chunkLayer;
    public List<Chunk> chunksToLoad;
    private bool isUpdatingChunks = false;
    private WaitForSeconds scanCooldownTime = new WaitForSeconds(0.5f);
    private RaycastHit2D hit;

    private void Awake() {
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(LoadChunks());
        StartCoroutine(UnloadChunks());
    }

    /// <summary>
    /// Returns whether the given position is an air block or not.
    /// Checks the TileType on the given position in the related chunk.
    /// </summary>
    /// <param name="tilePosition"></param>
    /// <param name="chunk"></param>
    /// <returns></returns>
    public bool IsAirBlock(Vector3Int tilePosition, Chunk chunk = null)
    {
        if (chunk == null)
            chunk = GetChunk(tilePosition);
        if (chunk == null)
            return true;

        for (int i = 0; i < (int)Chunk.TilemapType.ENUM_END; i++)
        {
            if (chunk.GetChunkTileType(tilePosition, (Chunk.TilemapType)i) != Chunk.TileType.AIR)
                return false;
        }
        return true;
    }


    /// <summary>
    /// Checks whether a given tile position has adjacent tiles that are not air blocks.
    /// Uses a simple sin and cos trigonometry calculation to quickly find the up, right, 
    /// down and left sides of the position in that order.
    /// </summary>
    /// <param name="tilePosition"></param>
    /// <param name="chunk"></param>
    /// <returns></returns>
    public bool HasAdjacentTiles(Vector3Int tilePosition, Chunk chunk = null)
    {
        if (chunk == null)
            chunk = GetChunk(tilePosition);
        if (chunk == null)
            return false;

        for (float i = 0; i < Mathf.PI * 2f; i += Mathf.PI / 2f)
        {
            if (!IsAirBlock(new Vector3Int(
                tilePosition.x + Mathf.RoundToInt(Mathf.Sin(i)),
                tilePosition.y + Mathf.RoundToInt(Mathf.Cos(i)), 0)))
                return true;
        }
        return false;
    }
    /// <summary>
    /// Removes all chunks currently in game.
    /// </summary>
    public void ClearAllChunks()
    {
        StopAllCoroutines();

        // Get all chunks
        foreach (Chunk chunk in GetAllChunks())
            chunk.UnloadChunk();

        StartCoroutine(LoadChunks());
        StartCoroutine(UnloadChunks());
    }
    /// <summary>
    /// Returns a list with all chunks currently in the game.
    /// </summary>
    /// <returns></returns>
    public List<Chunk> GetAllChunks()
    {
        List<Chunk> chunks = new List<Chunk>();
        foreach (Transform child in chunkRoot.transform)
        {
            Chunk chunk = child.GetComponent<Chunk>();
            if (chunk != null)
                chunks.Add(chunk);
        }
        return chunks;
    }
    /// <summary>
    /// Returns a list with all chunks currently in the game, inside the given Rect boundaries.
    /// Optionally allows inversion, returning all chunks outside the given Rect boundaries instead.
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="inverse"></param>
    /// <returns></returns>
    public List<Chunk> GetAllChunks(Rect rect, bool inverse = false)
    {
        List<Chunk> chunks = new List<Chunk>();
        foreach (Transform child in chunkRoot.transform)
        {
            Chunk chunk = child.GetComponent<Chunk>();
            if (chunk != null)
            {
                if (!inverse)
                {
                    if (rect.Contains(chunk.chunkPosition))
                        chunks.Add(chunk);
                }
                else
                {
                    if (!rect.Contains(chunk.chunkPosition))
                        chunks.Add(chunk);
                }
            }
        }
        return chunks;
    }
    /// <summary>
    /// Returns the chunk at the given position. 
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public Chunk GetChunk(Vector3Int position)
    {
        hit = Physics2D.Raycast(
            new Vector2(position.x + 0.5f, position.y + 0.5f),
            Vector2.zero, 0f, chunkLayer);
        return hit ? hit.collider.GetComponent<Chunk>() : null;
    }

    /// <summary>
    /// Returns the tile stored in the chunk at the given position, in the given map.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="mapType"></param>
    /// <returns></returns>
    public TileBase GetChunkTile(Vector3Int position, Chunk.TilemapType mapType)
    {
        Chunk chunk = GetChunk(position);
        if (chunk != null)
            return chunk.GetChunkTile(position, mapType);
        return null;
    }

    /// <summary>
    /// Sets the tile in the chunk at the given position, in the given map.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="mapType"></param>
    /// <param name="tile"></param>
    public void SetChunkTile(Vector3Int position, Chunk.TilemapType mapType, Tile tile)
    {
        Chunk chunk = GetChunk(position);
        if (chunk != null)
            chunk.SetChunkTile(position, mapType, tile);
    }

    /// <summary>
    /// Returns the type of tile stored in the chunk at the given position, in the given map.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="mapType"></param>
    /// <returns></returns>
    public Chunk.TileType GetChunkTileType(Vector3Int position, Chunk.TilemapType mapType)
    {
        Chunk chunk = GetChunk(position);
        if (chunk != null)
            return chunk.GetChunkTileType(position, mapType);
        return Chunk.TileType.AIR;
    }
    /// <summary>
    /// Returns the type of tile stored in the chunk at the given position, in the given map.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="mapType"></param>
    /// <returns></returns>
    public Color GetChunkTileColor(Vector3Int position)
    {
        Chunk chunk = GetChunk(position);
        if (chunk != null)
            return chunk.GetChunkTileColor(position);
        return Color.clear;
    }
    /// <summary>
    /// Sets the type of tile for the chunk at the given position, in the given map.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="mapType"></param>
    /// <param name="type"></param>
    public void SetChunkTileType(Vector3Int position, Chunk.TilemapType mapType, Chunk.TileType type)
    {
        Chunk chunk = GetChunk(position);
        if (chunk != null)
            chunk.SetChunkTileType(position, mapType, type);
    }
    /// <summary>
    /// Converts mouse position to tile position and returns it.
    /// </summary>
    /// <returns></returns>
    public Vector3Int GetTilePositionAtMouse()
    {
        Vector3 rawPosition = Camera.main.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, 50));
        return new Vector3Int((int)rawPosition.x, (int)rawPosition.y, 0);
    }
    public Vector3Int GetTilePosition(Vector3 input)
    {
        Vector3 rawPosition = Camera.main.ScreenToWorldPoint(
            new Vector3(input.x, input.y, 50));
        return new Vector3Int((int)rawPosition.x, (int)rawPosition.y, 0);
    }

    /// <summary>
    /// Returns a Rect that defines the area where chunks can be loaded in.
    /// </summary>
    /// <returns></returns>
    private Rect GetChunkLoadBounds()
    {
        Vector3 regionStart = Camera.main.transform.position +
            Vector3.left * chunkData.GetSliderData(SliderData.SliderField.CHUNK_RADIUS_HORIZONTAL) +
            Vector3.down * chunkData.GetSliderData(SliderData.SliderField.CHUNK_RADIUS_VERTICAL);
        Vector3 regionEnd = Camera.main.transform.position +
            Vector3.right * chunkData.GetSliderData(SliderData.SliderField.CHUNK_RADIUS_HORIZONTAL) +
            Vector3.up * chunkData.GetSliderData(SliderData.SliderField.CHUNK_RADIUS_VERTICAL);

        // Convert to int for automatic flooring of coordinates
        if (regionStart.x < 0) regionStart.x -= GenerationManager.Instance.chunkSize;
        int regionStartX = (int)regionStart.x / GenerationManager.Instance.chunkSize;
        int regionStartY = (int)regionStart.y / GenerationManager.Instance.chunkSize;
        int regionEndX = ((int)regionEnd.x + GenerationManager.Instance.chunkSize) / GenerationManager.Instance.chunkSize;
        int regionEndY = ((int)regionEnd.y + GenerationManager.Instance.chunkSize) / GenerationManager.Instance.chunkSize;
        Rect loadBoundaries = new Rect(regionStartX, regionStartY, regionEndX - regionStartX, regionEndY - regionStartY);

        return loadBoundaries;
    }

    /// <summary>
    /// Starts and maintains the sequence for loading chunks.
    /// </summary>
    /// <param name="done"></param>
    /// <param name="loadAll"></param>
    private IEnumerator LoadChunks()
    {
        while (true)
        {
            yield return StartCoroutine(PerformLoadChunks());
            yield return scanCooldownTime;
        }
    }

    /// <summary>
    /// Starts and maintains the sequence for unloading chunks.
    /// </summary>
    /// <returns></returns>
    private IEnumerator UnloadChunks()
    {
        while (true)
        {
            PerformUnloadChunks();
            yield return scanCooldownTime;
        }
    }


    /// <summary>
    /// Scans and loads chunks in view using a Rect as bounds.
    /// </summary>
    /// <param name="loadAll"></param>
    private IEnumerator PerformLoadChunks()
    {
        chunksToLoad = new List<Chunk>();
        Rect loadBoundaries = GetChunkLoadBounds();
        Chunk lastChunkCreated = null;
        for (int h = (int)loadBoundaries.xMax; h >= (int)loadBoundaries.xMin; h--)
        {
            for (int v = (int)loadBoundaries.yMax; v >= (int)loadBoundaries.yMin; v--)
            {
                Vector3Int chunkPosition = new Vector3Int(h, v, 0);
                Vector3Int worldPosition = new Vector3Int(
                    h * GenerationManager.Instance.chunkSize,
                    v * GenerationManager.Instance.chunkSize, 0);

                if (loadBoundaries.Contains(chunkPosition) && !GetChunk(worldPosition))
                {
                    // Chunk automatically loads itself upon creation
                    lastChunkCreated = Instantiate(chunkPrefab, worldPosition, Quaternion.identity, chunkRoot.transform)
                        .GetComponent<Chunk>();
                    chunksToLoad.Add(lastChunkCreated);
                    StartCoroutine(lastChunkCreated.LoadChunk());
                }
            }
        }
        if (chunksToLoad.Count == 0)
            yield break;

        // Sort the chunks on distance from the camera
        chunksToLoad = chunksToLoad.OrderBy(
            x => Vector2.Distance(Camera.main.transform.position, x.transform.position)).ToList();

        //LOAD ALL CHUNKS BEFORE THIS POINT OTHERWISE LIGHTING WILL NOT COVER EVERYTHING
        //THIS IS TO CHECK
        while (true)
        {
            bool breakOut = true;
            foreach (Chunk chunk in chunksToLoad)
                if (chunk != null && chunk.loading)
                    breakOut = false;

            if (breakOut)
                break;

            yield return null;
        }

        Queue<LightNode> seamQueue = new Queue<LightNode>();
        foreach (Chunk chunk in chunksToLoad)
        {
            if (chunk == null)
                continue;

            int sw = GenerationManager.Instance.spaceship.GetLength(1), sh = GenerationManager.Instance.spaceship.GetLength(0); //spaceship width/height
            Vector3Int spaceshipPosBelow = new Vector3Int(GenerationManager.Instance.spaceshipPos.x, GenerationManager.Instance.spaceshipPos.y - sh, 0);

            Chunk currChunk = GetChunk(GenerationManager.Instance.spaceshipPos);
            Chunk belowChunk = GetChunk(spaceshipPosBelow);

            // Create ambient lights for each new chunk. 
            chunk.AmbientLightSources = LightingManager.Instance.CreateAmbientLightSources(chunk, false);
            if (chunk.AmbientLightSources.Count > 0 || chunk == currChunk || chunk == belowChunk)
            {
                if (chunk == currChunk || chunk == GetChunk(spaceshipPosBelow))
                {
                    Vector3Int globalPos = ChunkData.Global(chunk.Position);

                    int ground = (int)2e9;
                    for (int h = GenerationManager.Instance.chunkSize / 2 - sw / 2; h < GenerationManager.Instance.chunkSize / 2 + sw / 2; h++)
                    {
                        ground = Mathf.Min(ground, GenerationManager.Instance.surfaceHeights[globalPos.x + h]);
                    }
                    for (int r = 0; r < sh; r++)
                    {
                        int v = ground + (sh - r) - globalPos.y;
                        if (v < 0 || v >= GenerationManager.Instance.chunkSize) continue;

                        for (int c = 0; c < sw; c++)
                        {
                            int h = GenerationManager.Instance.chunkSize / 2 - sw / 2 + c;
                            Vector3Int tilePos = new Vector3Int(chunk.Position.x + h, chunk.Position.y + v, 0);
                            if (GenerationManager.Instance.spaceship[r, c] == 7)
                            {
                                chunk.AmbientLightSources.Add(LightingManager.Instance.CreateLightSource(tilePos, LightingManager.Instance.ambientLightColor, LightingManager.Instance.ambientLightStrength, false));
                            }
                        }
                    }
                }
                // Update ambient lights smoothly. Reverse the list of lights depending on the chunk's position, so it always starts at the
                // side the player would see first. */
                if (chunk.Position.x < Camera.main.transform.position.x)
                    chunk.AmbientLightSources.Reverse();
                
                foreach (LightSource light in chunk.AmbientLightSources)
                    yield return StartCoroutine(LightingManager.Instance.UpdateLightSmooth(light));
            }

            // Stitch the chunk lighting seams by grabbing the edges of adjacent chunks and continuing their light into this chunk
            Chunk newChunk = null;
            Chunk chunkPrevious = chunk;
            Rect bounds = new Rect(0, 0, GenerationManager.Instance.chunkSize, GenerationManager.Instance.chunkSize);
            for (int i = -1; i < GenerationManager.Instance.chunkSize + 1; i++)
            {
                for (int j = -1; j < GenerationManager.Instance.chunkSize + 1; j++)
                {
                    // Any coordinates inside the current chunk are invalid, only want the edge ones outside of it
                    if (bounds.Contains(new Vector3Int(i, j, 0)))
                        continue;

                    Vector3Int nodePosition = new Vector3Int(chunk.Position.x + i, chunk.Position.y + j, 0);

                    // Get the appropriate chunk for this position
                    Vector3Int newChunkPosition = ChunkData.GetChunkPosition(nodePosition);
                    if (newChunkPosition != chunkPrevious.chunkPosition)
                    {
                        newChunk = GetChunk(nodePosition);
                        if (newChunk == null)
                            continue;
                        chunkPrevious = newChunk;
                    }

                    // Queue that tile
                    LightNode lightNode;
                    lightNode.position = nodePosition;
                    lightNode.color = chunkPrevious.GetChunkTileColor(nodePosition);
                    lightNode.chunk = chunkPrevious;
                    seamQueue.Enqueue(lightNode);
                }
            }
            // Spread the light for all queued seam tiles
            yield return StartCoroutine(LightingManager.Instance.PerformLightPasses(seamQueue));
        }
    }

    /// <summary>
    /// Unloads chunks that are outside the view, when not loading chunks.
    /// </summary>
    /// <returns></returns>
    private void PerformUnloadChunks()
    {
        foreach (Chunk chunk in GetAllChunks(GetChunkLoadBounds(), true))
            if (chunk != null)
                chunk.UnloadChunk();
    }
}