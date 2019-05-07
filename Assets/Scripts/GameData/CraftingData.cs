using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CraftingData : MonoBehaviour {
    //Stores crafting data in tuples
    //If items with IDs a, b, c, and d can combine to make e copies of an item with ID f,
    //then crafting[a][b][c][d] = (e, f).
    public static Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<int, Tuple<int, int>>>>> crafting;

    public void Start() {
        crafting = new Dictionary<int, Dictionary<int, Dictionary<int, Dictionary<int, Tuple<int, int>>>>>();

        //Read list of recipes
        TextAsset ta = Resources.Load<TextAsset>("recipes");
        string[] lines = ta.text.Split('\n');
        foreach (string line in lines) {
            if (line != null) {
                if (line.Length > 0 && line[0] == '-') break;

                string[] items = line.Split('/');
                if (items.Length == 4) {
                    int id0 = ItemData.GetID(items[0]),
                        id1 = ItemData.GetID(items[1]),
                        id2 = ItemData.GetID(items[2]),
                        q = int.Parse(items[3]);
                    Tuple<int, int> item = new Tuple<int, int>(id2, q);
                    SetRecipe(id0, id1, item: item);
                    //Singular blocks can be placed in any crafting input
                    if (id1 == 0) {
                        SetRecipe(0, id0, 0, 0, item);
                        SetRecipe(0, 0, id0, 0, item);
                        SetRecipe(0, 0, 0, id0, item);
                    }
                } else if (items.Length == 6) {
                    int id0 = ItemData.GetID(items[0]),
                        id1 = ItemData.GetID(items[1]),
                        id2 = ItemData.GetID(items[2]),
                        id3 = ItemData.GetID(items[3]),
                        id4 = ItemData.GetID(items[4]),
                        q = int.Parse(items[5]);
                    SetRecipe(id0, id1, id2, id3, new Tuple<int, int>(id4, q));
                }
            }
        }
    }

    //Add a recipe given IDs and resulting item
    private void SetRecipe(int id0, int id1, int id2 = 0, int id3 = 0, Tuple<int, int> item = null) {
        if (!crafting.ContainsKey(id0)) crafting.Add(id0, new Dictionary<int, Dictionary<int, Dictionary<int, Tuple<int, int>>>>());
        if (!crafting[id0].ContainsKey(id1)) crafting[id0].Add(id1, new Dictionary<int, Dictionary<int, Tuple<int, int>>>());
        if (!crafting[id0][id1].ContainsKey(id2)) crafting[id0][id1].Add(id2, new Dictionary<int, Tuple<int, int>>());
        crafting[id0][id1][id2].Add(id3, item);
    }

    //Return item created by crafting together items with IDs id0, id1, id2, id3
    public static Tuple<int, int> Craft(int id0, int id1, int id2, int id3, int q0 = 1, int q1 = 1, int q2 = 1, int q3 = 1) {
        if (!crafting.ContainsKey(id0)
            || crafting[id0] == null
            || !crafting[id0].ContainsKey(id1)
            || crafting[id0][id1] == null
            || !crafting[id0][id1].ContainsKey(id2)
            || crafting[id0][id1][id2] == null
            || !crafting[id0][id1][id2].ContainsKey(id3)
            || crafting[id0][id1][id2][id3] == null) {
            return null;
        }
        
        Tuple<int, int> item = crafting[id0][id1][id2][id3];
        return new Tuple<int, int>(item.Item1, item.Item2 * Mathf.Max(q0, q1, q2, q3));
    }
}
