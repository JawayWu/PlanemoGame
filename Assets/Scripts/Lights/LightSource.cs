using UnityEngine;

/// <summary>
/// A lightweight struct container that represents a block's lighting data while updating or removing lighting.
/// </summary>
public struct LightNode
{
    public Vector3Int position;
    public Color color;
    public Chunk chunk;
    public LightNode(Vector3Int position, Color color, Chunk chunk)
    {
        this.position = position;
        this.color = color;
        this.chunk = chunk;
    }
}

/// <summary>
/// Describes a source of light in the game.
/// </summary>
public class LightSource : MonoBehaviour
{
    public Color lightColor;
    public float LightStrength { get; set; }
    public bool Initialized { get; set; }
    public Vector3Int Position { get; set; }


    private void Awake()
    {
        // Save a Vector3Int version of its position for easy use with Tilemaps
        Position = new Vector3Int((int)transform.position.x, (int)transform.position.y, 0);
    }


    /// <summary>
    /// Set the light's core values and settings.
    /// </summary>
    /// <param name="color"></param>
    /// <param name="strength"></param>
    public void InitializeLight(Color color, float strength)
    {
        lightColor = color;
        LightStrength = strength;
        Initialized = true;
    }
}