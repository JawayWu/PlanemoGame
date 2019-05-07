using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxBackgroundController : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField]
    [Tooltip("The camera used for parallaxing")]
    private Camera parallaxCamera;
    [Header("Properties")]
    [SerializeField]
    [Tooltip("The y position of the background")]
    private float yPosition = 60;
    [SerializeField]
    [Tooltip("The rate at which the backgrounds will parallax")]
    private float parallaxRate = 0.1f;
    [SerializeField]
    [Tooltip("The factor used to determine how the z position will affect the amount of parallaxing")]
    private float zParallaxFactor = 1;
    [SerializeField]
    [Tooltip("The background will be duplicated once its edge is within this threshold")]
    private float duplicateThreshold = 10;
    [SerializeField]
    [Tooltip("The background will be removed once its edge is past this threshold")]
    private float removalThreshold = 60;

    private List<ParallaxBackground> backgrounds;
    private List<ParallaxBackground> backgroundsToRemove;
    private List<ParallaxBackground> duplicateBackgrounds;

    private float prevCameraXPosition;
    private float prevCameraYPosition;
    private void Start()
    {
        backgrounds = new List<ParallaxBackground>(transform.childCount);
        backgroundsToRemove = new List<ParallaxBackground>(transform.childCount);
        duplicateBackgrounds = new List<ParallaxBackground>(transform.childCount);
        for (int i = 0; i < transform.childCount; i++)
        {
            ParallaxBackground background = transform.GetChild(i).GetComponent<ParallaxBackground>();
            if (background != null)
            {
                //Offset all the backgrounds y positions by the set amount
                background.transform.position = new Vector3(parallaxCamera.transform.position.x, yPosition, background.transform.position.z);
                backgrounds.Add(background);
            }
        }
        prevCameraXPosition = parallaxCamera.transform.position.x;
        prevCameraYPosition = parallaxCamera.transform.position.y;

    }

    private void Update()
    {
        foreach (ParallaxBackground background in backgrounds)
        {
            //Move the backgrounds by a set amount depending on the position of the camera and their z position to produce a parallaxing effect
            Transform backgroundTransform = background.transform;
            float parallax = (prevCameraXPosition - parallaxCamera.transform.position.x) * backgroundTransform.position.z * zParallaxFactor;
            Vector3 newPosition = new Vector3(backgroundTransform.transform.position.x - parallax, backgroundTransform.transform.position.y, backgroundTransform.transform.position.z);
            backgroundTransform.position = Vector3.Lerp(backgroundTransform.position, newPosition, parallaxRate);

            float cameraHalfWidth = parallaxCamera.orthographicSize * Screen.width / Screen.height;
            float minCameraX = parallaxCamera.transform.position.x - cameraHalfWidth;
            float maxCameraX = parallaxCamera.transform.position.x + cameraHalfWidth;
            float minBackgroundX = background.transform.position.x - background.SpriteRenderer.sprite.bounds.size.x / 2f;
            float maxBackgroundX = background.transform.position.x + background.SpriteRenderer.sprite.bounds.size.x / 2f;
            //Add the background to the list to be destroyed if it is past the remove threshold off screen
            if (minCameraX - maxBackgroundX > removalThreshold || minBackgroundX - maxCameraX > removalThreshold)
                backgroundsToRemove.Add(background);
            //Duplicate the background if the edge is within the duplicate threshold of the camera
            if (background.leftBackground == null && minCameraX - minBackgroundX < duplicateThreshold)
            {
                Vector3 position = new Vector3(minBackgroundX - background.SpriteRenderer.sprite.bounds.size.x / 2f, background.transform.position.y, background.transform.position.z);
                background.leftBackground = Instantiate<ParallaxBackground>(background, position, new Quaternion(0, 0, 0, 0), transform);
                background.leftBackground.rightBackground = background;
                duplicateBackgrounds.Add(background.leftBackground);
            }
            else if (background.rightBackground == null && maxBackgroundX - maxCameraX < duplicateThreshold)
            {
                Vector3 position = new Vector3(maxBackgroundX + background.SpriteRenderer.sprite.bounds.size.x / 2f, background.transform.position.y, background.transform.position.z);
                background.rightBackground = Instantiate<ParallaxBackground>(background, position, new Quaternion(0, 0, 0, 0), transform);
                background.rightBackground.leftBackground = background;
                duplicateBackgrounds.Add(background.rightBackground);
            }

        }
        foreach (ParallaxBackground background in backgroundsToRemove)
        {
            backgrounds.Remove(background);
            Destroy(background.gameObject);
        }
        backgroundsToRemove.Clear();
        foreach (ParallaxBackground background in duplicateBackgrounds)
            backgrounds.Add(background);
        duplicateBackgrounds.Clear();
        prevCameraXPosition = parallaxCamera.transform.position.x;
    }
}
