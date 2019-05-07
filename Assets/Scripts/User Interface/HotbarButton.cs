using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HotbarButton : MonoBehaviour
{
    //Which hotbar index this button corresponds to.
    public int hotbarIndex;

    //The player that contains the hotbar.
    public GameObject player;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //When you click the button, change to this hotbar index.
    public void hotbarClick()
    {
        player.GetComponent<PlayerMovement>().selected = hotbarIndex;
    }
}
