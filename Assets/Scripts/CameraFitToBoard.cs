using UnityEngine;

public class CameraFitToBoard : MonoBehaviour
{
    public float boardWidth = 2400f;
    public float boardHeight = 1080f;

    void Start()
    {
        Camera cam = GetComponent<Camera>();
        float targetAspect = boardWidth / boardHeight;
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1f)
        {
            cam.orthographicSize = boardHeight / 2f / scaleHeight;
        }
        else
        {
            cam.orthographicSize = boardHeight / 2f;
        }

        // Center camera on board (assuming board center at (0,0))
        cam.transform.position = new Vector3(0, 0, -10);
    }
}
