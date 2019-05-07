using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arm : MonoBehaviour {
    public PlayerMovement pm;
    public Sprite arm_weapon, arm_noweapon;
    private SpriteRenderer sr;

    public void Awake() {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Update() {
        if (pm.selected == 10) {
            sr.sprite = arm_weapon;
        } else {
            sr.sprite = arm_noweapon;
        }
    }
}
