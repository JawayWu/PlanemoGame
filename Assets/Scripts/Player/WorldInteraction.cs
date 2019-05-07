using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldInteraction : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Player target")]
    private Targeting playerTarget;

    private PlayerMovement pm;

    private Chunk chunk;
    private Chunk.TilemapType tilemap;
    private Vector3Int target;

    private float duration, maxDuration = 5;

    private const Chunk.TilemapType FB = Chunk.TilemapType.FRONT_BLOCKS,
                                    BB = Chunk.TilemapType.BACK_BLOCKS;

    #region Unity_functions
    public void Start()
    {
        pm = GetComponent<PlayerMovement>();
    }

    public void Update()
    {
        //Can't interact with world while looking at inventory
        if (pm.inInventory) return;

        if (pm.selected != 10) duration = 0;
        playerTarget.UpdateBreaking(duration, maxDuration);

        //Breaking blocks
        if (pm.selected == 10)
        {
            if (Input.GetMouseButton(0))
            {
                //Check if player switched targets
                if (chunk != playerTarget.chunk
                    || tilemap != playerTarget.tilemap
                    || target != playerTarget.target)
                {
                    chunk = playerTarget.chunk;
                    tilemap = playerTarget.tilemap;
                    target = playerTarget.target;
                    duration = 0;
                }
                else
                {
                    if (chunk)
                    {
                        //increase breaking duration
                        duration += Time.deltaTime;

                        //Grab the type of the block we are going to break.
                        Chunk.TileType typeOfBlock = chunk.GetChunkTileType(target, tilemap);
                        //Can't break bedrock or air
                        if (typeOfBlock == Chunk.TileType.BEDROCK || typeOfBlock == Chunk.TileType.AIR)
                        {
                            duration = 0;
                            return;
                        }

                        maxDuration = ItemData.GetBreakDuration(ItemData.GetID(typeOfBlock));
                        playerTarget.UpdateBreaking(duration, maxDuration);

                        //once duration hits certain time, break block and spawn an item
                        if (duration > maxDuration)
                        {
                            //Break entire tree
                            Vector3Int treeCheck = new Vector3Int(target.x, target.y, 0);
                            if (chunk.GetChunkTileType(treeCheck, tilemap) == Chunk.TileType.LOG)
                            {
                                Chunk currChunk = chunk;
                                while (currChunk && currChunk.GetChunkTileType(treeCheck, tilemap) == Chunk.TileType.LOG)
                                {
                                    Vector3Int[] neighbors = new Vector3Int[] {
                                        new Vector3Int(-1, 0, 0),   //left
                                        new Vector3Int(0, 0, 0),    //current
                                        new Vector3Int(1, 0, 0),    //right
                                        new Vector3Int(0, 1, 0)     //up
                                    };
                                    //Check for neighboring leaves
                                    foreach (Vector3Int v in neighbors)
                                    {
                                        Vector3Int newTgt = treeCheck + v;

                                        Chunk targetChunk = GetChunk(newTgt);

                                        Chunk.TileType frontType = targetChunk.GetChunkTileType(newTgt, FB);
                                        if (frontType == Chunk.TileType.LEAF
                                            || frontType == Chunk.TileType.LEAFAPPLE
                                            || frontType == Chunk.TileType.LEAFORANGE)
                                        {
                                            //Break the block
                                            targetChunk.SetChunkTile(newTgt, FB, null);
                                            targetChunk.SetChunkTileType(newTgt, FB, Chunk.TileType.AIR);
                                            //Spawn stick w/ probability 1/3
                                            if (Random.Range(0, 3) == 0)
                                            {
                                                Item.Spawn(frontType, newTgt, 1);
                                            }
                                        }
                                        Chunk.TileType backType = targetChunk.GetChunkTileType(newTgt, BB);
                                        if (backType == Chunk.TileType.LEAF
                                            || backType == Chunk.TileType.LEAFAPPLE
                                            || backType == Chunk.TileType.LEAFORANGE)
                                        {
                                            //Break the block
                                            targetChunk.SetChunkTile(newTgt, BB, null);
                                            targetChunk.SetChunkTileType(newTgt, BB, Chunk.TileType.AIR);
                                            //Spawn stick w/ probability 1/3
                                            if (Random.Range(0, 3) == 0)
                                            {
                                                Item.Spawn(backType, newTgt, 1);
                                            }
                                        }
                                    }

                                    //Break the block
                                    currChunk.SetChunkTile(treeCheck, tilemap, null);
                                    currChunk.SetChunkTileType(treeCheck, tilemap, Chunk.TileType.AIR);
                                    //Spawn log
                                    Item.Spawn(Chunk.TileType.LOG, treeCheck, 1);

                                    treeCheck.y += 1;

                                    currChunk = GetChunk(treeCheck);
                                }

                                goto Reset; //skip over default block breaking
                            }

                            //Check if chest
                            if (typeOfBlock == Chunk.TileType.CHEST)
                            {
                                ItemStorage.GetChest(target, tilemap).Break();
                                ItemStorage.RemoveChest(target, tilemap);
                            }

                            //Check if furnace
                            if (typeOfBlock == Chunk.TileType.FURNACE)
                            {
                                ItemStorage.GetFurnace(target, tilemap).Break();
                                ItemStorage.RemoveFurnace(target, tilemap);
                            }

                            //Break the block
                            chunk.SetChunkTile(target, tilemap, null);
                            chunk.SetChunkTileType(target, tilemap, Chunk.TileType.AIR);
                            if (ItemData.IsLightSource(ItemData.GetID(typeOfBlock)))
                            {
                                LightSource lightSource = LightingManager.Instance.GetLightSource(target);
                                /* Only target lights that were placed by the player. Ambient lights can only be removed
                                 * by placing a block on top of them and is handled in PlaceTile();. */
                                if (lightSource != null && lightSource.lightColor != LightingManager.Instance.ambientLightColor)
                                    LightingManager.Instance.RemoveLightSource(lightSource);
                            }
                            else
                            {
                                // If the back layer is now air, it means we need to create an ambient light source there
                                if (chunk.GetChunkTileType(target, Chunk.TilemapType.BACK_BLOCKS) == Chunk.TileType.AIR
                                    && target.y >= GenerationManager.Instance.surfaceHeights[ChunkData.Global(new Vector3Int(target.x, 0, 0)).x] - 12)
                                {
                                    if (!LightingManager.Instance.GetLightSource(target))
                                    {
                                        LightingManager.Instance.CreateLightSource(target, LightingManager.Instance.ambientLightColor,
                                            LightingManager.Instance.ambientLightStrength);
                                    }
                                }
                                /* brighten the scene by simply simulating a new light with
                                    * a slightly brighter color. difference is the
                                    * difference between the front and back layer light falloff values. */
                                else
                                {
                                    Color currentColor = chunk.GetChunkTileColor(target);
                                    if (currentColor != Color.clear && currentColor != Color.black)
                                    {
                                        float colorIncrement = LightingManager.Instance.LightFalloff - LightingManager.Instance.LightFalloffBack;
                                        currentColor = new Color(
                                            Mathf.Clamp(currentColor.r + colorIncrement, 0f, 1f),
                                            Mathf.Clamp(currentColor.g + colorIncrement, 0f, 1f),
                                            Mathf.Clamp(currentColor.b + colorIncrement, 0f, 1f));
                                        chunk.SetChunkTileColor(target, currentColor);

                                        // Simulate light and remove just the light source object afterwards. Leave its light on the blocks
                                        LightSource source = LightingManager.Instance.CreateLightSource(target, currentColor,
                                            LightingManager.Instance.ambientLightStrength);
                                        LightingManager.Instance.RemoveLightSource(source, false);
                                    }
                                }
                            }
                            //Spawn an item in the position of the broken block.
                            Item.Spawn(typeOfBlock, target, 1);

                        Reset: duration = 0;
                            chunk = null;
                        }
                    }
                }
            }
            else
            {
                duration = 0;
            }
        }
        else
        {
            Chunk ch = playerTarget.chunk;
            Vector3Int tgt = playerTarget.target;
            Vector3 targetCenter = new Vector3(tgt.x + 0.5f, tgt.y + 0.5f, tgt.z);

            if (ch == null) return;

            //Open crafting UI
            if (Input.GetMouseButtonDown(1) && !pm.inInventory
                && (ch.GetChunkTileType(tgt, FB) == Chunk.TileType.WORKBENCH
                || (ch.GetChunkTileType(tgt, BB) == Chunk.TileType.WORKBENCH && ch.GetChunkTileType(tgt, FB) == Chunk.TileType.AIR)))
            {
                pm.inInventory = !pm.inInventory;
                pm.inventory.mode = "crafting";
                return;
            }

            //Open chest
            if (Input.GetMouseButtonDown(1) && !pm.inInventory)
            {
                if (ch.GetChunkTileType(tgt, FB) == Chunk.TileType.CHEST)
                {
                    pm.inInventory = !pm.inInventory;
                    pm.inventory.mode = "chest";
                    pm.chest = ItemStorage.GetChest(tgt, FB);
                    return;
                }
                else if (ch.GetChunkTileType(tgt, BB) == Chunk.TileType.CHEST && ch.GetChunkTileType(tgt, FB) == Chunk.TileType.AIR)
                {
                    pm.inInventory = !pm.inInventory;
                    pm.inventory.mode = "chest";
                    pm.chest = ItemStorage.GetChest(tgt, BB);
                    return;
                }
            }

            //Open furnace
            if (Input.GetMouseButtonDown(1) && !pm.inInventory)
            {
                if (ch.GetChunkTileType(tgt, FB) == Chunk.TileType.FURNACE)
                {
                    pm.inInventory = !pm.inInventory;
                    pm.inventory.mode = "furnace";
                    pm.furnace = ItemStorage.GetFurnace(tgt, FB);
                    return;
                }
                else if (ch.GetChunkTileType(tgt, BB) == Chunk.TileType.FURNACE && ch.GetChunkTileType(tgt, FB) == Chunk.TileType.AIR)
                {
                    pm.inInventory = !pm.inInventory;
                    pm.inventory.mode = "furnace";
                    pm.furnace = ItemStorage.GetFurnace(tgt, BB);
                    return;
                }
            }

            //Placing blocks
            if (Input.GetMouseButton(0))
            {
                //Check that selected item is placeable
                Item item = pm.inventory.GetItem(3, pm.selected);
                if (item && item.tile)
                {
                    //Make sure we're placing in an empty space
                    if (ch.GetChunkTileType(tgt, FB) != Chunk.TileType.AIR) return;

                    //RayCasts to check if we are touching another adjacent Sblock
                    RaycastHit2D hitUpperAdjacent = Physics2D.Raycast(targetCenter, new Vector2(0, 1), 1.0f, LayerMask.GetMask("Default"));
                    RaycastHit2D hitLowerAdjacent = Physics2D.Raycast(targetCenter, new Vector2(0, -1), 1.0f, LayerMask.GetMask("Default"));
                    RaycastHit2D hitLeftAdjactent = Physics2D.Raycast(targetCenter, new Vector2(-1, 0), 1.0f, LayerMask.GetMask("Default"));
                    RaycastHit2D hitRightAdjacent = Physics2D.Raycast(targetCenter, new Vector2(1, 0), 1.0f, LayerMask.GetMask("Default"));

                    //Check to make sure our target doesn't hit the player.
                    float widthBound = pm.bc.size.x / 2.0f;
                    float heightBound = pm.bc.size.y / 2.0f;

                    bool onPlayerTarget = pm.transform.position.x - (1 + widthBound) < tgt.x &&
                                            tgt.x < pm.transform.position.x + widthBound &&
                                            pm.transform.position.y - (1 + heightBound) < tgt.y &&
                                            tgt.y < pm.transform.position.y + heightBound;
                    if (!onPlayerTarget)
                    {
                        if (ch != null && playerTarget.tilemap == BB)
                        {
                            //Check to see if there are adjacent blocks or there is a block beneath.
                            if (ch.GetChunkTileType(tgt, BB) != Chunk.TileType.AIR
                                || hitUpperAdjacent
                                || hitLowerAdjacent
                                || hitLeftAdjactent
                                || hitRightAdjacent)
                            {
                                if (ItemData.IsLightSource(item.id))
                                {
                                    item.Place(ch, tgt, FB);
                                    Color lightColor = new Color(0.99f, 0.99f, 0.99f);
                                    LightSource newSource = LightingManager.Instance.CreateLightSource(tgt, lightColor, 1f);
                                    chunk.PlacedLightSources.Add(newSource);
                                }
                                else
                                {
                                    Color currentColor = ch.GetChunkTileColor(tgt);
                                    ch.SetChunkTileColor(tgt, Color.black);
                                    item.Place(ch, tgt, FB);
                                    // Mark the air blocks around it as light sources if valid
                                    for (float i = 0; i < Mathf.PI * 2f; i += Mathf.PI / 2f)
                                    {
                                        Vector3Int checkPosition = new Vector3Int(
                                            tgt.x + Mathf.RoundToInt(Mathf.Sin(i)),
                                            tgt.y + Mathf.RoundToInt(Mathf.Cos(i)), 0);

                                        if (ChunkLoadManager.Instance.IsAirBlock(checkPosition) && !LightingManager.Instance.GetLightSource(checkPosition))
                                            LightingManager.Instance.CreateLightSource(checkPosition, LightingManager.Instance.ambientLightColor, LightingManager.Instance.ambientLightStrength);
                                    }

                                    /* We want to darken the surroundings now that we placed a block. Simulate this by placing a light and
                                     * instead of updating it, simply remove the surrounding light using the current color. */
                                    LightSource existingLight = LightingManager.Instance.GetLightSource(tgt);
                                    if (existingLight == null)
                                        existingLight = LightingManager.Instance.CreateLightSource(tgt, currentColor,
                                            LightingManager.Instance.ambientLightStrength, false);
                                    LightingManager.Instance.RemoveLightSource(existingLight);
                                }
                            }
                        }
                    }
                }
            }
            if (Input.GetMouseButton(1))
            {
                Vector3Int blockAbovePosition = new Vector3Int(tgt.x, tgt.y + 1, tgt.z);
                Vector3Int blockLeftPosition = new Vector3Int(tgt.x - 1, tgt.y, tgt.z);
                Vector3Int blockRightPosition = new Vector3Int(tgt.x + 1, tgt.y, tgt.z);
                Vector3Int blockBelowPosition = new Vector3Int(tgt.x, tgt.y - 1, tgt.z);

                Item item = pm.inventory.GetItem(3, pm.selected);
                if (item && item.tile)
                {
                    if (ch.GetChunkTileType(tgt, BB) != Chunk.TileType.AIR) return;

                    Chunk.TileType blockAbove = ChunkData.GetTileType(blockAbovePosition, BB);
                    Chunk.TileType blockBelow = ChunkData.GetTileType(blockBelowPosition, BB);
                    Chunk.TileType blockLeft = ChunkData.GetTileType(blockLeftPosition, BB);
                    Chunk.TileType blockRight = ChunkData.GetTileType(blockRightPosition, BB);
                    Chunk.TileType blockFront = ch.GetChunkTileType(tgt, FB);

                    if (blockAbove != Chunk.TileType.AIR ||
                        blockLeft != Chunk.TileType.AIR ||
                        blockRight != Chunk.TileType.AIR ||
                        blockBelow != Chunk.TileType.AIR ||
                        blockFront != Chunk.TileType.AIR)
                    {
                        if (ItemData.IsLightSource(item.id))
                        {
                            item.Place(ch, tgt, FB);
                            Color lightColor = new Color(0.99f, 0.99f, 0.99f);
                            LightSource newSource = LightingManager.Instance.CreateLightSource(tgt, lightColor, 1f);
                            chunk.PlacedLightSources.Add(newSource);
                        }
                        else
                        {
                            Color currentColor = ch.GetChunkTileColor(tgt);
                            ch.SetChunkTileColor(tgt, Color.black);
                            item.Place(ch, tgt, BB);

                            // Mark the air blocks around it as light sources if valid
                            for (float i = 0; i < Mathf.PI * 2f; i += Mathf.PI / 2f)
                            {
                                Vector3Int checkPosition = new Vector3Int(
                                    tgt.x + Mathf.RoundToInt(Mathf.Sin(i)),
                                    tgt.y + Mathf.RoundToInt(Mathf.Cos(i)), 0);

                                if (ChunkLoadManager.Instance.IsAirBlock(checkPosition) && !LightingManager.Instance.GetLightSource(checkPosition))
                                    LightingManager.Instance.CreateLightSource(checkPosition, LightingManager.Instance.ambientLightColor, LightingManager.Instance.ambientLightStrength);
                            }

                            /* We want to darken the surroundings now that we placed a block. Simulate this by placing a light and
                             * instead of updating it, simply remove the surrounding light using the current color. */
                            LightSource existingLight = LightingManager.Instance.GetLightSource(tgt);
                            //Debug.Log(existingLight);
                            if (existingLight == null)
                                existingLight = LightingManager.Instance.CreateLightSource(tgt, currentColor,
                                    LightingManager.Instance.ambientLightStrength, false);
                            LightingManager.Instance.RemoveLightSource(existingLight);
                        }
                    }
                }
            }
        }
    }
    #endregion

    //copied from ChunkLoadManager for convenience
    public Chunk GetChunk(Vector3Int position)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            new Vector2(position.x + 0.5f, position.y + 0.5f),
            Vector2.zero, 0f, LayerMask.GetMask("Chunk"));
        return hit ? hit.collider.GetComponent<Chunk>() : null;
    }
}
