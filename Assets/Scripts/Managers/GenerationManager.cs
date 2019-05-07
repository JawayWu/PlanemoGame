using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

/// <summary>
/// Handles the main generation control of the world.
/// </summary>
public class GenerationManager : MonoBehaviour// : Singleton<GenerationManager>
{
    public static GenerationManager Instance;

    [Header("General Components")]
    public SliderData surfaceData;
    public SliderDataCustomizer defaultBlock;
    [NonSerialized]
    public SliderDataCustomizer defaultBlockBack;
    public Camera camera;
    [Header("World Settings")]
    [SerializeField]
    public int seed;
    [NonSerialized]
    public int chunkSize = 32;
    public int worldWidth;
    public int worldHeight;
    private bool pauseTime;
    /// <summary>
    /// The time the sun will rise, used for setting the color of the ambient lighting material 
    /// </summary>
    public float SunriseTime = 6;
    [HideInInspector]
    /// <summary>
    /// The time the sun will set, used for setting the color of the ambient lighting material 
    /// </summary>
    public float SunsetTime = 18;
    public float MoonriseTime = 18;
    [HideInInspector]
    /// <summary>
    /// The time the sun will set, used for setting the color of the ambient lighting material 
    /// </summary>
    public float MoonsetTime = 6;
    [NonSerialized]
    public int[] surfaceHeights;
    [NonSerialized]
    public int[] stoneHeights;
    [NonSerialized]
    public int stoneDepth;
    [NonSerialized]
    public List<ScriptableObject> scriptableObjects;

    [Space]
    [Range(0f, 1f)]
    private readonly float surfaceHeightPosition = 0.6f;
    private readonly float backLayerShadowFactor = 0.6f;
    private int surfaceHeightAverage;
    private Vector2 perlinOffset;
    private readonly float perlinOffsetMax = 10000f;
    private float perlinAddition;

    [NonSerialized]
    public int[] treeHeights;
    private float timeOfDay = 12;
    /// <summary>
    /// The current time of day
    /// </summary>
    int lightUpdate = 7;
    private LightingManager lightingManager;
    public float TimeOfDay
    {
        get { return timeOfDay; }
    }
    private int timeFactor = 100;
    /// <summary>
    /// The factor used to determine how fast time will go by in the game (a time factor of 1 is realtime)
    /// </summary>
    public int TimeFactor
    {
        get { return timeFactor; }
    }
    private int dayCount;
    /// <summary>
    /// The amount of days that have gone by
    /// </summary>
    public int DayCount
    {
        get { return dayCount; }
    }
    /// <summary>
    /// Actions invoked when the day count changes
    /// </summary>
    /// <param name="dayCount">The current day count</param>
    public delegate void DayCycle(int dayCount);
    /// <summary>
    /// Event called when a new day starts
    /// </summary>
    public static event DayCycle OnNewDay;
    Color dayColor;
    Color nightColor;
    [SerializeField]
    int typeOfWorld;
    private void Awake() {
        Instance = this;
    }

    private void Start()
    {
        if (ChunkData.seed != -1) {
            SetSeed(ChunkData.seed);
        }

        scriptableObjects = Resources.LoadAll<ScriptableObject>("Scriptable Objects/" + SceneManager.GetActiveScene().name).ToList();

        if (typeOfWorld == 0)
        {
            dayColor = new Color(0, 175 / 255f, 1);
            nightColor = new Color(0, 0, 20 / 255f);
        }
        if (typeOfWorld == 1)
        {
            dayColor = new Color(204/255f, 218 / 255f, 238/255f);
            nightColor = new Color(0, 0, 20 / 255f);
        }
        if (typeOfWorld == 2)
        {
            dayColor = new Color(1, 160 / 255f, 0);
            nightColor = new Color(0, 0, 20 / 255f);
        }
        Initialize();
    }

    private void Update()
    {
        if (!pauseTime)
        {
            timeOfDay += (Time.deltaTime / 3600f) * timeFactor;

            if (timeOfDay >= 24)
            {
                timeOfDay = 0;
                dayCount++;
                if (OnNewDay != null)
                    OnNewDay(dayCount);
            }

            float lightStrength = Mathf.Max(Mathf.Sin((timeOfDay - 6) / 12f * Mathf.PI), 0);
            camera.backgroundColor = Color.Lerp(nightColor, dayColor, lightStrength);
        }
        if (timeOfDay > lightUpdate && timeOfDay < lightUpdate + 20)
        {
            lightUpdate = (lightUpdate + 1) % 24;
            ChunkLoadManager chunkLoadManager = ChunkLoadManager.Instance;
            foreach (Chunk chunk in chunkLoadManager.GetAllChunks())
            {
                foreach (LightSource light in chunk.AmbientLightSources)
                {
                }
            }
        }
    }
    /// <summary>
    /// Starts a new world generation process.
    /// </summary>
    public void Initialize()
    {
        surfaceHeights = new int[worldWidth];
        stoneHeights = new int[worldWidth];
        surfaceHeightAverage = (int)(worldHeight * surfaceHeightPosition);
        treeHeights = new int[worldWidth];
        chunkSize = (int)ChunkLoadManager.Instance.chunkData.GetSliderData(SliderData.SliderField.CHUNK_SIZE);
        GenerateWorldBase();
    }


    /// <summary>
    /// Changes the seed of the game to a new seed.
    /// </summary>
    /// <param name="newSeed"></param>
    public void SetSeed(int newSeed = -1)
    {
        seed = (newSeed == -1) ? (int)System.DateTime.Now.Ticks : newSeed;
        UnityEngine.Random.InitState(seed);
    }


    //spaceship design
    private Chunk.TileType[] spaceshipTiletypes = new Chunk.TileType[] {
        Chunk.TileType.AIR, //0
        Chunk.TileType.GLASS, //1
        Chunk.TileType.STEEL_PLATE, //2
        Chunk.TileType.HATCH, //3
        Chunk.TileType.WING, //4
        Chunk.TileType.ENGINE, //5
        Chunk.TileType.LANDING_GEAR, //6
        Chunk.TileType.AIR, //7 where light sources go
    };
    public int[,] spaceship = new int[,] {
        {0, 0, 7, 7, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 7, 4, 4, 4, 4, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 7, 4, 4, 4, 4, 4, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 7, 4, 4, 4, 4, 4, 7, 7, 7, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 7, 2, 2, 4, 4, 4, 4, 4, 2, 1, 1, 1, 7, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 7, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        {7, 5, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0},
        {7, 5, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 7, 7, 7, 0, 0, 0, 0, 0},
        {0, 7, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 7, 7, 0, 0, 0},
        {7, 5, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 7, 7, 0},
        {7, 5, 2, 2, 2, 2, 4, 4, 4, 4, 4, 4, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 7},
        {0, 7, 2, 2, 2, 4, 4, 4, 4, 4, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2},
        {0, 0, 7, 4, 4, 4, 4, 4, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 7},
        {0, 7, 4, 4, 4, 4, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 7, 7, 7, 7, 7, 0},
    };
    /*
    public int[,] spaceshipBroken = new int[,] {
        {0, 0, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 7, 4, 4, 7, 0, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 7, 4, 4, 7, 4, 4, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 7, 4, 7, 7, 4, 4, 7, 7, 7, 0, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 7, 2, 2, 4, 4, 4, 4, 4, 2, 1, 7, 1, 7, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 7, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 7, 1, 1, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 7, 7, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 7, 7, 1, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0},
        {7, 5, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 7, 7, 7, 0, 0, 0, 0, 0},
        {0, 7, 7, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 7, 7, 7, 0, 0, 0},
        {7, 5, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 7, 7, 7, 2, 2, 7, 0, 0},
        {7, 5, 2, 2, 2, 2, 4, 4, 4, 4, 4, 4, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 7, 2, 2, 2, 2, 7, 7},
        {0, 7, 7, 7, 2, 4, 4, 4, 4, 4, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2},
        {0, 0, 7, 4, 4, 7, 4, 4, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 7, 2, 7},
        {0, 0, 0, 7, 7, 7, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 7, 7, 7, 0, 7, 0},
    };
    */
    public int[,] spaceshipBroken = new int[,] {
        {0, 0, 7, 7, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 7, 4, 4, 4, 4, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 7, 4, 4, 4, 4, 4, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 0, 7, 4, 4, 4, 4, 4, 7, 7, 7, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 0, 7, 2, 2, 4, 4, 4, 4, 4, 2, 1, 1, 1, 7, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        {0, 7, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
        {7, 5, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 7, 7, 0, 0, 0, 0, 0, 0, 0, 0},
        {7, 5, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 7, 7, 7, 0, 0, 0, 0, 0},
        {0, 7, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 7, 7, 0, 0, 0},
        {7, 5, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 7, 7, 0},
        {7, 5, 2, 2, 2, 2, 4, 4, 4, 4, 4, 4, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 7},
        {0, 7, 2, 2, 2, 4, 4, 4, 4, 4, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2},
        {0, 0, 7, 4, 4, 4, 4, 4, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 7},
        {0, 7, 4, 4, 4, 4, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 7, 7, 7, 7, 7, 0},
    };
    public Vector3Int spaceshipPos;


    /// <summary>
    /// Generates surface height data based on the current seed.
    /// </summary>
    public void GenerateWorldBase()
    {
        SetSeed(seed);
        perlinOffset = new Vector2(
            UnityEngine.Random.Range(0f, perlinOffsetMax),
            UnityEngine.Random.Range(0f, perlinOffsetMax));
        perlinAddition = 0;

        //Tree generation
        int minHeight = 8, maxHeight = 11;
        int minGap = 2; //minimum horizontal gap between two trees
        int lastTree = -1;

        int index = 0;
        while (true)
        {
            // Stop if out of bounds
            if (index < 0 || index >= worldWidth)
            {
                break;
            }

            // Sample
            float noiseX = perlinOffset.x + perlinAddition;
            float noiseY = perlinOffset.y + perlinAddition;

            /* Sets an average height to continue on and manipulate this height with additional Perlin noise for hills.
             * NOTE: PerlinNoise may return values higher than 1f sometimes as stated in the Unity documentation, so we 
             * have to compensate slightly for an approximate average height with Perlin noise. */
            surfaceHeightAverage += (int)((Mathf.Clamp(Mathf.PerlinNoise(noiseX, noiseY), 0f, 1f) - 0.475f) *
                surfaceData.GetSliderData(SliderData.SliderField.SURFACE_AVG_HEIGHT_MULTIPLIER));
            surfaceHeights[index] = surfaceHeightAverage + (int)(Mathf.PerlinNoise(-noiseX, -noiseY) *
                surfaceData.GetSliderData(SliderData.SliderField.SURFACE_HEIGHT_MULTIPLIER));
            perlinAddition += surfaceData.GetSliderData(SliderData.SliderField.SURFACE_PERLIN_SPEED);

            //Generate trees
            if (index - minGap > lastTree)
            {
                int treeSpawn = Random.Range(0, 10);
                if (treeSpawn == 0)
                {
                    treeHeights[index] = Random.Range(minHeight, maxHeight + 1);
                    lastTree = index;
                }
            }

            index++;
        }

        //Rounding world loop
        for (int i = 0; i < 10; i++)
        {
            int x = (worldWidth - 5 + i) % worldWidth;
            int y = surfaceHeights[worldWidth - 6] + (surfaceHeights[5] - surfaceHeights[worldWidth - 6]) * i / 11;
            surfaceHeights[x] = y;
        }

        //flattening out land for spaceship
        for (int c = 0; c < worldWidth / chunkSize; c++)
        {
            if (c * chunkSize >= worldWidth / 2 && c * chunkSize < worldWidth / 2 + chunkSize)
            {
                int sw = spaceship.GetLength(1), sh = spaceship.GetLength(0); //spaceship width/height

                int ground = (int)2e9;
                for (int h = chunkSize / 2 - sw / 2; h < chunkSize / 2 + sw / 2; h++)
                {
                    ground = Mathf.Min(ground, surfaceHeights[c * chunkSize + h]);
                }

                //flatten
                for (int h = chunkSize / 2 - sw / 2; h < chunkSize / 2 + sw / 2; h++)
                {
                    surfaceHeights[c * chunkSize + h] = ground;
                }

                //remove trees nearby
                for (int h = chunkSize / 2 - sw / 2 - 5; h < chunkSize / 2 + sw / 2 + 5; h++)
                {
                    treeHeights[c * chunkSize + h] = 0;
                }

                //rounding
                int heightLeft = surfaceHeights[c * chunkSize + chunkSize / 2 - sw / 2 - 6],
                    heightRight = surfaceHeights[c * chunkSize + chunkSize / 2 - sw / 2];
                for (int h = 1; h <= 5; h++)
                {
                    int x = c * chunkSize + chunkSize / 2 - sw / 2 - 6 + h;
                    surfaceHeights[x] = heightLeft + h * (heightRight - heightLeft) / 6;
                }
                heightLeft = surfaceHeights[c * chunkSize + chunkSize / 2 + sw / 2 - 1];
                heightRight = surfaceHeights[c * chunkSize + chunkSize / 2 + sw / 2 + 5];
                for (int h = 1; h <= 5; h++)
                {
                    int x = c * chunkSize + chunkSize / 2 + sw / 2 - 1 + h;
                    surfaceHeights[x] = heightLeft + h * (heightRight - heightLeft) / 6;
                }
            }
        }
    }


    /// <summary>
    /// A quick check whether the given PerlinNoise parameters exceeds the given threshold.
    /// Used to check whether a type of ore can spawn depending on the perlin height for example.
    /// NOTE: Double sampling is used to avoid straight line syndrome on certain Perlin coordinates.
    /// </summary>
    /// <param name="tilePosition"></param>
    /// <param name="perlinSpeed"></param>
    /// <param name="perlinLevel"></param>
    /// <returns></returns>
    public bool CheckPerlinLevel(Vector3Int tilePosition, float perlinSpeed, float perlinLevel)
    {
        return (Mathf.PerlinNoise(
                    perlinOffset.x + tilePosition.x * perlinSpeed,
                    perlinOffset.y + tilePosition.y * perlinSpeed) +
                Mathf.PerlinNoise(
                    perlinOffset.x - tilePosition.x * perlinSpeed,
                    perlinOffset.y - tilePosition.y * perlinSpeed)) / 2f >= perlinLevel;
    }


    /// <summary>
    /// Uses multiple CheckPerlinLevel calls to extensively check whether a certain type of ore or
    /// block can be spawned.
    /// </summary>
    /// <param name="tilePosition"></param>
    /// <param name="depthMin"></param>
    /// <param name="depthMax"></param>
    /// <param name="perlinSpeed"></param>
    /// <param name="perlinLevel"></param>
    /// <param name="zonePerlinSpeed"></param>
    /// <param name="zonePerlinLevel"></param>
    /// <param name="mapPerlinSpeed"></param>
    /// <param name="mapPerlinLevel"></param>
    /// <returns></returns>
    public bool CheckPerlinEligibility(Vector3Int tilePosition, float depthMin, float depthMax, float perlinSpeed, float perlinLevel,
        float zonePerlinSpeed = 0f, float zonePerlinLevel = 0f, float mapPerlinSpeed = 0f, float mapPerlinLevel = 0f)
    {
        if (depthMin != -1f && depthMax != -1f)
        {
            int depth = surfaceHeights[tilePosition.x] - tilePosition.y;
            if (!(depth >= depthMin && depth < depthMax))
                return false;
        }

        if ((mapPerlinSpeed == 0f && mapPerlinLevel == 0f) ||
            CheckPerlinLevel(tilePosition, mapPerlinSpeed, mapPerlinLevel))
        {
            if ((zonePerlinSpeed == 0f && zonePerlinLevel == 0f) ||
                CheckPerlinLevel(tilePosition, zonePerlinSpeed, zonePerlinLevel))
            {
                if (perlinSpeed == 0f && perlinLevel == 0f)
                    return false;

                return (CheckPerlinLevel(tilePosition, perlinSpeed, perlinLevel));
            }
        }
        return false;
    }


    /// <summary>
    /// Create a brand new chunk using Perlin data combined with the given seed.
    /// </summary>
    /// <param name="chunkX"></param>
    /// <param name="chunkY"></param>
    /// <param name="mapFront"></param>
    /// <param name="mapBack"></param>
    /// <param name="generateCollisionMap"></param>
    /// <param name="smoothLoading"></param>
    /// <returns></returns>
    public IEnumerator GenerateChunk(Chunk chunk)
    {
        // Arrays to store block data
        SliderDataCustomizer[,] dataFront = new SliderDataCustomizer[chunkSize, chunkSize],
                                dataBack = new SliderDataCustomizer[chunkSize, chunkSize];

        int modX = (((chunk.Position.x % worldWidth) + worldWidth) % worldWidth) / chunkSize;
        Tuple<int, int> tuplePos = new Tuple<int, int>(modX, chunk.chunkPosition.y);
        bool tiletypesProvided = ChunkData.frontTiletypes.ContainsKey(tuplePos);

        if (!tiletypesProvided)
        {
            for (int v = 0; v < chunkSize; v++)
            {
                for (int h = 0; h < chunkSize; h++)
                {
                    int newH = ((chunk.Position.x + h) % worldWidth + worldWidth) % worldWidth;
                    Vector3Int tilePosition = new Vector3Int(newH, chunk.Position.y + v, 0);

                    if ((tilePosition.x < 0 || tilePosition.x >= worldWidth) ||
                        (tilePosition.y < 0 || tilePosition.y >= worldHeight))
                        continue;

                    if (tilePosition.y <= surfaceHeights[tilePosition.x])
                    {
                        // Start with the default block
                        dataFront[v, h] = defaultBlock;
                        dataBack[v, h] = defaultBlock;
                        // Loop through the blocks and overwrite the default block if that block can be spawned
                        for (int i = 0; i < scriptableObjects.Count; i++)
                        {
                            SliderDataCustomizer block = scriptableObjects[i] as SliderDataCustomizer;
                            if (block != defaultBlock)
                            {
                                if (CheckPerlinEligibility(tilePosition,
                                    block.GetSliderData(SliderData.SliderField.DEPTH_MIN),
                                    block.GetSliderData(SliderData.SliderField.DEPTH_MAX),
                                    block.GetSliderData(SliderData.SliderField.PERLIN_SPEED),
                                    block.GetSliderData(SliderData.SliderField.PERLIN_LEVEL),
                                    block.GetSliderData(SliderData.SliderField.ZONE_PERLIN_SPEED),
                                    block.GetSliderData(SliderData.SliderField.ZONE_PERLIN_LEVEL),
                                    block.GetSliderData(SliderData.SliderField.MAP_PERLIN_SPEED),
                                    block.GetSliderData(SliderData.SliderField.MAP_PERLIN_LEVEL)))
                                {
                                    dataFront[v, h] = block;
                                    break;
                                }
                            }
                        }
                        for (int i = 1; i < scriptableObjects.Count; i++)
                        {
                            SliderDataCustomizer blockBack = scriptableObjects[i] as SliderDataCustomizer;
                            if (blockBack != defaultBlock)
                            {
                                if (CheckPerlinEligibility(tilePosition,
                                    blockBack.GetSliderData(SliderData.SliderField.DEPTH_MIN),
                                    blockBack.GetSliderData(SliderData.SliderField.DEPTH_MAX),
                                    blockBack.GetSliderData(SliderData.SliderField.PERLIN_SPEED),
                                    blockBack.GetSliderData(SliderData.SliderField.PERLIN_LEVEL),
                                    blockBack.GetSliderData(SliderData.SliderField.ZONE_PERLIN_SPEED),
                                    blockBack.GetSliderData(SliderData.SliderField.ZONE_PERLIN_LEVEL),
                                    blockBack.GetSliderData(SliderData.SliderField.MAP_PERLIN_SPEED),
                                    blockBack.GetSliderData(SliderData.SliderField.MAP_PERLIN_LEVEL)))
                                {
                                    dataBack[v, h] = blockBack;
                                    break;
                                }
                            }
                        }

                        // Change everything below y=64 to bedrock
                        if (tilePosition.y < 64)
                        {
                            dataFront[v, h] = dataBack[v, h] = ItemData.data[ItemData.GetID("Bedrock")];
                        }
                    }
                }
            }

            for (int h = 0; h < chunkSize; h++)
            {
                for (int v = chunkSize - 1; v >= 0; v--)
                {
                    if (!dataBack[v, h]) continue;
                    bool frontOres = dataFront[v, h].itemName == "Coal Ore" ||
                                    dataFront[v, h].itemName == "Copper Ore" ||
                                    dataFront[v, h].itemName == "Iron Ore";
                    bool backOres = (dataBack[v, h].itemName == "Coal Ore" ||
                                    dataBack[v, h].itemName == "Copper Ore" ||
                                    dataBack[v, h].itemName == "Iron Ore") &&
                                    dataFront[v, h].itemName == "Air";
                    if (backOres)
                    {
                        int stoneChance = Random.Range(0, 2);
                        if (stoneChance == 1)
                        {
                            dataBack[v, h] = ItemData.data[ItemData.GetID("Stone")];
                        }
                    }
                    if (frontOres)
                    {
                        int frontOrBack = Random.Range(0, 2);
                        if (frontOrBack == 0)
                        {
                            int stoneChance = Random.Range(0, 2);
                            if (stoneChance == 1)
                            {
                                dataBack[v, h] = dataFront[v, h];
                                dataFront[v, h] = ItemData.data[ItemData.GetID("Stone")];
                            }
                        }
                        else if (frontOrBack == 1)
                        {
                            int stoneChance = Random.Range(0, 2);
                            if (stoneChance == 1)
                            {
                                dataFront[v, h] = dataBack[v, h];
                                dataBack[v, h] = ItemData.data[ItemData.GetID("Stone")];
                            }
                        }
                    }
                    if (dataFront[v, h].itemName == "Grass" || dataBack[v, h].itemName == "Grass")
                    {
                        bool lowerSandBack, lowerSandFront;
                        if (v > 0)
                        {
                            lowerSandBack = dataBack[v - 1, h].itemName == "Sand";
                            lowerSandFront = dataFront[v - 1, h].itemName == "Sand";
                        }
                        else
                        {
                            int newH = ((chunk.Position.x + h) % worldWidth + worldWidth) % worldWidth;
                            Vector3Int tilePosition = new Vector3Int(newH, chunk.Position.y + v - 1, 0);

                            SliderDataCustomizer sand = ItemData.data[ItemData.GetID("Sand")];
                            lowerSandBack = lowerSandFront = CheckPerlinEligibility(tilePosition,
                                    sand.GetSliderData(SliderData.SliderField.DEPTH_MIN),
                                    sand.GetSliderData(SliderData.SliderField.DEPTH_MAX),
                                    sand.GetSliderData(SliderData.SliderField.PERLIN_SPEED),
                                    sand.GetSliderData(SliderData.SliderField.PERLIN_LEVEL),
                                    sand.GetSliderData(SliderData.SliderField.ZONE_PERLIN_SPEED),
                                    sand.GetSliderData(SliderData.SliderField.ZONE_PERLIN_LEVEL),
                                    sand.GetSliderData(SliderData.SliderField.MAP_PERLIN_SPEED),
                                    sand.GetSliderData(SliderData.SliderField.MAP_PERLIN_LEVEL));
                        }

                        if (lowerSandBack)
                        {
                            dataBack[v, h] = ItemData.data[ItemData.GetID("Sand")];
                        }
                        if (dataFront[v, h].itemName == "Grass")
                        {
                            if (lowerSandFront || lowerSandBack || dataBack[v, h].itemName == "Sand")
                            {
                                dataFront[v, h] = ItemData.data[ItemData.GetID("Sand")];
                            }
                            /*
                            //Don't spawn trees on edges of chunks,
                            //too close to the top of chunks,
                            //on blocks with no front layer,
                            //or within `minGap` blocks of the last tree
                            if (h - minGap <= lastTree
                                || v + maxHeight >= chunkSize
                                || dataFront[v, h].itemName == "Air"
                                || h == 0
                                || h == chunkSize - 1) break;

                            int treeSpawn = Random.Range(0, 10);
                            if (treeSpawn <= 1)
                            {
                                treeType = Random.Range(0, 10);
                                int treeHeight = Random.Range(minHeight, maxHeight);
                                int leafHeight = treeHeight / 2;
                                if (dataFront[v, h].itemName == "Sand" || dataBack[v, h].itemName == "Sand")
                                {
                                    treeType = 11;
                                }
                                if (treeType == 0)
                                {
                                    leafType = ItemData.data[ItemData.GetID("LeafApple")];
                                }
                                else if (treeType == 1)
                                {
                                    leafType = ItemData.data[ItemData.GetID("LeafOrange")];
                                }
                                else if (treeType == 11)
                                {
                                    logType = ItemData.data[ItemData.GetID("Cactus")];
                                    treeHeight = (treeHeight + 1) / 2;
                                    leafHeight = (treeHeight + 1) / 2;
                                }
                                int leafDistance = 1;
                                for (int treeModifier = v + 1; treeModifier <= v + treeHeight; treeModifier++)
                                {
                                    dataFront[treeModifier, h] = ItemData.data[ItemData.GetID("Air")];
                                    dataBack[treeModifier, h] = logType;
                                    if ((treeModifier > v + leafHeight) && logType == ItemData.data[ItemData.GetID("Log")])
                                    {
                                        dataFront[treeModifier, h] = leafType;
                                        dataFront[treeModifier, h + leafDistance] = ItemData.data[ItemData.GetID("Air")];
                                        dataBack[treeModifier, h + leafDistance] = leafType;
                                        dataFront[treeModifier, h - leafDistance] = ItemData.data[ItemData.GetID("Air")];
                                        dataBack[treeModifier, h - leafDistance] = leafType;
                                        if (treeModifier == v + treeHeight)
                                        {
                                            dataFront[treeModifier + leafDistance, h] = ItemData.data[ItemData.GetID("Air")];
                                            dataBack[treeModifier + leafDistance, h] = leafType;
                                        }
                                    }
                                    if ((treeModifier > v + leafHeight) && (logType == ItemData.data[ItemData.GetID("Cactus")]))
                                    {
                                        int[] armChances = new int[2];
                                        for (int c = 0; c < armChances.Length; c++)
                                        {
                                            armChances[c] = Random.Range(0, 10);
                                            if (armChances[c] < 3)
                                            {
                                                //if (dataBack[treeModifier, h + leafDistance] == ItemData.data[ItemData.GetID("Air")])
                                                {
                                                    dataBack[treeModifier, h + leafDistance] = ItemData.data[ItemData.GetID("Cactus")];
                                                    dataFront[treeModifier, h + leafDistance] = ItemData.data[ItemData.GetID("Air")];
                                                }
                                            }
                                            else if (armChances[c] >= 3 && armChances[c] < 6)
                                            {
                                                //if (dataBack[treeModifier, h - leafDistance] == ItemData.data[ItemData.GetID("Air")])
                                                {
                                                    dataBack[treeModifier, h - leafDistance] = ItemData.data[ItemData.GetID("Cactus")];
                                                    dataFront[treeModifier, h - leafDistance] = ItemData.data[ItemData.GetID("Air")];
                                                }
                                            }
                                            else if (armChances[c] >= 6 && armChances[c] < 10)
                                            {
                                                //if (dataBack[treeModifier + 1, h] == ItemData.data[ItemData.GetID("Air")])
                                                {
                                                    dataBack[treeModifier + 1, h] = ItemData.data[ItemData.GetID("Cactus")];
                                                    dataFront[treeModifier + 1, h] = ItemData.data[ItemData.GetID("Air")];
                                                }
                                            }
                                        }
                                    }
                                }

                                break;
                            }
                            */
                        }
                    }
                }
            }

            //Attempt to generate trees
            if (typeOfWorld != 1 && typeOfWorld != 2)
            {
                SliderDataCustomizer logType = ItemData.data[ItemData.GetID("Log")];
                SliderDataCustomizer leafType = ItemData.data[ItemData.GetID("Leaf")];
                for (int h = 0; h < chunkSize; h++)
                {
                    int x = ChunkData.Global(chunk.Position + new Vector3Int(h, 0, 0)).x;
                    int x_left = ChunkData.Global(chunk.Position + new Vector3Int(h - 1, 0, 0)).x;
                    int x_right = ChunkData.Global(chunk.Position + new Vector3Int(h + 1, 0, 0)).x;

                    for (int v = 0; v < chunkSize; v++)
                    {
                        int y = chunk.Position.y + v;

                        //trunk
                        if (treeHeights[x] > 0)
                        {
                            int treeHeight = surfaceHeights[x] + treeHeights[x];
                            int leafHeight = surfaceHeights[x] + (treeHeights[x] + 1) / 2 + 1;
                            if (surfaceHeights[x] < y && y < treeHeight)
                            {
                                dataBack[v, h] = logType;
                                if (leafHeight <= y) dataFront[v, h] = leafType;
                            }
                            else if (y == treeHeight)
                            {
                                dataBack[v, h] = leafType;
                            }
                        }
                        //side leaves
                        if (treeHeights[x_left] > 0)
                        {
                            int treeHeight = surfaceHeights[x_left] + treeHeights[x_left];
                            int leafHeight = surfaceHeights[x_left] + (treeHeights[x_left] + 1) / 2 + 1;
                            if (leafHeight <= y && y < treeHeight)
                            {
                                dataBack[v, h] = leafType;
                            }
                        }
                        else if (treeHeights[x_right] > 0)
                        {
                            int treeHeight = surfaceHeights[x_right] + treeHeights[x_right];
                            int leafHeight = surfaceHeights[x_right] + (treeHeights[x_right] + 1) / 2 + 1;
                            if (leafHeight <= y && y < treeHeight)
                            {
                                dataBack[v, h] = leafType;
                            }
                        }
                    }
                }
            }
            //Spawning in spaceship
            Vector3Int globalPos = ChunkData.Global(chunk.Position);
            //check that we're on a middle chunk
            if (globalPos.x >= worldWidth / 2 && globalPos.x < worldWidth / 2 + chunkSize)
            {
                int sw = spaceship.GetLength(1), sh = spaceship.GetLength(0); //spaceship width/height

                int ground = (int)2e9;
                for (int h = chunkSize / 2 - sw / 2; h < chunkSize / 2 + sw / 2; h++)
                {
                    ground = Mathf.Min(ground, surfaceHeights[globalPos.x + h]);
                }

                //put in spaceship
                for (int r = 0; r < sh; r++)
                {
                    int v = ground + (sh - r) - globalPos.y;
                    if (v < 0 || v >= chunkSize) continue;

                    for (int c = 0; c < sw; c++)
                    {
                        int h = chunkSize / 2 - sw / 2 + c;
                        Vector3Int tilePos = new Vector3Int(chunk.Position.x + h, chunk.Position.y + v, 0);

                        dataBack[v, h] = ItemData.data[ItemData.GetID("Air")];
                        dataFront[v, h] = ItemData.data[ItemData.GetID(spaceshipTiletypes[spaceshipBroken[r, c]])];

                        //chunk.SetChunkTileColor(tilePos, Color.white);

                        if (r == 0 && c == 0)
                        {
                            spaceshipPos = tilePos;
                        }
                    }
                }
            }
        }
        else
        {
            //Set data using given TileType arrays
            for (int v = 0; v < chunkSize; v++)
            {
                for (int h = 0; h < chunkSize; h++)
                {
                    dataFront[v, h] = ItemData.data[ItemData.GetID(chunk.typeFrontBlocks[h, v])];
                    dataBack[v, h] = ItemData.data[ItemData.GetID(chunk.typeBackBlocks[h, v])];
                }
            }

            //Checking for spaceship
            Vector3Int globalPos = ChunkData.Global(chunk.Position);
            //check that we're on a middle chunk
            if (globalPos.x >= worldWidth / 2 && globalPos.x < worldWidth / 2 + chunkSize) {
                int sw = spaceship.GetLength(1), sh = spaceship.GetLength(0); //spaceship width/height

                int ground = (int)2e9;
                for (int h = chunkSize / 2 - sw / 2; h < chunkSize / 2 + sw / 2; h++) {
                    ground = Mathf.Min(ground, surfaceHeights[globalPos.x + h]);
                }

                //put in spaceship
                for (int r = 0; r < sh; r++) {
                    int v = ground + (sh - r) - globalPos.y;
                    if (v < 0 || v >= chunkSize) continue;

                    for (int c = 0; c < sw; c++) {
                        int h = chunkSize / 2 - sw / 2 + c;
                        Vector3Int tilePos = new Vector3Int(chunk.Position.x + h, chunk.Position.y + v, 0);

                        if (r == 0 && c == 0) {
                            spaceshipPos = tilePos;
                        }
                    }
                }
            }
        }

        // Set tile and tiletype values
        for (int v = 0; v < chunkSize; v++)
        {
            for (int h = 0; h < chunkSize; h++)
            {
                Vector3Int tilePosition = new Vector3Int(chunk.Position.x + h, chunk.Position.y + v, 0);

                if (dataFront[v, h] != null)
                {
                    // Set the desired tile
                    chunk.SetChunkTile(tilePosition, Chunk.TilemapType.FRONT_BLOCKS, dataFront[v, h].itemTile);
                    chunk.SetChunkTileType(tilePosition, Chunk.TilemapType.FRONT_BLOCKS, dataFront[v, h].itemType);

                }
                if (dataBack[v, h] != null)
                {
                    chunk.SetChunkTile(tilePosition, Chunk.TilemapType.BACK_BLOCKS, dataBack[v, h].itemTile);
                    chunk.SetChunkTileType(tilePosition, Chunk.TilemapType.BACK_BLOCKS, dataBack[v, h].itemType);
                }
                if (chunk.GetChunkTileColor(tilePosition) == Color.clear)
                    chunk.SetChunkTileColor(tilePosition, Color.black);
            }
            yield return null;
        }

        // Add chunk to ChunkData
        if (!tiletypesProvided)
        {
            ChunkData.frontTiletypes.Add(tuplePos, chunk.typeFrontBlocks);
            ChunkData.backTiletypes.Add(tuplePos, chunk.typeBackBlocks);
        }

        yield break;
    }

    public bool spaceshipComplete()
    {
        if (spaceshipPos == null)
        {
            return false;
        }

        for (int r = 0; r < spaceship.GetLength(0); r++)
        {
            for (int c = 0; c < spaceship.GetLength(1); c++)
            {
                if (ChunkData.GetTileType(spaceshipPos + new Vector3Int(c, -r, 0), Chunk.TilemapType.FRONT_BLOCKS)
                    != spaceshipTiletypes[spaceship[r, c]])
                {
                    return false;
                }
            }
        }

        return true;
    }
}