using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles all lighting effects.
/// </summary>
public class LightingManager : Singleton<LightingManager>
{
    [Header("Block Lighting")]
    public SliderData lightingData;
    public GameObject lightSourceRoot;
    public GameObject lightSourcePrefab;
    [HideInInspector]
    public int iterationLimit;
    public LayerMask lightSourceLayer;
    public Color ambientLightColor = Color.white;
    [Range(0f, 1f)]
    public float ambientLightStrength = 1f;
    public float LightPenetration { get; private set; }
    public float LightPenetrationBack { get; private set; }
    public float BackLayerShadowFactor { get; private set; }
    public bool LightingIsActive { get; set; }
    public float LightFalloff { get; set; }
    public float LightFalloffBack { get; set; }
    public enum LightingChannelMode
    {
        RED,
        GREEN,
        BLUE
    }

    private int iterationCounter = 0;
    private float lightPassThreshold;
    private Queue<LightNode> queueUpdate, queueRemoval;
    private List<Vector3Int> removalPositions;
    private List<LightSource> removalLights;
    private int worldWidth, worldHeight;
    private Vector3Int vectorRight, vectorUp, vectorLeft, vectorDown;
    private Chunk chunkDirection;
    private Vector3Int newChunkPosition;
    private Color currentColor;
    private float lightValueDirection, blockFalloff;
    private Chunk.TileType typeBack, typeFront;


    private void Start()
    {
        LightingIsActive = true;

        queueUpdate = new Queue<LightNode>();
        queueRemoval = new Queue<LightNode>();
        removalPositions = new List<Vector3Int>();
        removalLights = new List<LightSource>();
        vectorRight = Vector3Int.right;
        vectorUp = Vector3Int.up;
        vectorLeft = Vector3Int.left;
        vectorDown = Vector3Int.down;
        worldWidth = GenerationManager.Instance.worldWidth;
        worldHeight = GenerationManager.Instance.worldHeight;

        UpdateLightPenetration();
    }


    /// <summary>
    /// Updates light penetration values. Called whenever a lighting slider value changes.
    /// </summary>
    public void UpdateLightPenetration()
    {
        LightPenetration = lightingData.GetSliderData(SliderData.SliderField.LIGHTING_PENETRATION);
        LightPenetrationBack = lightingData.GetSliderData(SliderData.SliderField.LIGHTING_PENETRATION_BACK);
        BackLayerShadowFactor = lightingData.GetSliderData(SliderData.SliderField.LIGHTING_BACK_SHADOW);
        iterationLimit = (int)lightingData.GetSliderData(SliderData.SliderField.LIGHTING_ITERATION);

        LightFalloff = 1.0f / LightPenetration;
        LightFalloffBack = 1.0f / LightPenetrationBack;
        lightPassThreshold = LightFalloffBack;
    }


    /// <summary>
    /// Creates a light source with the given color and light strength.
    /// Optionally able to turn off automatic light update after creation.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="color"></param>
    /// <param name="strength"></param>
    /// <param name="updateLight"></param>
    /// <returns></returns>
    public LightSource CreateLightSource(Vector3Int position, Color color,
        float strength = 1f, bool updateLight = true)
    {
        LightSource lightSource = Instantiate(lightSourcePrefab, position,
            Quaternion.identity, lightSourceRoot.transform).GetComponent<LightSource>();
        lightSource.InitializeLight(color, strength);

        if (updateLight)
            UpdateLight(lightSource);

        return lightSource;
    }
    public LightSource CreateAmbientSource(Vector3Int position, Color color,
       float strength = 1f, bool updateLight = true)
    {
        LightSource lightSource = Instantiate(lightSourcePrefab, position,
            Quaternion.identity, lightSourceRoot.transform).GetComponent<LightSource>();
        lightSource.InitializeLight(color, strength);

        if (updateLight)
            UpdateLightSmooth(lightSource);

        return lightSource;
    }


    /// <summary>
    /// Removes the given LightSource object.
    /// Removes its light first.
    /// </summary>
    /// <param name="light"></param>
    /// <param name="removeLight"></param>
    public void RemoveLightSource(LightSource light, bool removeLight = true)
    {
        if (light == null)
            return;

        if (removeLight)
            RemoveLight(light);

        Destroy(light.gameObject);
    }


    /// <summary>
    /// Creates all ambient LightSources for the given chunk.
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="updateLights"></param>
    /// <returns></returns>
    public List<LightSource> CreateAmbientLightSources(Chunk chunk, bool updateLights = true)
    {
        Chunk positionChunk;
        Rect rectPositions = new Rect();
        Rect rectChunk = new Rect(
            new Vector2(chunk.Position.x - 1, chunk.Position.y - 1),
            new Vector2(GenerationManager.Instance.chunkSize + 1, GenerationManager.Instance.chunkSize + 1));
        Vector3Int tilePosition, tilePositionNext, rectPosition;
        List<LightSource> chunkLights = new List<LightSource>();

        // Start and end one block outside the chunk to fix light source gaps inbetween chunks
        for (int h = -1; h < GenerationManager.Instance.chunkSize; h++)
        {
            int surfacePosition = ChunkData.Global(new Vector3Int(chunk.Position.x + h, 0, 0)).x;
            int surfacePositionNext = ChunkData.Global(new Vector3Int(chunk.Position.x + h + 1, 0, 0)).x;

            // Make a rect from the current surface level position to the next
            tilePosition = new Vector3Int(chunk.Position.x + h, GenerationManager.Instance.surfaceHeights[surfacePosition] + 1, 0);
            tilePositionNext = new Vector3Int(chunk.Position.x + h + 1, GenerationManager.Instance.surfaceHeights[surfacePositionNext] + 1, 0);
            rectPositions.position = new Vector2(tilePosition.x, tilePosition.y);
            rectPositions.size = new Vector2(1f, tilePositionNext.y - tilePosition.y);

            // Check if the chunk rect contains this rect to see if the surface height is within this chunk
            if (!rectChunk.Contains(new Vector2(rectPositions.xMin, rectPositions.yMax), true) &&
                !rectChunk.Contains(new Vector2(rectPositions.xMax, rectPositions.yMin), true))
                continue;

            /* If any tiles within rectPositions are valid for an ambient light source, create one on that position.
             * Compensate for inverse Rect bounds first, since the Rect.*min and Rect.*max don't account for this. */
            float minimumX = Mathf.Min(rectPositions.xMin, rectPositions.xMax);
            float maximumX = Mathf.Max(rectPositions.xMin, rectPositions.xMax);
            float minimumY = Mathf.Min(rectPositions.yMin, rectPositions.yMax);
            float maximumY = Mathf.Max(rectPositions.yMin, rectPositions.yMax);
            for (float rectX = minimumX; rectX <= maximumX; rectX++)
            {
                for (float rectY = minimumY; rectY <= maximumY; rectY++)
                {
                    rectPosition = new Vector3Int((int)rectX, (int)rectY, 0);
                    positionChunk = ChunkLoadManager.Instance.GetChunk(rectPosition);

                    if (ChunkLoadManager.Instance.IsAirBlock(rectPosition, positionChunk) && !GetLightSource(rectPosition))
                        if (ChunkLoadManager.Instance.HasAdjacentTiles(rectPosition, positionChunk))
                            chunkLights.Add(CreateLightSource(rectPosition, ambientLightColor, ambientLightStrength, updateLights));
                }
            }
        }
        return chunkLights;
    }


    /// <summary>
    /// Returns the LightSource at the given position or null if no LightSource was found.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public LightSource GetLightSource(Vector3Int position)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            new Vector2(position.x + 0.5f, position.y + 0.5f),
            Vector2.zero, 0f, lightSourceLayer);
        return hit ? hit.collider.GetComponent<LightSource>() : null;
    }


    /// <summary>
    /// Returns the LightSources that are inside or overlap with the given Rect bounds.
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    public List<LightSource> GetLightSources(Rect rect)
    {
        List<LightSource> lights = new List<LightSource>();
        Collider2D[] lightColliders = Physics2D.OverlapBoxAll(rect.center, rect.size / 2f,
            0f, lightSourceLayer);

        foreach (Collider2D coll in lightColliders)
        {
            LightSource light = coll.GetComponent<LightSource>();
            if (light != null)
                lights.Add(light);
        }
        return lights;
    }
    public void UpdateLightTime(LightSource light, float lightTime)
    {
        // Create a struct as lightweight data storage per visited block
        LightNode lightNode;
        lightNode.position = light.Position;
        lightNode.color = (light.lightColor * light.LightStrength) * lightTime;
        lightNode.chunk = ChunkLoadManager.Instance.GetChunk(light.Position);
        if (lightNode.chunk == null)
            return;

        // Set the color of the light source's own tile
        Color currentColor = lightNode.chunk.GetChunkTileColor(light.Position);
        lightNode.chunk.SetChunkTileColor(light.Position, new Color(
            Mathf.Max(currentColor.r, (light.lightColor.r * light.LightStrength) * lightTime),
            Mathf.Max(currentColor.g, (light.lightColor.g * light.LightStrength) * lightTime),
            Mathf.Max(currentColor.b, (light.lightColor.b * light.LightStrength) * lightTime)));

        removalPositions.Clear();
        queueUpdate.Clear();
        queueUpdate.Enqueue(lightNode);
        StartCoroutine(PerformLightPasses(queueUpdate, true, true, true, false));
    }

    /// <summary>
    /// Updates the given LightSource by trying to spread its light to surrounding tiles.
    /// Works on an overwrite basis. If the surrounding light is stronger, it stops.
    /// Done in a single frame and useful for things like placing torches for example
    /// where you want the light to be there instantly. for smoothing, look at
    /// the UpdateLightSmooth() coroutine instead.
    /// </summary>
    /// <param name="light"></param>
    public void UpdateLight(LightSource light, float lightTime = 1)
    {
        // Create a struct as lightweight data storage per visited block
        LightNode lightNode;
        lightNode.position = light.Position;
        lightNode.color = light.lightColor * light.LightStrength * lightTime;
        lightNode.chunk = ChunkLoadManager.Instance.GetChunk(light.Position);
        if (lightNode.chunk == null)
            return;

        // Set the color of the light source's own tile
        Color currentColor = lightNode.chunk.GetChunkTileColor(light.Position);
        lightNode.chunk.SetChunkTileColor(light.Position, new Color(
            Mathf.Max(currentColor.r, light.lightColor.r * light.LightStrength * lightTime),
            Mathf.Max(currentColor.g, light.lightColor.g * light.LightStrength * lightTime),
            Mathf.Max(currentColor.b, light.lightColor.b * light.LightStrength * lightTime)));

        removalPositions.Clear();
        queueUpdate.Clear();
        queueUpdate.Enqueue(lightNode);
        StartCoroutine(PerformLightPasses(queueUpdate, true, true, true, false));
    }


    /// <summary>
    /// Updates the given LightSource by trying to spread its light to surrounding tiles.
    /// Works exactly the same as UpdateLight(), but it'll space out calculations over multiple frames
    /// whenever the iteration limit is reached. This works on an overwrite basis. If the surrounding 
    /// light is stronger, it stops.
    /// </summary>
    /// <param name="light"></param>
    /// <returns></returns>
    public IEnumerator UpdateLightSmooth(LightSource light)
    {
        // Create a struct as lightweight data storage per block
        LightNode lightNode;
        lightNode.position = light.Position;
        lightNode.color = light.lightColor * light.LightStrength;
        lightNode.chunk = ChunkLoadManager.Instance.GetChunk(light.Position);
        if (lightNode.chunk == null)
            yield break;

        // Set the color of the light source's own tile
        Color currentColor = lightNode.chunk.GetChunkTileColor(light.Position);
        lightNode.chunk.SetChunkTileColor(light.Position, new Color(
            Mathf.Max(currentColor.r, light.lightColor.r * light.LightStrength),
            Mathf.Max(currentColor.g, light.lightColor.g * light.LightStrength),
            Mathf.Max(currentColor.b, light.lightColor.b * light.LightStrength)));

        removalPositions.Clear();
        queueUpdate.Clear();
        queueUpdate.Enqueue(lightNode);
        yield return StartCoroutine(PerformLightPasses(queueUpdate));
    }


    /// <summary>
    /// Removes all light of the given LightSource.
    /// This does not delete its gameObject. Use RemoveLightSource() instead, since that combines both.
    /// </summary>
    /// <param name="light"></param>
    public void RemoveLight(LightSource light)
    {
        // Create a struct as lightweight data storage per block
        LightNode lightNode;
        lightNode.position = light.Position;
        lightNode.chunk = ChunkLoadManager.Instance.GetChunk(light.Position);
        if (lightNode.chunk == null)
            return;

        /* If there are many LightSources close together, the color of the LightSource is drowned out.
         * To correctly remove this enough, instead of using the light's color to remove, use the color 
         * of the LightSource's tile if that color is greater. */
        Color currentColor = lightNode.chunk.GetChunkTileColor(light.Position);
        lightNode.color = new Color(
            Mathf.Max(currentColor.r, light.lightColor.r * light.LightStrength),
            Mathf.Max(currentColor.g, light.lightColor.g * light.LightStrength),
            Mathf.Max(currentColor.b, light.lightColor.b * light.LightStrength));

        lightNode.chunk.SetChunkTileColor(light.Position, Color.black);
        queueRemoval.Clear();
        queueRemoval.Enqueue(lightNode);
        PerformLightRemovalPasses(queueRemoval);

        /* If we touched LightSources during removal spreading, we completely drowned out their color.
         * In order to correctly fill in the void, we need to update these lights. */
        foreach (LightSource lightSource in removalLights)
            if (lightSource != light)
                UpdateLight(lightSource);
    }


    /// <summary>
    /// Generates colored lighting for every queued node.
    /// Makes use of Breadth-First Search using a FIFO Queue. This way, all blocks are only visited once.
    /// Executes passes per light channel (RGB) to ensure correct blending.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="redChannel"></param>
    /// <param name="greenChannel"></param>
    /// <param name="blueChannel"></param>
    /// <param name="smoothing"></param>
    /// <returns></returns>
    public IEnumerator PerformLightPasses(Queue<LightNode> queue, bool redChannel = true,
        bool greenChannel = true, bool blueChannel = true, bool smoothing = true)
    {
        if (!redChannel && !greenChannel && !blueChannel)
            yield break;

        /* Generate a backup queue to refill the original queue after each channel
         * (since every channel execution empties the queue). 
         * 
         * In case of being called during light removal, an extra check ensures only 
         * the outer edge nodes of stronger lighted tiles will fill in the removed light
         * if applicable and not tiles within its own light (see ExtendQueueLightRemovalPass)*/
        Queue<LightNode> queueBackup = new Queue<LightNode>();
        foreach (LightNode lightNode in queue)
            if (!removalPositions.Contains(lightNode.position))
                queueBackup.Enqueue(lightNode);
        queue.Clear();

        // Spread light for each channel
        if (redChannel)
        {
            if (queue.Count == 0)
                foreach (LightNode lightNode in queueBackup)
                    queue.Enqueue(lightNode);
            while (queue.Count > 0)
            {
                ExecuteLightingPass(queue, LightingChannelMode.RED);

                if (smoothing)
                {
                    iterationCounter++;
                    if (iterationCounter > iterationLimit)
                    {
                        yield return null;
                        iterationCounter = 0;
                    }
                }
            }
        }
        if (greenChannel)
        {
            if (queue.Count == 0)
                foreach (LightNode lightNode in queueBackup)
                    queue.Enqueue(lightNode);
            while (queue.Count > 0)
            {
                ExecuteLightingPass(queue, LightingChannelMode.GREEN);

                if (smoothing)
                {
                    iterationCounter++;
                    if (iterationCounter > iterationLimit)
                    {
                        yield return null;
                        iterationCounter = 0;
                    }
                }
            }
        }
        if (blueChannel)
        {
            if (queue.Count == 0)
                foreach (LightNode lightNode in queueBackup)
                    queue.Enqueue(lightNode);
            while (queue.Count > 0)
            {
                ExecuteLightingPass(queue, LightingChannelMode.BLUE);

                if (smoothing)
                {
                    iterationCounter++;
                    if (iterationCounter > iterationLimit)
                    {
                        yield return null;
                        iterationCounter = 0;
                    }
                }
            }
        }
    }


    /// <summary>
    /// Removes all queued nodes' light channels.
    /// </summary>
    /// <param name="queue"></param>
    private void PerformLightRemovalPasses(Queue<LightNode> queue)
    {
        removalLights.Clear();

        /* Generate a backup queue to refill the original queue after each channel
         * (since every channel execution empties the queue).*/
        Queue<LightNode> queueBackup = new Queue<LightNode>();
        foreach (LightNode lightNode in queue)
            queueBackup.Enqueue(lightNode);

        removalPositions.Clear();
        queueUpdate.Clear();
        while (queue.Count > 0)
            ExecuteLightingRemovalPass(queue, LightingChannelMode.RED);
        // Fill in empty space if I found stronger (red channel) tiles
        StartCoroutine(PerformLightPasses(queueUpdate, true, false, false, false));

        removalPositions.Clear();
        queueUpdate.Clear();
        foreach (LightNode lightNode in queueBackup)
            queue.Enqueue(lightNode);
        while (queue.Count > 0)
            ExecuteLightingRemovalPass(queue, LightingChannelMode.GREEN);
        // Fill in empty space if I found stronger (green channel) tiles
        StartCoroutine(PerformLightPasses(queueUpdate, false, true, false, false));

        removalPositions.Clear();
        queueUpdate.Clear();
        foreach (LightNode lightNode in queueBackup)
            queue.Enqueue(lightNode);
        while (queue.Count > 0)
            ExecuteLightingRemovalPass(queue, LightingChannelMode.BLUE);
        // Fill in empty space if I found stronger (blue channel) tiles
        StartCoroutine(PerformLightPasses(queueUpdate, false, false, true, false));
    }


    /// <summary>
    /// Spreads the given channel light of all queued nodes in 4 directions.
    /// This is one pass that scans left, down, right and up in that order, so
    /// at most 4 tiles are affected by a single pass per node.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="mode"></param>
    private void ExecuteLightingPass(Queue<LightNode> queue, LightingChannelMode mode)
    {
        // Get the LightNode that's first in line
        LightNode light = queue.Dequeue();
        float lightValue;

        /* Obtain surrounding light values from the corresponding channel to lessen overhead
         * on extension passes. */
        switch (mode)
        {
            case LightingChannelMode.RED:
                if (light.color.r <= 0f)
                    return;
                lightValue = light.color.r;
                break;
            case LightingChannelMode.GREEN:
                if (light.color.g <= 0f)
                    return;
                lightValue = light.color.g;
                break;
            case LightingChannelMode.BLUE:
                if (light.color.b <= 0f)
                    return;
                lightValue = light.color.b;
                break;
            default:
                return;
        }

        // Try and spread its light to surrounding blocks
        ExtendQueueLightPass(queue, light, lightValue, vectorLeft, mode);
        ExtendQueueLightPass(queue, light, lightValue, vectorDown, mode);
        ExtendQueueLightPass(queue, light, lightValue, vectorRight, mode);
        ExtendQueueLightPass(queue, light, lightValue, vectorUp, mode);
    }


    /// <summary>
    /// Try to extend the light in the given direction and add to the queue if succesful.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="light"></param>
    /// <param name="lightValue"></param>
    /// <param name="direction"></param>
    /// <param name="mode"></param>
    private void ExtendQueueLightPass(Queue<LightNode> queue, LightNode light, float lightValue,
        Vector3Int direction, LightingChannelMode mode)
    {
        // Get the right chunk for this position. Only update if we entered a different chunk
        chunkDirection = light.chunk;
        newChunkPosition = ChunkData.GetChunkPosition(light.position + direction);
        if (newChunkPosition != chunkDirection.chunkPosition)
            chunkDirection = ChunkLoadManager.Instance.GetChunk(light.position + direction);
        if (chunkDirection == null)
            return;

        currentColor = chunkDirection.GetChunkTileColor(light.position + direction);
        lightValueDirection =
            (mode == LightingChannelMode.RED ?
                currentColor.r :
                (mode == LightingChannelMode.GREEN ?
                    currentColor.g :
                    currentColor.b));

        typeBack = chunkDirection.GetChunkTileType(light.position + direction, Chunk.TilemapType.BACK_BLOCKS);
        typeFront = chunkDirection.GetChunkTileType(light.position + direction, Chunk.TilemapType.FRONT_BLOCKS);

        if (typeFront != Chunk.TileType.AIR)
            blockFalloff = LightFalloff;
        else if (typeBack != Chunk.TileType.AIR)
            blockFalloff = LightFalloffBack;
        else
            return;


        /* Spread light if the tile's channel color in this direction is lower in lightValue even after compensating
         * its falloff. lightPassThreshold acts as an additional performance boost and defaults to lightFalloffBack. 
         * It basically makes sure that only tiles that are at least two falloffs down are evaluated instead of just
         * one falloff. This extra threshold can be adjusted but can result in ugly lighting artifacts with high values. */
        if (lightValueDirection + blockFalloff + lightPassThreshold < lightValue)
        {
            lightValue = Mathf.Clamp(lightValue - blockFalloff, 0f, 1f);
            Color newColor =
                (mode == LightingChannelMode.RED ?
                    new Color(lightValue, currentColor.g, currentColor.b) :
                (mode == LightingChannelMode.GREEN ?
                    new Color(currentColor.r, lightValue, currentColor.b) :
                    new Color(currentColor.r, currentColor.g, lightValue)));

            LightNode lightNode;
            lightNode.position = light.position + direction;
            lightNode.color = newColor;
            lightNode.chunk = chunkDirection;

            chunkDirection.SetChunkTileColor(light.position + direction, newColor);

            queue.Enqueue(lightNode);
        }
    }


    /// <summary>
    /// Removes the given channel light of all queued nodes in 4 directions.
    /// This is one pass that scans left, down, right and up in that order, so
    /// at most 4 tiles are affected by a single pass.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="mode"></param>
    private void ExecuteLightingRemovalPass(Queue<LightNode> queue, LightingChannelMode mode)
    {
        // Get the LightNode that's first in line
        LightNode light = queue.Dequeue();
        float lightValue;

        /* Detect passing over LightSources, while removing, to update them later. When we touch
         * such a LightSource, it means we completely drowned out its color and we need to
         * update the light again to fill in the blanks correctly. */
        LightSource lightSource = GetLightSource(light.position);
        if (lightSource != null && !removalLights.Contains(lightSource))
            removalLights.Add(lightSource);
        removalPositions.Add(light.position);

        /* Obtain surrounding light values from the corresponding channel to lessen overhead
         * on extension passes. */
        switch (mode)
        {
            case LightingChannelMode.RED:
                if (light.color.r <= 0f)
                    return;
                lightValue = light.color.r;
                break;
            case LightingChannelMode.GREEN:
                if (light.color.g <= 0f)
                    return;
                lightValue = light.color.g;
                break;
            case LightingChannelMode.BLUE:
                if (light.color.b <= 0f)
                    return;
                lightValue = light.color.b;
                break;
            default:
                return;
        }

        // Try and spread the light removal
        ExtendQueueLightRemovalPass(queue, light, lightValue, vectorLeft, mode);
        ExtendQueueLightRemovalPass(queue, light, lightValue, vectorDown, mode);
        ExtendQueueLightRemovalPass(queue, light, lightValue, vectorRight, mode);
        ExtendQueueLightRemovalPass(queue, light, lightValue, vectorUp, mode);
    }


    /// <summary>
    /// Try to extend the removal of light in the given direction.
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="light"></param>
    /// <param name="lightValue"></param>
    /// <param name="direction"></param>
    /// <param name="mode"></param>
    private void ExtendQueueLightRemovalPass(Queue<LightNode> queue, LightNode light, float lightValue,
        Vector3Int direction, LightingChannelMode mode)
    {
        /* Get the right chunk for this position.
         * We don't want to use GetChunk on every check, just the ones where we enter a new chunk. */
        chunkDirection = light.chunk;
        newChunkPosition = ChunkData.GetChunkPosition(light.position + direction);
        if (newChunkPosition != light.chunk.chunkPosition)
            chunkDirection = ChunkLoadManager.Instance.GetChunk(light.position + direction);
        if (chunkDirection == null)
            return;

        currentColor = chunkDirection.GetChunkTileColor(light.position + direction);
        lightValueDirection =
            (mode == LightingChannelMode.RED ?
                currentColor.r :
                (mode == LightingChannelMode.GREEN ?
                    currentColor.g :
                    currentColor.b));

        if (lightValueDirection > 0f)
        {
            // Continue removing and extending while the block I'm looking at has a lower lightValue for this channel
            if (lightValueDirection < lightValue)
            {
                Color newColor =
                    (mode == LightingChannelMode.RED ?
                        new Color(0f, currentColor.g, currentColor.b) :
                        (mode == LightingChannelMode.GREEN ?
                            new Color(currentColor.r, 0f, currentColor.b) :
                            new Color(currentColor.r, currentColor.g, 0f)));

                LightNode lightRemovalNode;
                lightRemovalNode.position = light.position + direction;
                lightRemovalNode.color = currentColor;
                lightRemovalNode.chunk = chunkDirection;

                chunkDirection.SetChunkTileColor(light.position + direction, newColor);

                queue.Enqueue(lightRemovalNode);
            }
            /* ^finds a tile with a higher lightValue for this channel which means another strong light source
             * is nearby. Add tile to the update queue and spread its light after all removal to fill in the blanks 
             * this removal leaves behind.
             *   
             * Because it switches between two different falloff rates, sometimes targets tiles within its own
             * light. Filtered out later before spreading the light (using removalPositions). */
            else
            {
                LightNode lightNode;
                lightNode.position = light.position + direction;
                lightNode.color = currentColor;
                lightNode.chunk = chunkDirection;
                queueUpdate.Enqueue(lightNode);
            }
        }
    }
}
