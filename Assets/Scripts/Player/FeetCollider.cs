using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeetCollider : MonoBehaviour {
    private PlayerMovement parent;

    #region Unity_functions
    public void Start() {
        parent = GetComponentInParent<PlayerMovement>();
    }

    //check for landings
    public void OnTriggerEnter2D(Collider2D collision) {
        if (!collision.isTrigger) parent.jumping = false;
    }
    public void OnTriggerStay2D(Collider2D collision) {
        if (!collision.isTrigger) parent.jumping = false;
    }
    //activates when jumping/walking off a block
    public void OnTriggerExit2D(Collider2D collision) {
        if (!collision.isTrigger) parent.jumping = true;
    }
    #endregion
}
