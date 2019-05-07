using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cloud : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private float speed;
    /// <summary>
    /// The horizontal speed of the clouds movement
    /// </summary>
    public float Speed
    {
        get { return speed; }
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Initialize the cloud 
    /// </summary>
    /// <param name="position">The position to place the cloud</param>
    /// <param name="speed">The speed of the cloud</param>
    /// <param name="sprite">The sprite to use for the cloud</param>
    public void Initialize(Vector3 position, float speed, Sprite sprite)
    {
        transform.position = position;
        this.speed = speed;
        spriteRenderer.sprite = sprite;
    }
}
