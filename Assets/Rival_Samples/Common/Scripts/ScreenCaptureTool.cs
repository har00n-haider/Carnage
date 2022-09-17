using UnityEngine;

// Generate a screenshot and save to disk with the name SomeLevel.png.

public class ScreenCaptureTool : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F6))
        {
            Debug.Log("Captured Screenshot");
            ScreenCapture.CaptureScreenshot("Screenshot.png");
        }
    }
}