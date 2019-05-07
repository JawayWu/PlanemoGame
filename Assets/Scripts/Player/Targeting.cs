using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Targeting : MonoBehaviour {
    private SpriteRenderer sr;
    private SpriteRenderer sr_break;
    private LineRenderer lr;

    private PlayerMovement pm;

    [SerializeField]
    [Tooltip("Current player")]
    private GameObject player;

    [SerializeField]
    [Tooltip("Breaking animation")]
    private Sprite[] breaking;

    private const float distance = 10; //Max targeting distance

    #region chunk_variables
    [HideInInspector]
    public Chunk chunk;
    [HideInInspector]
    public Chunk.TilemapType tilemap;
    [HideInInspector]
    public Vector3Int target;

    private const Chunk.TilemapType fb = Chunk.TilemapType.FRONT_BLOCKS,
                                    bb = Chunk.TilemapType.BACK_BLOCKS;
    #endregion

    #region Unity_functions
    public void Start() {
        sr = GetComponent<SpriteRenderer>();
        sr_break = transform.GetChild(0).GetComponent<SpriteRenderer>();
        lr = transform.GetChild(1).GetComponent<LineRenderer>();

        pm = player.GetComponent<PlayerMovement>();
    }

    public void Update() {
        //Convert mouse position to canvas coordinates
        Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        pos.z = 0;

        //Player position
        Vector3 playerPos = pm.armWithWeaponTransform.position;

        Vector3 direction = (pos - playerPos).normalized;  //normal vector from player to mouse
        float distBetween = (pos - playerPos).magnitude;   //distance between player and mouse

        //Find tilemaps using raycast
        RaycastHit2D r = Physics2D.Raycast(playerPos, direction,
            Mathf.Min(distance, distBetween), LayerMask.GetMask("Default"));
        
        //Check that the raycast hit a tilemap
        if (r) {
            //Chunk that the tilemap belongs to
            Chunk ch = r.collider.GetComponentInParent<Chunk>();

            //Store Vector3Int corresponding to tile being broken
            Vector3Int tile = new Vector3Int(Mathf.FloorToInt(r.point.x), Mathf.FloorToInt(r.point.y), 0);
            try {
                if (ch.GetChunkTileType(tile, Chunk.TilemapType.FRONT_BLOCKS) == Chunk.TileType.AIR) {
                    if (r.point.x - tile.x > r.point.y - tile.y) {
                        tile.y--;
                    } else {
                        tile.x--;
                    }
                }
            } catch {
                if (r.point.x - tile.x > r.point.y - tile.y) {
                    tile.y--;
                } else {
                    tile.x--;
                }
            }

            chunk = ch;
            tilemap = fb;
            target = tile;
        } else if (distBetween <= distance) { //Check that mouse is within max distance
            //Attempt to cast to back tiles
            RaycastHit2D r2 = Physics2D.Raycast(pos, Vector2.zero, 0.001f, LayerMask.GetMask("Chunk"));

            //Check for chunk
            if (r2) {
                Chunk ch = r2.collider.GetComponent<Chunk>();

                //Store Vector3Int corresponding to tile being broken
                Vector3Int tile = new Vector3Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), 0);

                chunk = ch;
                tilemap = bb;
                target = tile;
            } else { //Empty space targetted
                ResetTarget();
            }
        } else { //Out of range
            ResetTarget();
        }


        DrawRay();

        //Update sprite
        if (chunk) {
            sr.enabled = true;
            transform.position = target;
        } else {
            sr.enabled = false;
        }
    }
    #endregion

    public void DrawRay() {
        if (pm.selected != 10) {
            lr.enabled = false;
            return;
        }
        
        //Convert mouse position to canvas coordinates
        Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        pos.z = 0;
        //Player position
        Vector3 playerPos = pm.armWithWeaponTransform.position;
        Vector3 direction = (pos - playerPos).normalized;  //normal vector from player to mouse
        float distBetween = (pos - playerPos).magnitude;   //distance between player and mouse

        Color c = new Color(1f, 0, 0, Input.GetMouseButton(0) ? 0.8f : 0.3f);
        lr.endColor = c;

        if (target.z != -1) {
            lr.SetPosition(0, target + new Vector3(0.5f, 0.5f, 0));
            lr.startColor = c;
        } else {
            lr.SetPosition(0, playerPos + direction * Mathf.Min(distBetween, distance));
            lr.startColor = new Color(1f, 0, 0, 0.1f);
        }

        float angle = Quaternion.Angle(pm.armWithWeaponTransform.localRotation, Quaternion.identity) * Mathf.Deg2Rad;
        if (pos.y < playerPos.y) angle *= -1;
        Vector3 arm = new Vector3(Mathf.Cos(angle) * 1.5f * pm.transform.localScale.x,
            Mathf.Sin(angle) * 1.5f * pm.transform.localScale.y);
        lr.SetPosition(1, playerPos + arm);

        lr.enabled = true;
    }

    public void UpdateBreaking(float dur, float maxDur) {
        for (int i = 0; i < breaking.Length; i++) {
            float start = maxDur * (i + 1) / (breaking.Length + 1);
            float end = maxDur * (i + 2) / (breaking.Length + 1);
            if (dur > start && dur <= end) {
                sr_break.sprite = breaking[i];
                sr_break.enabled = true;
                return;
            }
        }
        sr_break.enabled = false;
    }

    private void ResetTarget() {
        target = new Vector3Int(-1, -1, -1);
        tilemap = fb;
        chunk = null;
    }
}
