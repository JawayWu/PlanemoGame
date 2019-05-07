using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    //The grid onto which we add the tasks.
    public Transform grid;

    //The prefab of Task that we are adding.
    public GameObject task;

    //Testing if file input works.
    public TextAsset spawnTask;

    // Start is called before the first frame update
    void Start()
    {
        addTask(spawnTask);
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    //This method adds a task to the task list with the text inside the passed-in text file.
    public void addTask(TextAsset textFile)
    {
        GameObject holder = Instantiate(task, grid, false);

        Task holderTask = holder.GetComponent<Task> ();

        //Access the text of the Text.
        holderTask.Text.text = textFile.text;

    }
}
