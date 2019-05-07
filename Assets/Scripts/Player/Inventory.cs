using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour {

    #region textTriggerVariables
    //The text manager for triggering text when items picked up first
    public TextBoxManager textManager;

    public TextAsset woodScript;
    public TextAsset stoneScript;
    public TextAsset benchScript;
    public TextAsset furnaceScript;

    private bool notSeenLog = true;
    private bool notSeenStone = true;
    private bool notSeenBench = true;
    private bool notSeenFurnace = true;

    #endregion textTriggerVariables

    #region taskTrigger Variables
    //The Script to control the task manager
    public TaskManager taskManager;

    public TextAsset workbenchTask;
    public TextAsset furnaceTask;
    #endregion

    public PlayerMovement player;
    public GameObject hotbar;
    public Image baseImage;
    public Sprite
        inventorySprite,
        craftingSprite,
        chestSprite,
        furnaceSprite;

    [HideInInspector]
    public string mode = "inventory";

    private Item[,] items;
    private Image[,] images;

    private Item handItem;
    private Image handImage;

    private Item[] craftingItems;
    private Image[] craftingImages;

    private Image sprite;
    private RectTransform rect;
    private Image playerImage;
    private Image hotbarImage;
    private Image hotbarSelectorImage;
    private Text hotbarText;
    private Text inventoryText;
    private Image inventoryTextBg;

    #region Unity_functions
    public void Start() {
        items = new Item[4, 10];
        images = new Image[4, 10];
        craftingItems = new Item[5];
        craftingImages = new Image[5];
        sprite = GetComponent<Image>();
        rect = GetComponentInParent<RectTransform>();
        playerImage = transform.GetChild(0).GetComponent<Image>();
        hotbarImage = hotbar.GetComponent<Image>();
        hotbarSelectorImage = hotbar.transform.GetChild(0).GetComponent<Image>();
        hotbarText = hotbar.transform.GetChild(1).GetComponent<Text>();
        inventoryText = transform.GetChild(1).GetComponent<Text>();
        inventoryTextBg = inventoryText.transform.GetChild(0).GetComponent<Image>();

        ReadData();
    }

    public void Update() {
        //Use correct sprite
        if (mode == "crafting") {
            sprite.sprite = craftingSprite;
        } else if (mode == "chest") {
            sprite.sprite = chestSprite;
        } else if (mode == "furnace") {
            sprite.sprite = furnaceSprite;
        } else {
            sprite.sprite = inventorySprite;
        }
        rect.sizeDelta = new Vector2(sprite.sprite.rect.width, sprite.sprite.rect.height);

        //Toggle visibility depending on player
        bool showCrafting = mode == "inventory" || mode == "crafting";
        if (player.inInventory) {
            if (!sprite.enabled) {
                Display();
                if (showCrafting) DisplayCrafting();
                if (mode == "chest") player.chest.Display();
                if (mode == "furnace") player.furnace.Display();
            }
            DisplayHand();
            if (hotbarImage.enabled) {
                HideHotbar();
            }
        } else {
            if (sprite.enabled) {
                Hide();
            }
            DisplayHotbar();
        }

        //Keep track of mouse clicks
        bool leftClick = Input.GetMouseButtonDown(0),
            rightClick = Input.GetMouseButtonDown(1);

        //Reset inventory text
        inventoryText.text = "";

        if (player.inInventory) {
            float sc = rect.lossyScale.x;
            float w = sc * 10;
            Vector3 right = new Vector3(12, 0) * sc, down = new Vector3(0, -12) * sc;

            Vector3 ul;
            if (mode == "crafting") ul = rect.position + new Vector3(-83, 23) * sc;
            else if (mode == "chest") ul = rect.position + new Vector3(-59, 6) * sc;
            else if (mode == "furnace") ul = rect.position + new Vector3(-83, 23) * sc;
            else ul = rect.position + new Vector3(-59, 23) * sc;

            Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            //Clicking within inventory
            for (int r = 0; r < 4; r++) {
                for (int c = 0; c < 10; c++) {
                    Vector3 pos = ul + r * down + c * right;
                    //Check if mouse is within bounds of inv space
                    if (pos.x < mouse.x && mouse.x < pos.x + w
                        && pos.y - w < mouse.y && mouse.y < pos.y) {
                        //Update inventory text
                        if (items[r, c]) {
                            inventoryText.text = ItemData.GetTileName(items[r, c].id);
                        }

                        if (leftClick) {
                            if (handItem != null && items[r, c] != null && handItem.id == items[r, c].id) {
                                //Add to stack
                                items[r, c].quantity += handItem.quantity;
                                DeleteHand();
                            } else {
                                //Swap items
                                Item tempItem = handItem;
                                Image tempImage = handImage;
                                handItem = items[r, c];
                                handImage = images[r, c];
                                items[r, c] = tempItem;
                                images[r, c] = tempImage;
                            }
                        } else if (rightClick) {
                            if (handItem == null) { 
                                if (items[r, c] != null) {
                                    //Pick up half of stack
                                    handItem = Item.Create(items[r, c].id, (items[r, c].quantity + 1) / 2);
                                    items[r, c].quantity -= handItem.quantity;
                                    if (items[r, c].quantity == 0) {
                                        Delete(r, c);
                                    }
                                    handImage = NewImage(handItem);
                                }
                            } else {
                                //Instantiate an item if it doesn't already exist
                                if (items[r, c] == null) {
                                    items[r, c] = Item.Create(handItem.id, 0);
                                    images[r, c] = NewImage(items[r, c]);
                                }
                                //Place one item
                                if (handItem.id == items[r, c].id) {
                                    items[r, c].quantity++;
                                    handItem.quantity--;
                                    if (handItem.quantity == 0) {
                                        DeleteHand();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Clicking within chest
            if (mode == "chest") {
                Tuple<int, int> pos = player.chest.GetSlot(mouse, ul + new Vector3(0, 29 * sc), sc);
                if (pos != null) {
                    //Update inventory text
                    if (player.chest.items[pos.Item1, pos.Item2]) {
                        inventoryText.text = ItemData.GetTileName(player.chest.items[pos.Item1, pos.Item2].id);
                    }

                    if (leftClick) {
                        if (handItem != null && player.chest.items[pos.Item1, pos.Item2] != null
                            && handItem.id == player.chest.items[pos.Item1, pos.Item2].id) {
                            //Add to stack
                            player.chest.items[pos.Item1, pos.Item2].quantity += handItem.quantity;
                            DeleteHand();
                        } else {
                            //Swap items
                            Item tempItem = handItem;
                            Image tempImage = handImage;
                            handItem = player.chest.items[pos.Item1, pos.Item2];
                            handImage = player.chest.images[pos.Item1, pos.Item2];
                            player.chest.items[pos.Item1, pos.Item2] = tempItem;
                            player.chest.images[pos.Item1, pos.Item2] = tempImage;
                        }
                    } else if (rightClick) {
                        if (handItem == null) {
                            if (player.chest.items[pos.Item1, pos.Item2] != null) {
                                //Pick up half of stack
                                handItem = Item.Create(
                                    player.chest.items[pos.Item1, pos.Item2].id,
                                    (player.chest.items[pos.Item1, pos.Item2].quantity + 1) / 2);
                                player.chest.items[pos.Item1, pos.Item2].quantity -= handItem.quantity;
                                if (player.chest.items[pos.Item1, pos.Item2].quantity == 0) {
                                    player.chest.Delete(pos.Item1, pos.Item2);
                                }
                                handImage = NewImage(handItem);
                            }
                        } else {
                            //Instantiate an item if it doesn't already exist
                            if (player.chest.items[pos.Item1, pos.Item2] == null) {
                                player.chest.items[pos.Item1, pos.Item2] = Item.Create(handItem.id, 0);
                                player.chest.images[pos.Item1, pos.Item2] = NewImage(player.chest.items[pos.Item1, pos.Item2]);
                            }
                            //Place one item
                            if (handItem.id == player.chest.items[pos.Item1, pos.Item2].id) {
                                player.chest.items[pos.Item1, pos.Item2].quantity++;
                                handItem.quantity--;
                                if (handItem.quantity == 0) {
                                    DeleteHand();
                                }
                            }
                        }
                    }
                }
            }

            //Furnace
            if (mode == "furnace") {
                Furnace f = player.furnace;

                //input
                Vector3 pos = rect.position + new Vector3(51, 11) * sc;
                //Check if mouse is within bounds
                if (pos.x < mouse.x && mouse.x < pos.x + w
                    && pos.y - w < mouse.y && mouse.y < pos.y) {
                    //Update inventory text
                    if (player.furnace.inputItem) {
                        inventoryText.text = ItemData.GetTileName(f.inputItem.id);
                    }

                    if (leftClick) {
                        if (handItem != null && f.inputItem != null && handItem.id == f.inputItem.id) {
                            //Add to stack
                            f.inputItem.quantity += handItem.quantity;
                            DeleteHand();
                        } else {
                            //Swap items
                            Item tempItem = handItem;
                            Image tempImage = handImage;
                            handItem = f.inputItem;
                            handImage = f.inputImage;
                            f.inputItem = tempItem;
                            f.inputImage = tempImage;
                        }
                    } else if (rightClick) {
                        if (handItem == null) {
                            if (f.inputItem != null) {
                                //Pick up half of stack
                                handItem = Item.Create(f.inputItem.id, (f.inputItem.quantity + 1) / 2);
                                f.inputItem.quantity -= handItem.quantity;
                                if (f.inputItem.quantity == 0) {
                                    f.DeleteInput();
                                }
                                handImage = NewImage(handItem);
                            }
                        } else {
                            //Instantiate an item if it doesn't already exist
                            if (f.inputItem == null) {
                                f.inputItem = Item.Create(handItem.id, 0);
                                f.inputImage = NewImage(f.inputItem);
                            }
                            //Place one item
                            if (handItem.id == f.inputItem.id) {
                                f.inputItem.quantity++;
                                handItem.quantity--;
                                if (handItem.quantity == 0) {
                                    DeleteHand();
                                }
                            }
                        }
                    }
                }

                //output
                if (f.outputItem) {
                    pos.x += 20 * sc;
                    //Check if mouse is within bounds
                    if (pos.x < mouse.x && mouse.x < pos.x + w
                        && pos.y - w < mouse.y && mouse.y < pos.y) {
                        //Update inventory text
                        if (f.outputItem) {
                            inventoryText.text = ItemData.GetTileName(f.outputItem.id);
                        }

                        if (leftClick || rightClick) {
                            //Instantiate an item if hand is empty
                            if (handItem == null) {
                                handItem = Item.Create(f.outputItem.id, 0);
                                handImage = NewImage(handItem);
                            }
                            //Make sure player can pick up items
                            if (handItem.id == f.outputItem.id) {
                                if (leftClick) {
                                    //Pick up stack
                                    handItem.quantity += f.outputItem.quantity;
                                    f.DeleteOutput();
                                } else {
                                    //Pick up half of stack
                                    handItem.quantity += (f.outputItem.quantity + 1) / 2;
                                    f.outputItem.quantity /= 2;
                                    if (f.outputItem.quantity == 0) f.DeleteOutput();
                                }
                            }
                        }
                    }
                }
            }

            //Crafting inputs
            if (showCrafting) {
                if (mode == "crafting") {
                    ul = rect.position + new Vector3(44, 18) * sc;
                    right = new Vector3(26, 0) * sc;
                    down = new Vector3(0, -26) * sc;
                } else {
                    ul = rect.position + new Vector3(65, 15) * sc;
                    right = new Vector3(15, 0) * sc;
                    down = Vector3.zero;
                }
                for (int i = 0; i < 4; i++) {
                    if (mode == "inventory" && i > 1) break;

                    Vector3 pos = ul + i % 2 * right + i / 2 * down;
                    //Check if mouse is within bounds
                    if (pos.x < mouse.x && mouse.x < pos.x + w
                        && pos.y - w < mouse.y && mouse.y < pos.y) {
                        //Update inventory text
                        if (craftingItems[i]) {
                            inventoryText.text = ItemData.GetTileName(craftingItems[i].id);
                        }

                        if (leftClick) {
                            if (handItem != null && craftingItems[i] != null && handItem.id == craftingItems[i].id) {
                                //Add to stack
                                craftingItems[i].quantity += handItem.quantity;
                                DeleteHand();
                            } else {
                                //Swap items
                                Item tempItem = handItem;
                                Image tempImage = handImage;
                                handItem = craftingItems[i];
                                handImage = craftingImages[i];
                                craftingItems[i] = tempItem;
                                craftingImages[i] = tempImage;
                            }
                        } else if (rightClick) {
                            if (handItem == null) {
                                if (craftingItems[i] != null) {
                                    //Pick up half of stack
                                    handItem = Item.Create(craftingItems[i].id, (craftingItems[i].quantity + 1) / 2);
                                    craftingItems[i].quantity -= handItem.quantity;
                                    if (craftingItems[i].quantity == 0) {
                                        DeleteCrafting(i);
                                    }
                                    handImage = NewImage(handItem);
                                }
                            } else {
                                //Instantiate an item if it doesn't already exist
                                if (craftingItems[i] == null) {
                                    craftingItems[i] = Item.Create(handItem.id, 0);
                                    craftingImages[i] = NewImage(craftingItems[i]);
                                }
                                //Place one item
                                if (handItem.id == craftingItems[i].id) {
                                    craftingItems[i].quantity++;
                                    handItem.quantity--;
                                    if (handItem.quantity == 0) {
                                        DeleteHand();
                                    }
                                }
                            }
                        }
                    }
                }

                //Crafting output
                if (craftingItems[4]) {
                    Vector3 pos;
                    if (mode == "crafting") pos = ul + new Vector3(13 * sc, -13 * sc);
                    else pos = ul + new Vector3(8 * sc, -25 * sc);

                    //Check if mouse is within bounds
                    if (pos.x < mouse.x && mouse.x < pos.x + w
                        && pos.y - w < mouse.y && mouse.y < pos.y) {
                        //Update inventory text
                        if (craftingItems[4]) {
                            inventoryText.text = ItemData.GetTileName(craftingItems[4].id);
                        }

                        if (!textManager.disabled)
                        {
                            switch (ItemData.GetTileType(craftingItems[4].id))
                            {
                                case Chunk.TileType.WORKBENCH:
                                    if (notSeenBench)
                                    {
                                        textManager.reload(benchScript);
                                        taskManager.addTask(workbenchTask);
                                    }
                                    notSeenBench = false;
                                    break;

                                case Chunk.TileType.FURNACE:
                                    if (notSeenFurnace)
                                    {
                                        textManager.reload(furnaceScript);
                                        taskManager.addTask(furnaceTask);
                                    }
                                    notSeenFurnace = false;
                                    break;

                                default:
                                    break;
                            }
                        }

                        if (leftClick || rightClick) {
                            //Instantiate an item if hand is empty
                            if (handItem == null) {
                                handItem = Item.Create(craftingItems[4].id, 0);
                                handImage = NewImage(handItem);
                            }
                            //Make sure player can pick up items
                            if (handItem.id == craftingItems[4].id) {
                                handItem.quantity += craftingItems[4].quantity;
                                //Subtract from each input slot
                                for (int i = 0; i < 4; i++) {
                                    if (craftingItems[i]) {
                                        craftingItems[i].quantity--;
                                        if (craftingItems[i].quantity == 0) {
                                            DeleteCrafting(i);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            Display();
            if (showCrafting) DisplayCrafting();
            if (mode == "chest") player.chest.Display();
            if (mode == "furnace") player.furnace.Display();
        }
    }
    #endregion

    #region display_functions
    public void Display() {
        Vector3 right = new Vector3(12, 0), down = new Vector3(0, -12);
        Vector3 scale = new Vector3(8, 8, 1);

        Vector3 ul;
        if (mode == "crafting") ul = -1.5f * down - 6.5f * right;
        else if (mode == "chest") ul = new Vector3(0, 1) - 4.5f * right;
        else if (mode == "furnace") ul = -1.5f * down - 6.5f * right;
        else ul = -1.5f * down - 4.5f * right;

        for (int r = 0; r < 4; r++) {
            for (int c = 0; c < 10; c++) {
                if (items[r, c]) {
                    //Calculate position in inventory
                    Vector3 pos = ul + r * down + c * right;

                    items[r, c].Display(pos, scale, images[r, c]);
                } else if (images[r, c]) {
                    Destroy(images[r, c].gameObject);
                }
            }
        }

        //Enable image component
        sprite.enabled = true;
        //Player sprite
        if (mode == "inventory") playerImage.enabled = true;
        else playerImage.enabled = false;

        DisplayInventoryText();
    }
    public void DisplayHand() {
        if (handItem == null) return;

        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float sc = rect.lossyScale.x;
        Vector3 scale = new Vector3(8, 8, 1);

        Vector3 pos = (mouse - rect.position) / sc;
        handItem.Display(pos, scale, handImage);
        handImage.GetComponent<Canvas>().sortingOrder = 5;
    }
    public void DisplayCrafting() {
        //Check if anything can be crafted
        int id1 = craftingItems[0] == null ? 0 : craftingItems[0].id,
            id2 = craftingItems[1] == null ? 0 : craftingItems[1].id,
            id3 = craftingItems[2] == null ? 0 : craftingItems[2].id,
            id4 = craftingItems[3] == null ? 0 : craftingItems[3].id;
        Tuple<int, int> product = CraftingData.Craft(id1, id2, id3, id4);
        //Remove crafting result if invalid result
        if (product == null && craftingItems[4]) {
            DeleteCrafting(4);
        }
        //Add result to crafting grid
        if (product != null && (craftingItems[4] == null || craftingItems[4].id != product.Item1)) {
            DeleteCrafting(4);
            craftingItems[4] = Item.Create(product.Item1, product.Item2);
            craftingImages[4] = NewImage(craftingItems[4]);
        }

        //Display crafting items
        for (int i = 0; i < 5; i++) {
            if (mode == "inventory" && i > 1) i = 4;

            if (craftingItems[i]) {
                //Calculate position in inventory
                Vector3 pos;
                if (mode == "crafting") {
                    if (i == 4) pos = new Vector3(62, 0);
                    else pos = new Vector3(49 + i % 2 * 26, 13 - i / 2 * 26);
                } else {
                    if (i == 4) pos = new Vector3(78, -15);
                    else pos = new Vector3(70 + i * 15, 10);
                }

                craftingItems[i].Display(pos, new Vector3(8, 8, 1), craftingImages[i]);
            }
        }
    }
    public void DisplayHotbar() {
        Vector3 right = new Vector3(12, 0);
        Vector3 ul = -4.5f * right + new Vector3(0, 8);
        Vector3 scale = new Vector3(8, 8, 1);

        //Display hotbar items
        for (int c = 0; c < 10; c++) {
            if (items[3, c]) {
                //Calculate position in inventory
                Vector3 pos = ul + c * right;

                items[3, c].Display(pos, scale, images[3, c]);

                //adjust anchors
                Transform t = images[3, c].transform;
                RectTransform rt = t.GetComponent<RectTransform>();
                rt.anchoredPosition = pos;
                rt.anchorMax = new Vector2(0.5f, 0);
                rt.anchorMin = new Vector2(0.5f, 0);
            } else if (images[3, c]) {
                Destroy(images[3, c].gameObject);
            }
        }

        //Hotbar selection
        hotbarSelectorImage.GetComponent<RectTransform>().anchoredPosition = player.selected < 10 ? right * player.selected : new Vector3(146, 0);

        //Enable hotbar images
        hotbarImage.enabled = true;
        hotbarSelectorImage.enabled = true;

        DisplayHotbarText();
    }

    private int selection = 0;
    private bool isNull = true;
    private float duration = 0, maxDuration = 2;
    public void DisplayHotbarText() {
        if (player.selected < 10) {
            //Get item text
            if (items[3, player.selected] == null) {
                hotbarText.text = "";
            } else {
                hotbarText.text = ItemData.GetTileName(items[3, player.selected].id);
            }

            //If player switches position in hotbar, display item text
            if (player.selected != selection || (isNull && items[3, player.selected])) {
                selection = player.selected;
                isNull = items[3, player.selected] == null;
                duration = maxDuration;
            }
        } else {
            hotbarText.text = "";
        }

        //Display initially white, then fade
        hotbarText.color = Color.Lerp(new Color(255, 255, 255, 0), Color.white, duration / .5f);
        duration -= Time.deltaTime;

        hotbarText.enabled = true;
    }

    public void DisplayInventoryText() {
        //Change position
        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float sc = rect.lossyScale.x;
        Vector3 scale = new Vector3(8, 8, 1);

        RectTransform rt = inventoryText.GetComponent<RectTransform>();
        rt.anchoredPosition = (mouse - rect.position) / sc + new Vector3(6, 0);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);

        inventoryText.enabled = true;

        //Text background
        inventoryTextBg.GetComponent<RectTransform>().sizeDelta = new Vector2(6 + inventoryText.preferredWidth, 6 + inventoryText.fontSize);
        inventoryTextBg.enabled = inventoryText.text.Length > 0;
    }

    public void Hide() {
        //Hide inventory items
        for (int r = 0; r < 4; r++) {
            for (int c = 0; c < 10; c++) {
                if (items[r, c]) {
                    images[r, c].enabled = false;
                    images[r, c].GetComponentInChildren<Text>().enabled = false;
                }
            }
        }

        //Hide held item
        if (handItem != null) {
            handImage.enabled = false;
            handImage.GetComponentInChildren<Text>().enabled = false;
        }

        //Hide chest
        player.chest.Hide();
        //Hide furnace
        if (player.furnace) player.furnace.Hide();

        //Hide crafting items
        for (int i = 0; i < 5; i++) {
            if (craftingItems[i]) {
                craftingImages[i].enabled = false;
                craftingImages[i].GetComponentInChildren<Text>().enabled = false;
            }
        }

        //Disable image component
        sprite.enabled = false;
        playerImage.enabled = false;
        inventoryText.enabled = false;
        inventoryTextBg.enabled = false;
    }
    public void HideHotbar() {
        //Hide the hotbar
        hotbarImage.enabled = false;
        hotbarSelectorImage.enabled = false;
        hotbarText.enabled = false;
    }
    #endregion

    #region management_functions
    public void WriteData() {
        StreamWriter sw = new StreamWriter(Application.dataPath + "/inventory.txt");
        using (sw) {
            //write inventory
            for (int r = 0; r < 4; r++) {
                for (int c = 0; c < 10; c++) {
                    if (items[r, c] == null) {
                        sw.WriteLine("0 0");
                    } else {
                        sw.WriteLine((int)ItemData.GetTileType(items[r, c].id) + " " + items[r, c].quantity);
                    }
                }
            }

            //write crafting items
            for (int i = 0; i < 5; i++) {
                if (craftingItems[i] == null) {
                    sw.WriteLine("0 0");
                } else {
                    sw.WriteLine((int)ItemData.GetTileType(craftingItems[i].id) + " " + craftingItems[i].quantity);
                }
            }

            //write hand item
            if (handItem == null) {
                sw.WriteLine("0 0");
            } else {
                sw.WriteLine((int)ItemData.GetTileType(handItem.id) + " " + handItem.quantity);
            }

            sw.Close();
        }
    }

    public void ReadData() {
        try {
            StreamReader sr = new StreamReader(Application.dataPath + "/inventory.txt");
            using (sr) {
                string line;

                //read inventory items
                for (int r = 0; r < 4; r++) {
                    for (int c = 0; c < 10; c++) {
                        line = sr.ReadLine();
                        string[] split = line.Split(' ');
                        int id = ItemData.GetID((Chunk.TileType)int.Parse(split[0])), q = int.Parse(split[1]);
                        if (q > 0) {
                            items[r, c] = Item.Create(id, q);
                            images[r, c] = NewImage(items[r, c]);
                        } else {
                            Delete(r, c);
                        }
                    }
                }

                //read crafting items
                for (int i = 0; i < 5; i++) {
                    line = sr.ReadLine();
                    string[] split = line.Split(' ');
                    int id = ItemData.GetID((Chunk.TileType)int.Parse(split[0])), q = int.Parse(split[1]);
                    if (q > 0) {
                        craftingItems[i] = Item.Create(id, q);
                        craftingImages[i] = NewImage(craftingItems[i]);
                    } else {
                        DeleteCrafting(i);
                    }
                }

                //read hand item
                line = sr.ReadLine();
                string[] split2 = line.Split(' ');
                int id2 = ItemData.GetID((Chunk.TileType)int.Parse(split2[0])), q2 = int.Parse(split2[1]);
                if (q2 > 0) {
                    handItem = Item.Create(id2, q2);
                    handImage = NewImage(handItem);
                } else {
                    DeleteHand();
                }

                sr.Close();
            }
        } catch {
            return;
        }
    }

    //Attempt to add an item to inventory
    public void Add(Item i) {
        if (!textManager.disabled)
        {
            switch (ItemData.GetTileType(i.id))
            {
                //If you get wood for first time
                case Chunk.TileType.LOG:
                    if (notSeenLog)
                    {
                        textManager.reload(woodScript);
                    }
                    notSeenLog = false;
                    break;

                //If you get stone for first time
                case Chunk.TileType.STONE:
                    if (notSeenStone)
                    {
                        textManager.reload(stoneScript);
                    }
                    notSeenStone = false;
                    break;

                default:
                    break;
            }
        }

        //Check if item already exists; stack items
        for (int r = 3; r >= 0; r--) {
            for (int c = 0; c < 10; c++) {
                if (items[r, c] && items[r, c].id == i.id) {
                    //Add to existing item
                    items[r, c].quantity += i.quantity;
                    Destroy(i.gameObject);

                    if (player.inInventory) {
                        Display();
                    }

                    return;
                }
            }
        }
        //Check for empty spaces
        for (int r = 3; r >= 0; r--) {
            for (int c = 0; c < 10; c++) {
                if (items[r, c] == null) {
                    //Create new item
                    items[r, c] = Item.Create(i);
                    Destroy(i.gameObject);

                    //Create a UI sprite
                    images[r, c] = NewImage(items[r, c]);

                    if (player.inInventory) {
                        Display();
                    }

                    return;
                }
            }
        }
    }

    //Delete an item from the inventory
    public void Delete(int r, int c) {
        if (items[r, c] != null) Destroy(items[r, c].gameObject);
        if (images[r, c] != null) Destroy(images[r, c].gameObject);
    }
    //Empty hand
    public void DeleteHand() {
        if (handItem != null) Destroy(handItem.gameObject);
        if (handImage != null) Destroy(handImage.gameObject);
    }
    //Empty crafting grid
    public void DeleteCrafting(int i) {
        if (craftingItems[i] != null) {
            Destroy(craftingItems[i].gameObject);
            craftingItems[i] = null; //prevents duping
        }
        if (craftingImages[i] != null) {
            Destroy(craftingImages[i].gameObject);
        }
    }

    //Get item
    public Item GetItem(int r, int c) {
        return items[r, c];
    }
    #endregion

    public Image NewImage(Item item = null) {
        Image output = Instantiate(baseImage);
        output.transform.SetParent(transform.parent);
        //Get appropriate sprite
        if (item != null) {
            output.sprite = ItemData.GetTileSprite(item.id);
        }
        return output;
    }
}
