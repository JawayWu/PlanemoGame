using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sun : MonoBehaviour
{
    private GenerationManager world;
    private Transform mainCameraTransform;
    private SpriteRenderer spriteRenderer;

    [Header("Orbit")]
    [SerializeField]
    [Tooltip("The center of the suns orbit")]
    private Vector2 orbitCenter = new Vector2(0, 40);
    [SerializeField]
    [Tooltip("The radius of the suns orbit")]
    private float orbitRadius = 80;
    [Header("Sunrise")]
    [SerializeField]
    [Tooltip("The angle at which the sun will rise (clockwise from the -y axis)")]
    private float angleSunRise = 120;
    [Header("Sunset")]
    [SerializeField]
    [Tooltip("The angle at which the sun will set (clockwise from the -y axis)")]
    private float angleSunSet = 240;

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
        float timeSunrise = GenerationManager.Instance.SunriseTime - 2;
        float timeSunset = GenerationManager.Instance.SunsetTime + 2;

        if (time < timeSunrise || time > timeSunset)
        {
            spriteRenderer.enabled = false;
            return;
        }
        else
            spriteRenderer.enabled = true;

        float maxSunlightAngle = angleSunSet - angleSunRise;
        float totalSunTime = timeSunset - timeSunrise;
        //Get the angle for the Sun vector
        float angleInDegrees = (time - timeSunrise) / totalSunTime * maxSunlightAngle;
        //The angle clockwise from the -y axis (represents the sun position at 0/24h)
        float angleForSunPosition = -angleInDegrees - angleSunRise - 90;
        //Get the Vector2 position of the sun
        Vector2 sunVector = DegreeToVector(angleForSunPosition) * orbitRadius + orbitCenter;
        //Set the position of the sun
        transform.position = new Vector3(sunVector.x + mainCameraTransform.position.x, sunVector.y, transform.position.z);
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
