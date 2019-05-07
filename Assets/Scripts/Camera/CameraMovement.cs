using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Camera movement script
public class CameraMovement : MonoBehaviour
{
    public float speed;
    public float zoomIncrement;
    public float zoomMin, zoomMax;

    private void Update ()
    {
        // Movement input
        transform.position += new Vector3(
            Input.GetAxis("Horizontal") * Time.deltaTime * speed,
            Input.GetAxis("Vertical") * Time.deltaTime * speed);

        // Zoom input
        float scrollwheelInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollwheelInput != 0f && !Input.GetKey(KeyCode.LeftShift))
            Zoom(scrollwheelInput < 0f ? zoomIncrement : -zoomIncrement);
    }

    /// Zoom method based on given increments between min and max values.
    public void Zoom(float relativeChange)
    {
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize + relativeChange, 
            zoomMin, zoomMax);
    }
}