using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

public class TextBoxManager : MonoBehaviour
{
    //The text box onto which you are writing.
    public GameObject textBox;

    //The loading screen object so we can check when it leaves.
    public GameObject loadingScreen;

    public Text theText;

    //The textFile where you are grabbing your string from.
    public TextAsset textFile;

    //The string array holding your lines of text.
    public string[] textLines;

    //Number to keep track of instances running.
    private int instancesRunning = 0;

    //Your player
    public PlayerMovement player;

    //The original alpha of the textbox is preserved so we can return it.
    private float originalBoxAlpha;

    //The original alpha of the text is preserved so we can return it.
    private float originalTextAlpha;

    //The image of the text box so we can modify it.
    private Image textBoxImage;

    private bool loadedOut = false;

    public bool disabled = false;

    // Start is called before the first frame update
    void Start()
    {
        Scene currentScene = SceneManager.GetActiveScene();

        string sceneName = currentScene.name;

        if (sceneName != "SpawnPlanet")
        {
            disabled = true;
            gameObject.active = false;
        }

        else
        {
            textBoxImage = textBox.GetComponent<Image>();

            originalBoxAlpha = textBoxImage.color.a;

            originalTextAlpha = theText.color.a;

            //textBox.GetComponent<CanvasRenderer>().SetAlpha(0.0f);

            //theText.GetComponent<CanvasRenderer>().SetAlpha(0.0f);

            //textBoxImage.CrossFadeAlpha(0, 2.0f, true);

            if (textFile != null)
            {
                textLines = (textFile.text.Split('\n'));
            }
        }
    }

    //Spawn text only a little after you have spawned in.
    IEnumerator textSpawn()
    {
        WriteData();
        yield return new WaitForSeconds(3.0f);
        StartCoroutine(enableTextBox());
    }

    //Despawn text 5 seconds
    public void textDespawn()
    {
        //If we don't have another co-routine running, we can close the text box.
        if (instancesRunning < 2)
        {
            disableTextBox();
        } 

        //If we do have another co-routine running, we cannot close the text box.
        else
        {
            instancesRunning--;
        }

    }
    // Update is called once per frame
    void Update()
    {
        if (disabled != true)
        {
            theText.text = textLines[0];

            //Take textBox off the screen if you press return
            if (Input.GetKeyDown(KeyCode.Return))
            {
                disableTextBox();
            }

            if ((loadingScreen == null) && (loadedOut == false) && !alreadyLoaded())
            {
                loadedOut = true;
                StartCoroutine(textSpawn());
                return;
            }

            else if (loadingScreen == null)
            {
                return;
            }

            if ((loadingScreen.GetComponent<LoadingScreen>().loading == false) && !alreadyLoaded())
            {
                StartCoroutine(textSpawn());
            }
        }
    }

    //Turn the textbox on.
    IEnumerator enableTextBox()
    {
        textBox.SetActive(true);
        //BRING THE TEXT BOX BACK UP BOYS MAKE IT seeable
        textBox.GetComponent<CanvasRenderer>().SetAlpha(1.0f);
        theText.GetComponent<CanvasRenderer>().SetAlpha(1.0f);
        instancesRunning++;
        //Wait 5 seconds before despawning
        yield return new WaitForSeconds(5.0f);
        textDespawn();
    }

    //Turn the dialogue box off
    public void disableTextBox()
    {

        StartCoroutine(fadeOutText());
        StartCoroutine(fadeOutTextBox());

    }

    //Fade the textbox out when it's done
    IEnumerator fadeOutTextBox()
    {
        float duration = 2.0f;
        float currentTime = 0;

        //Despawning text box takes 2 seconds
        while (currentTime < duration)
        {
            //YA TRIGGERED ANOTHER TEXT BOX! STOP FADING REEEE COME BACK
            if (instancesRunning > 1)
            {
                instancesRunning--;
                yield break;
            }
            //ALPHA is the transparency factor, we lerpin boys
            float alpha = Mathf.Lerp(0.5f, 0f, currentTime / duration);
            textBox.GetComponent<CanvasRenderer>().SetAlpha(alpha);
            currentTime += Time.deltaTime;
            yield return null;
        }
        textBox.SetActive(false);
        instancesRunning--;
        yield break;
    }

    //Fade the text out when it's done
    IEnumerator fadeOutText()
    {
        float duration = 2.0f;
        float currentTime = 0;

        //Fade out over 2 seconds
        while (currentTime < duration)
        {
            //YOU TRIGGERED ANOTHER TEXT BOX STOP FADING REEEE
            if (instancesRunning > 1)
            {
                yield break;
            }
            //LERP THAT SHIT GRADUALLY
            float alpha = Mathf.Lerp(0.5f, 0f, currentTime / duration);
            theText.GetComponent<CanvasRenderer>().SetAlpha(alpha);
            currentTime += Time.deltaTime;
            yield return null;
        }
        yield break;
    }

    //Reload a new text file AND generate a new fresh text box
    public void reload(TextAsset newFile)
    {
        if (newFile != null)
        {
            textLines = new string[1];
            textLines = (newFile.text.Split('\n'));
        }

        StartCoroutine(enableTextBox());
    }

    public void WriteData()
    {
        Debug.Log("we should have wrote data");
        StreamWriter sw = new StreamWriter(Application.dataPath + "/textManager.txt");
        using (sw)
        {
            sw.WriteLine("1");
            sw.Close();
        }
    }

    public bool alreadyLoaded()
    {
        try
        {
            StreamReader sr = new StreamReader(Application.dataPath + "/textManager.txt");
            string line;
            line = sr.ReadLine();
            Debug.Log("we are in the alreadyLoaded method");
            if (line.Equals("1"))
            {
                sr.Close();
                return true;
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }
}
