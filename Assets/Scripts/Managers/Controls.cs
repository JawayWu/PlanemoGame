using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controls : MonoBehaviour {
    private bool showing;
    private Transform images, texts;

    public void Awake() {
        showing = false;
        images = transform.GetChild(0);
        texts = transform.GetChild(1);
        Hide();
    }

    public void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (showing) {
                Hide();
            } else {
                Show();
            }
        }
    }

    public void Hide() {
        showing = false;
        images.gameObject.SetActive(false);
        texts.gameObject.SetActive(false);
    }

    public void Show() {
        showing = true;
        images.gameObject.SetActive(true);
        texts.gameObject.SetActive(true);
    }
}
