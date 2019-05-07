using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    //The loading screen itself
    private Image image;

    //The text box we are writing on
    public Text theText;

    //The textFile where you are grabbing your string from.
    public TextAsset textFile;

    //The string array holding your lines of text.
    public string[] textLines;

    //The text we use to increment
    private string text;

    public bool loading = true;

    // Start is called before the first frame update
    void Start()
    {

        if (alreadyLoaded())
        {
            destroyLoading();
        }
        else
        {
            WriteData();
            image = GetComponent<Image>();
            text = textFile.text;
            StartCoroutine(fadeOutImage());
            StartCoroutine(textPerChar(text, 0));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (loading && Input.GetKeyDown(KeyCode.Escape)) {
            destroyLoading();
        }
    }

    void destroyLoading()
    {
        loading = false;
        StopAllCoroutines();
        Destroy(theText);
        Destroy(gameObject);
    }

    IEnumerator fadeOutImage()
    {
        yield return new WaitForSeconds(21.0f);
        float duration = 1.0f;
        float currentTime = 0;

        Destroy(theText);

        //Despawning text box takes 2 seconds
        while (currentTime < duration)
        {
            //ALPHA is the transparency factor, we lerpin boys
            float alpha = Mathf.Lerp(1.0f, 0f, currentTime / duration);
            image.GetComponent<CanvasRenderer>().SetAlpha(alpha);
            currentTime += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);

        yield break;
    }

    IEnumerator fadeOutText() {
        float currentTime = 0;
        float duration = 2f;

        while (currentTime < duration) {
            //ALPHA is the transparency factor, we lerpin boys
            float alpha = Mathf.Lerp(1.0f, 0f, currentTime / duration);
            theText.color = new Color(1, 1, 1, alpha);
            currentTime += Time.deltaTime;
            yield return null;
        }
        
        yield break;
    }

    IEnumerator textPerChar(string text, float delay)
    {
        yield return new WaitForSeconds(delay);
        theText.text = "";

        theText.color = Color.white;

        for (int i = 0; i < text.Length; i++)
        {
            theText.text += text[i];
            yield return new WaitForSeconds(0.0175f);
        }

        if (text != "[10:47:20] Energy restored. Powering on.​​​​​​.​​​​​​.")
        {
            yield return new WaitForSeconds(1.5f);
            StartCoroutine(fadeOutText());
            StartCoroutine(textPerChar("[10:47:20] Energy restored. Powering on.​​​​​​.​​​​​​.", 4f));
        }

        yield break;
    }

    public void WriteData()
    {
        Debug.Log("we should have wrote data");
        StreamWriter sw = new StreamWriter(Application.dataPath + "/loading.txt");
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
            StreamReader sr = new StreamReader(Application.dataPath + "/loading.txt");
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
