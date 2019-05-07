using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    #region movement_variables
    [SerializeField]
    [Tooltip("Movement speed")]
    private float movespeed;
    [SerializeField]
    [Tooltip("Initial speed of jump")]
    private float jumpspeed;

    [HideInInspector]
    public bool jumping;
    [HideInInspector]
    public bool crouching;

    [HideInInspector]
    public bool inInventory;
    public Inventory inventory;
    public Chest chest;
    public Furnace furnace;
    public int selected = 0;
    #endregion

    #region loading_screen variables
    private bool waitingForChunk = true;
    public Image loadingImage;
    #endregion

    #region falling_variables
    private bool grounded;
    float timeInAir = 0.0f;
    float distanceDown;
    float lastFrameYVelocity = 0.0f;
    #endregion

    #region health_variables
    public float maxHealth;
    float currHealth;
    //Can change the HP representation later if we want more interesting UI.
    public Slider hpSlider;
    #endregion

    #region control_variables
    //keyboard controls
    private const string
        key_left = "a",
        key_right = "d",
        key_jump_1 = "w",
        key_jump_2 = "space",
        key_crouch = "s",
        toggle_inventory = "e",
        key_break = "q";
    private string[]
        hotbar_slot = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
    #endregion

    #region physics_variables
    [HideInInspector]
    public Rigidbody2D rb;
    [HideInInspector]
    public BoxCollider2D bc;
    private BoxCollider2D feet;
    #endregion

    #region animation_variables
    private Animator anim;
    private Chunk chunk;
    private SpriteRenderer lightedSprite;
    [Header("Objects and Components")]
    [SerializeField]
    private SpriteRenderer graphicSpriteRenderer;
    [SerializeField]
    private Transform armTransform;
    [SerializeField]
    public Transform armWithWeaponTransform;
    [SerializeField]
    public Camera camera;
    private SpriteRenderer heldItem;
    #endregion

    #region Unity_functions
    public void Start()
    {
        jumping = true;
        inInventory = false;
        chest = new Chest();
        currHealth = maxHealth;

        rb = GetComponent<Rigidbody2D>();
        bc = GetComponent<BoxCollider2D>();
        feet = transform.GetChild(0).GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
        lightedSprite = GetComponent<SpriteRenderer>();
        heldItem = armWithWeaponTransform.GetChild(0).GetComponent<SpriteRenderer>();

        int x = Mathf.RoundToInt(transform.position.x);
        while (GenerationManager.Instance.surfaceHeights[x] == 0);
        int y = GenerationManager.Instance.surfaceHeights[x];
        transform.position = new Vector3(x + 0.5f, y + 3f);
    }

    public void Update()
    {
        if (waitingForChunk) {
            int x = Mathf.RoundToInt(transform.position.x);
            int y = Mathf.RoundToInt(transform.position.y - 2);
            if (ChunkLoadManager.Instance.GetChunk(new Vector3Int(x, y, 0)).loading) {
                rb.Sleep();
                return;
            } else if (rb.IsSleeping()) {
                rb.WakeUp();
                waitingForChunk = false;
            }
        }

        if (loadingImage != null)
        {
            return;
        }

        if (GenerationManager.Instance.spaceshipComplete() && Input.GetKeyDown(KeyCode.P)) {
            inventory.WriteData();
            ChunkData.WriteData();
            Debug.Log("World saved");
            SceneManager.LoadScene("SpaceUI");
        }

        //reset horizontal velocity to 0, but keep current vertical velocity
        //terminal velocity of 100 blocks/sec
        Vector2 vel = new Vector2(0, Mathf.Max(rb.velocity.y, -100));
        Vector3Int tilePosition = new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);
        chunk = ChunkLoadManager.Instance.GetChunk(tilePosition);
        if (chunk.GetChunkTileColor(tilePosition) == null
            || transform.position.y > GenerationManager.Instance.surfaceHeights[ChunkData.Global(tilePosition).x] + 2)
        {
            SetPlayerColor(LightingManager.Instance.ambientLightColor);
            lightedSprite.color = LightingManager.Instance.ambientLightColor;
        }
        else
        {
            Color baseColor = new Color(chunk.GetChunkTileColor(tilePosition).r + 0.05f, chunk.GetChunkTileColor(tilePosition).g + 0.05f, chunk.GetChunkTileColor(tilePosition).b + 0.05f);
            SetPlayerColor(baseColor);
        }

        if (Mathf.Abs(vel.y) < 0.1)
        {
            if (lastFrameYVelocity < -40.0)
            {
                takeDamage(-lastFrameYVelocity);
            }
        }

        //Inventory toggle
        if (Input.GetKeyDown(toggle_inventory))
        {
            inInventory = !inInventory;
            inventory.mode = "inventory";
        }
        //Hotbar selection
        if (!inInventory)
        {
            //Scroll controls
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.1)
            {
                selected = (selected + 10) % 11;
            }
            else if (scroll < -0.1)
            {
                selected = (selected + 1) % 11;
            }
            //Manual selection through keyboard
            for (int i = 0; i < 10; i++)
            {
                if (Input.GetKeyDown(hotbar_slot[i]))
                {
                    selected = i;
                }
            }
            if (Input.GetKeyDown(key_break))
            {
                selected = 10;
            }

            if (selected != 10 && inventory.GetItem(3, selected) != null) {
                heldItem.sprite = ItemData.GetTileSprite(inventory.GetItem(3, selected).id);
                heldItem.enabled = true;
            } else {
                heldItem.enabled = false;
            }
        }

        //Disable controls while checking inventory
        if (inInventory)
        {
            rb.velocity = vel;
            if (crouching)
            {
                RaycastHit2D objectAbove = Physics2D.Raycast(transform.position, Vector2.up, 1.7f, LayerMask.GetMask("Default"));
                RaycastHit2D objectAboveLeft = Physics2D.Raycast(transform.position - new Vector3(bc.size.x / 2, 0, 0), Vector2.up, 1.7f, LayerMask.GetMask("Default"));
                RaycastHit2D objectAboveRight = Physics2D.Raycast(transform.position + new Vector3(bc.size.x / 2, 0, 0), Vector2.up, 1.7f, LayerMask.GetMask("Default"));
                if (!objectAbove && !objectAboveLeft && !objectAboveRight)
                {
                    Uncrouch();
                }
            }
            return;
        }

        //horizontal movement
        if (Input.GetKey(key_left))
        {
            vel.x -= movespeed;
        }
        if (Input.GetKey(key_right))
        {
            vel.x += movespeed;
        }

        //jump controls
        //checks that player is not jumping and has around 0 vertical velocity
        if (!jumping && Mathf.Abs(vel.y) < 0.1 && (Input.GetKey(key_jump_1) || Input.GetKey(key_jump_2)))
        {
            jumping = true;
            vel.y += crouching ? jumpspeed / 1.5f : jumpspeed;
        }

        if (Input.GetKeyDown(key_crouch) && !crouching)
        {
            Crouch();
        }

        if (!Input.GetKey(key_crouch) && crouching)
        {
            RaycastHit2D objectAbove = Physics2D.Raycast(transform.position, Vector2.up, 1.7f, LayerMask.GetMask("Default"));
            RaycastHit2D objectAboveLeft = Physics2D.Raycast(transform.position - new Vector3(bc.size.x / 2, 0, 0), Vector2.up, 1.7f, LayerMask.GetMask("Default"));
            RaycastHit2D objectAboveRight = Physics2D.Raycast(transform.position + new Vector3(bc.size.x / 2, 0, 0), Vector2.up, 1.7f, LayerMask.GetMask("Default"));
            if (!objectAbove && !objectAboveLeft && !objectAboveRight)
            {
                Uncrouch();
            }
        }
        if (crouching)
        {
            vel.x /= 2;
        }

        //check if walking into a block, then try to jump/teleport
        if (!jumping && Mathf.Abs(vel.y) < 0.01)
        {
            /*RaycastHit2D blockUp = Physics2D.Raycast(transform.position + new Vector3(0, -bc.size.y) / 2 + Vector3.up * 10,
                Vector2.left, 0.05f, LayerMask.GetMask("Default"));
            */                   
                
            RaycastHit2D blockUp = Physics2D.Raycast(transform.position, Vector2.up, 2.7f, LayerMask.GetMask("Default"));
            if (crouching)
            {
                blockUp = Physics2D.Raycast(transform.position, Vector2.up, 1.7f, LayerMask.GetMask("Default"));
            }

            if (blockUp)
            {
            }
            else if (vel.x < -0.1)
            { 
                RaycastHit2D objectFront = Physics2D.Raycast(transform.position + new Vector3(-bc.size.x, -bc.size.y) / 2,
                    Vector2.left, 0.05f, LayerMask.GetMask("Default"));
                bool objectBlock = false;
                for (int v = 1; v <= 4; v++)
                {
                    RaycastHit2D block = Physics2D.Raycast(transform.position + new Vector3(-bc.size.x, -bc.size.y) / 2 + Vector3.up * v,
                        Vector2.left, 0.05f, LayerMask.GetMask("Default"));
                    objectBlock = objectBlock || block;
                }
                if (objectFront && !objectBlock)
                {
                    jumping = true;
                    StartCoroutine(Teleport(Vector3.up * 1.01f));
                }
            }
            else if (vel.x > 0.1)
            {
                RaycastHit2D objectFront = Physics2D.Raycast(transform.position + new Vector3(bc.size.x, -bc.size.y) / 2,
                    Vector2.right, 0.05f, LayerMask.GetMask("Default"));
                bool objectBlock = false;
                for (int v = 1; v <= 4; v++)
                {
                    RaycastHit2D block = Physics2D.Raycast(transform.position + new Vector3(bc.size.x, -bc.size.y) / 2 + Vector3.up * v,
                        Vector2.right, 0.05f, LayerMask.GetMask("Default"));
                    objectBlock = objectBlock || block;
                }
                if (objectFront && !objectBlock)
                {
                    jumping = true;
                    StartCoroutine(Teleport(Vector3.up * 1.01f));
                }
            }
        }

        //turn player
        if (Camera.main.ScreenToWorldPoint(Input.mousePosition).x < transform.position.x)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            transform.localScale = new Vector3(1, 1, 1);
        }

        if (Mathf.Abs(vel.x) > 0.1 && !jumping)
        {
            anim.Play(crouching ? "CrouchMove" : "Walk");
        }
        else if (jumping)
        {
            if (crouching)
            {
                anim.Play("CrouchStill");
            }
            else
            {
                if (vel.y > jumpspeed * 0.6f)
                {
                    anim.Play("Jump");
                }
                else if (vel.y < -jumpspeed / 2)
                {
                    anim.Play("Idle");
                }
                else
                {
                    anim.Play("Idle");
                }
            }
        }
        else
        {
            anim.Play(crouching ? "CrouchStill" : "Idle");
        }

        rb.velocity = vel;
        lastFrameYVelocity = vel.y;

        //Rotate the arm to point to the position of the cursor
        Vector3 cursorPositionRelativePlayer = Camera.main.ScreenToWorldPoint(Input.mousePosition) - armTransform.position;
        cursorPositionRelativePlayer.Normalize();
        float angle = Mathf.Atan2(cursorPositionRelativePlayer.y, cursorPositionRelativePlayer.x) * Mathf.Rad2Deg;
        if (graphicSpriteRenderer.transform.localScale.x < 0)
            angle += 180;
        armTransform.eulerAngles = new Vector3(0, 0, angle);
        armWithWeaponTransform.eulerAngles = new Vector3(0, 0, angle);
    }
    #endregion

    #region movement_functions
    private IEnumerator Teleport(Vector3 v)
    {
        Vector3 orig = transform.position;
        float elapsedTime = 0, duration = 0.07f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector2.Lerp(orig, orig + v, Mathf.Pow(elapsedTime / duration, 2));
            yield return null;
        }
    }

    //adjust player animation to crouch.
    private void Crouch()
    {
        transform.position += new Vector3(0, -10) / 32;
        bc.size = new Vector2(1.2f, 3.5f - 10f / 16);
        feet.offset = new Vector2(0, -47f) / 32;
        anim.Play("CrouchStill");
        crouching = true;
    }

    //adjust player animation to uncrouch.
    private void Uncrouch()
    {
        transform.position += new Vector3(0, 10) / 32;
        bc.size = new Vector2(1.2f, 3.5f);
        feet.offset = new Vector2(0, -57f) / 32;
        anim.Play("Idle");
        crouching = false;
    }
    #endregion

    #region health_functions

    //Take damage equal to how much you pass in
    public void takeDamage(float value)
    {
        //decrement health
        currHealth -= value;

        //adjust UI to reflect damage
        hpSlider.value = currHealth / maxHealth;
        Debug.Log("Health is now" + currHealth.ToString());
    }

    #endregion

    #region misc_functions
    private void SetPlayerColor(Color c) {
        lightedSprite.color = c;
        armTransform.GetComponent<SpriteRenderer>().color = c;
        armWithWeaponTransform.GetComponent<SpriteRenderer>().color = c;
    }
    #endregion
}
