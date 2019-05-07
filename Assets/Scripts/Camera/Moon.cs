using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Moon : MonoBehaviour
{
    private GenerationManager world;
    private Transform mainCameraTransform;
    private SpriteRenderer spriteRenderer;

    [Header("Orbit")]
    [SerializeField]
    [Tooltip("The center of the moons orbit")]
    private Vector2 orbitCenter = new Vector2(0, 40);
    [SerializeField]
    [Tooltip("The radius of the moons orbit")]
    private float orbitRadius = 80;
    [Header("Sunrise")]
    [SerializeField]
    [Tooltip("The angle at which the moon will rise (clockwise from the -y axis)")]
    private float angleMoonRise = 120;
    [Header("Sunset")]
    [SerializeField]
    [Tooltip("The angle at which the moon will set (clockwise from the -y axis)")]
    private float angleMoonSet = 240;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        world = GenerationManager.Instance;
        mainCameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        float time = world.TimeOfDay;
        float timeMoonrise = GenerationManager.Instance.MoonriseTime;
        float timeMoonset = GenerationManager.Instance.MoonsetTime;
        float timeSunrise = GenerationManager.Instance.SunriseTime;
        float timeSunset = GenerationManager.Instance.SunsetTime;

        if (time < timeMoonset || time > timeMoonrise) {
            spriteRenderer.enabled = true;
        } else {
            spriteRenderer.enabled = false;
            return;
        }

        float maxMoonlightAngle = angleMoonSet - angleMoonRise;
        float totalMoonTime = timeMoonrise - timeMoonset;
        //Get the angle for the Moon vector
        float angleInDegrees = (time - timeMoonrise) / totalMoonTime * maxMoonlightAngle;
        if (time < timeMoonset) angleInDegrees += 24 / totalMoonTime * maxMoonlightAngle;
        //The angle clockwise from the -y axis (represents the sun position at 0/24h)
        float angleForMoonPosition = -angleInDegrees - angleMoonRise - 90;
        //Get the Vector2 position of the moon
        Vector2 moonVector = DegreeToVector(angleForMoonPosition) * orbitRadius + orbitCenter;
        //Set the position of the moon
        transform.position = new Vector3(moonVector.x + mainCameraTransform.position.x, moonVector.y, transform.position.z);
    }

    /// <summary>
    /// Convert an angle in degrees to a Vector2
    /// </summary>
    /// <param name="angle">Angle in degrees</param>
    /// <returns>Returns the resultant Vector2</returns>
    private Vector2 DegreeToVector(float angle)
    {
        float angleInRadians = Mathf.Deg2Rad * angle;
        return new Vector2(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians));
    }
}
