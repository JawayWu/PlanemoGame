using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Furnace : MonoBehaviour {
    [HideInInspector]
    public Item inputItem, outputItem;
    [HideInInspector]
    public Image inputImage, outputImage;
    [HideInInspector]
    public Vector3Int position;
    [HideInInspector]
    public Chunk.TilemapType tt;

    private float progress = 0;

    public static Inventory inv;

    #region instantiation
    public static Furnace prefab;
    public static Furnace Create(Vector3Int position, Chunk.TilemapType tt) {
        Furnace furnace = Instantiate(prefab);

        furnace.position = position;
        furnace.tt = tt;

        return furnace;
    }
    #endregion

    #region Unity_functions
    public void Awake() {
        prefab = Resources.Load<Furnace>("Prefabs/Furnace");

        inv = FindObjectOfType<Inventory>();
    }

    public void Update() {
        if (inputItem) {
            int outputID = ItemData.GetSmeltID(ItemData.GetTileType(inputItem.id));

            //Invalid input or output item is blocking
            if (outputID == 0
                || (outputItem && outputItem.id != outputID)) {
                progress = 0;
                return;
            }

            progress += Time.deltaTime;
            float totalTime = 1; // Time to complete smelting (in sec)
            if (progress > totalTime) {
                progress = 0;

                //If output is empty
                if (!outputItem) {
                    outputItem = Item.Create(outputID);
                    outputImage = inv.NewImage(outputItem);
                }

                outputItem.quantity++;

                inputItem.quantity--;
                if (inputItem.quantity == 0) DeleteInput();
            }
        }
    }
    #endregion

    #region display_functions
    public void Display() {
        Vector3 scale = new Vector3(8, 8, 1);

        //display input
        if (inputItem) {
            Vector3 pos = new Vector3(56, 6);
            inputItem.Display(pos, scale, inputImage);
        } else if (inputImage) {
            Destroy(inputImage.gameObject);
        }

        //display output
        if (outputItem) {
            Vector3 pos = new Vector3(76, 6);
            outputItem.Display(pos, scale, outputImage);
        } else if (outputImage) {
            Destroy(outputImage.gameObject);
        }
    }

    public void Hide() {
        if (inputItem) {
            inputImage.enabled = false;
            inputImage.GetComponentInChildren<Text>().enabled = false;
        }
        if (outputItem) {
            outputImage.enabled = false;
            outputImage.GetComponentInChildren<Text>().enabled = false;
        }
    }
    #endregion

    #region management_functions
    public void DeleteInput() {
        if (inputItem != null) Destroy(inputItem.gameObject);
        if (inputImage != null) Destroy(inputImage.gameObject);
    }

    public void DeleteOutput() {
        if (outputItem != null) Destroy(outputItem.gameObject);
        if (outputImage != null) Destroy(outputImage.gameObject);
    }

    //Drop all items
    public void Break() {
        if (inputItem) {
            Item.Spawn(ItemData.GetTileType(inputItem.id), position + new Vector3(0.5f, 0.5f), inputItem.quantity);
            DeleteInput();
        }
        if (outputItem) {
            Item.Spawn(ItemData.GetTileType(outputItem.id), position + new Vector3(0.5f, 0.5f), outputItem.quantity);
            DeleteOutput();
        }
    }
    #endregion
}
