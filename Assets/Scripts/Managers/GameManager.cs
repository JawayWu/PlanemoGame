using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
    public static GameManager instance = null;

    #region Unity_functions
    private void Awake() {
        if (instance == null) {
            instance = this;
        } else if (instance != this) {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
    }
    #endregion

    #region scene_transitions
    public void StartGame() {
        DeleteSave();
        SceneManager.LoadScene("SpawnPlanet");
    }

    public void LoadGame() {
        SceneManager.LoadScene("SpawnPlanet");
    }

    public void GoToPlanet(string p) {
        SceneManager.LoadScene(p);
    }

    public void GoToMainMenu() {
        SceneManager.LoadScene("MainMenu");
    }
    #endregion

    #region management_functions
    public void DeleteSave() {
        string[] files = Directory.GetFiles(Application.dataPath);
        foreach (string file in files) {
            if (file.Contains("txt") && !file.Contains("recipes")) File.Delete(file);
        }

        File.Delete(Application.dataPath + "/inventory.txt");
        File.Delete(Application.dataPath + "/loading.txt");
        File.Delete(Application.dataPath + "/textManager.txt");
    }
    #endregion
}
