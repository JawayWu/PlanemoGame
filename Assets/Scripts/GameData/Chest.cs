using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chest : MonoBehaviour {
    private const int ROWS = 2, COLUMNS = 10;

    public Item[,] items = new Item[ROWS, COLUMNS];
    public Image[,] images = new Image[ROWS, COLUMNS];
    public Vector3Int position;
    public Chunk.TilemapType tt;

    public Chest() { }
    public Chest(Vector3Int position, Chunk.TilemapType tt) {
        this.position = position;
        this.tt = tt;
    }

    //Return row and column of chest at given mouse position
    public Tuple<int, int> GetSlot(Vector3 mouse, Vector3 ul, float scale) {
        float w = scale * 10;
        Vector3 right = new Vector3(12 * scale, 0), down = new Vector3(0, -12 * scale);

        for (int r = 0; r < ROWS; r++) {
            for (int c = 0; c < COLUMNS; c++) {
                Vector3 pos = ul + r * down + c * right;
                //Check if mouse is within bounds of inv space
                if (pos.x < mouse.x && mouse.x < pos.x + w
                    && pos.y - w < mouse.y && mouse.y < pos.y) {
                    return new Tuple<int, int>(r, c);
                }
            }
        }

        return null;
    }

    public void Display() {
        Vector3 right = new Vector3(12, 0), down = new Vector3(0, -12);
        Vector3 scale = new Vector3(8, 8, 1);

        Vector3 ul = new Vector3(0, 5) - 2 * down - 4.5f * right;

        for (int r = 0; r < ROWS; r++) {
            for (int c = 0; c < COLUMNS; c++) {
                if (items[r, c]) {
                    //Calculate position in chest
                    Vector3 pos = ul + r * down + c * right;

                    items[r, c].Display(pos, scale, images[r, c]);
                } else if (images[r, c]) {
                    Destroy(images[r, c].gameObject);
                }
            }
        }
    }

    public void Hide() {
        for (int r = 0; r < ROWS; r++) {
            for (int c = 0; c < COLUMNS; c++) {
                if (items[r, c]) {
                    images[r, c].enabled = false;
                    images[r, c].GetComponentInChildren<Text>().enabled = false;
                }
            }
        }
    }

    //Delete an item
    public void Delete(int r, int c) {
        if (items[r, c] != null) Destroy(items[r, c].gameObject);
        if (images[r, c] != null) Destroy(images[r, c].gameObject);
    }

    //Drop all items in the chest
    public void Break() {
        for (int r = 0; r < ROWS; r++) {
            for (int c = 0; c < COLUMNS; c++) {
                if (items[r, c]) {
                    //Drop item
                    Item.Spawn(ItemData.GetTileType(items[r, c].id), position + new Vector3(0.5f, 0.5f), items[r, c].quantity);

                    Delete(r, c);
                }
            }
        }
    }
}
