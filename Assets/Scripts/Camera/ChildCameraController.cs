using UnityEngine;

public class ChildCameraController : MonoBehaviour
{
    private Camera childCamera;
    private Camera mainCamera;

    private void Awake()
    {
        childCamera = GetComponent<Camera>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        childCamera.orthographicSize = mainCamera.orthographicSize;
    }
}
