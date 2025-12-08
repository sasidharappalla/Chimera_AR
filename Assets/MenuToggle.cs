using UnityEngine;

public class MenuToggle : MonoBehaviour
{
    public GameObject wristCanvas; // Assign your WristPalette here
    private bool isMenuVisible = true;

    void Start()
    {
        // Optional: Hide it at start so the view is clean
        isMenuVisible = false;
        wristCanvas.SetActive(false);
    }

    void Update()
    {
        // Listen for the "Start" or "Menu" button on the Left Controller
        // (Button.Start is usually the small flush button on the left controller)
        if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LTouch) || 
            OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch)) // Button "Y" as backup
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        isMenuVisible = !isMenuVisible; // Flip the boolean
        wristCanvas.SetActive(isMenuVisible); // Show or Hide
    }
}