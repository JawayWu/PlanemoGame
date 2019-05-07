using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudSpawner : MonoBehaviour
{
    public enum WindDirection { East, West }

    private const float CloudSpawnScreenOffset = 10;
    private const float CloudRemoveScreenThreshold = 10;

    [Header("Cloud Objects")]
    [SerializeField]
    [Tooltip("The camera to use for the cloud spawning")]
    private Camera cloudCamera;
    [SerializeField]
    [Tooltip("The cloud prefab to be spawned")]
    private GameObject cloudPrefab;
    [SerializeField]
    [Tooltip("Sprites of clouds")]
    private List<Sprite> cloudSprites;
    [Header("Spawn Properties")]
    [SerializeField]
    [Tooltip("The initial amount of clouds to be spawned")]
    private int initialSpawnAmount = 5;
    [SerializeField]
    [Tooltip("The rate at which the clouds will spawn")]
    private float spawnRate = 1f;
    [SerializeField]
    [Tooltip("The max number of clouds that can be spawned at any given time")]
    private int maxNumberClouds = 10;
    [SerializeField]
    [Tooltip("The amount of clouds that can be stored in the object pool")]
    private int cloudPoolLimit = 20;
    [SerializeField]
    [Tooltip("The y position to spawn the clouds around")]
    private float cloudSpawnY = 100;
    [SerializeField]
    [Tooltip("The fluctuation in the y position of the clouds")]
    private float cloudSpawnYFluctuation = 20;
    [SerializeField]
    [Tooltip("The smallest z position allowed when spawning the clouds")]
    private float cloudMinZPosition = 0;
    [SerializeField]
    [Tooltip("The largest z position allowed when spawning the clouds")]
    private float cloudMaxZPosition = 10;
    [Header("Cloud Movement")]
    [SerializeField]
    [Tooltip("The maximum speed of any given cloud")]
    private float maxCloudSpeed = 10;
    [SerializeField]
    [Tooltip("The direction the clouds will move")]
    private WindDirection windDirection;
    [SerializeField]
    [Tooltip("The factor affecting the difference in speed between clouds at different z positions")]
    private float zSpeedFactor = 30;

    private Queue<Cloud> cloudPool;
    private List<Cloud> clouds;
    private List<Cloud> cloudsForRemoval;

    private float cloudSpawnTimer;

    private Vector3 prevCameraPosition;

    private void Start()
    {
        cloudPool = new Queue<Cloud>();
        clouds = new List<Cloud>(maxNumberClouds);
        cloudsForRemoval = new List<Cloud>(maxNumberClouds);
        //Ensure that the initial spawn amount is less than or equal to the maximum amount of clouds
        if (initialSpawnAmount > maxNumberClouds)
            maxNumberClouds = initialSpawnAmount;

        float cameraHalfWidth = cloudCamera.orthographicSize * Screen.width / Screen.height;
        for (int i = 0; i < initialSpawnAmount; i++)
        {
            //Spawn the initial amount of clouds at varying x, y and z positions in front of the camera
            float x, y, z;
            x = Random.Range(cloudCamera.transform.position.x - cameraHalfWidth, cloudCamera.transform.position.x + cameraHalfWidth);
            y = cloudSpawnY + Random.Range(-cloudSpawnYFluctuation, cloudSpawnYFluctuation);
            z = Random.Range(cloudMinZPosition, cloudMaxZPosition);
            Vector3 cloudPosition = new Vector3(x, y, z);
            SpawnCloud(cloudPosition);
        }
        prevCameraPosition = cloudCamera.transform.position;
    }

    public void Update()
    {
        cloudSpawnTimer += Time.deltaTime;
        float cameraHalfWidth = cloudCamera.orthographicSize * Screen.width / Screen.height;
        //Spawn another cloud whenever the cloudSpawnTimer reaches the spawnRate
        if (cloudSpawnTimer >= spawnRate && clouds.Count < maxNumberClouds)
        {
            float x, y, z;
            if (Random.Range(0, 2) == 0)
                x = cloudCamera.transform.position.x - cameraHalfWidth - CloudSpawnScreenOffset;
            else
                x = cloudCamera.transform.position.x + cameraHalfWidth + CloudSpawnScreenOffset;
            y = cloudSpawnY + Random.Range(-cloudSpawnYFluctuation, cloudSpawnYFluctuation);
            z = Random.Range(cloudMinZPosition, cloudMaxZPosition);
            Vector3 cloudPosition = new Vector3(x, y, z);
            SpawnCloud(cloudPosition);
            cloudSpawnTimer = 0;
        }
        //Update Cloud positions
        foreach (Cloud cloud in clouds)
        {
            //Set the new position of the cloud based on the cloud's speed and parallax
            float parallax = (prevCameraPosition.x - cloudCamera.transform.position.x) * cloud.transform.position.z;
            float newX = cloud.transform.position.x - parallax + cloud.Speed * Time.deltaTime;
            Vector3 newPosition = new Vector3(newX, cloud.transform.position.y, cloud.transform.position.z);
            cloud.transform.position = Vector3.Lerp(cloud.transform.position, newPosition, 0.1f);
            //Add the cloud to the removal list if it exits the screen beyond the Cloud Remove Screen Threshold
            switch (windDirection)
            {
                case WindDirection.East:
                    if (cloud.transform.position.x > cloudCamera.transform.position.x + cameraHalfWidth + CloudRemoveScreenThreshold)
                        cloudsForRemoval.Add(cloud);
                    break;
                case WindDirection.West:
                    if (cloud.transform.position.x < cloudCamera.transform.position.x - cameraHalfWidth - CloudRemoveScreenThreshold)
                        cloudsForRemoval.Add(cloud);
                    break;
            }
        }
        //Remove the clouds
        foreach (Cloud cloud in cloudsForRemoval)
            RemoveCloud(cloud);
        cloudsForRemoval.Clear();
        prevCameraPosition = cloudCamera.transform.position;
    }

    /// <summary>
    /// Spawn a cloud at a given position
    /// </summary>
    /// <param name="position">The position to spawn the cloud</param>
    public void SpawnCloud(Vector3 position)
    {
        Cloud cloud;
        if (cloudPool.Count > 0)
        {
            cloud = cloudPool.Dequeue();
            cloud.gameObject.SetActive(true);
        }
        else
            cloud = Instantiate(cloudPrefab, transform).GetComponent<Cloud>();
        clouds.Add(cloud);
        float speed = maxCloudSpeed + cloudMaxZPosition / position.z * zSpeedFactor - zSpeedFactor;
        if (windDirection == WindDirection.West)
            speed *= -1;
        cloud.Initialize(position, speed, GetCloudSprite());
    }

    /// <summary>
    /// Remove a given cloud
    /// </summary>
    /// <param name="cloud">The cloud to be removed</param>
    public void RemoveCloud(Cloud cloud)
    {
        clouds.Remove(cloud);
        if (cloudPool.Count == cloudPoolLimit)
            Destroy(cloud.gameObject);
        else
        {
            cloudPool.Enqueue(cloud);
            cloud.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Get a random cloud sprite
    /// </summary>
    /// <returns>Returns a randomly selected cloud sprite from the list</returns>
    public Sprite GetCloudSprite()
    {
        int index = Random.Range(0, cloudSprites.Count);
        return cloudSprites[index];
    }
}
