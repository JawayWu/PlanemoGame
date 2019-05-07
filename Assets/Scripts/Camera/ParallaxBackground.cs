using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxBackground : MonoBehaviour
{
    public ParallaxBackground leftBackground;
    public ParallaxBackground rightBackground;

    private SpriteRenderer spriteRenderer;
    public SpriteRenderer SpriteRenderer
    {
        get { return spriteRenderer; }
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
}

