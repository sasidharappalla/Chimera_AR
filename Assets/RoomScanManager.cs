using UnityEngine;
using Meta.XR.MRUtilityKit; // Needed to talk to MRUK

public class RoomScanManager : MonoBehaviour
{
    // Connect this to your "SCAN ROOM" Button
    public void StartRoomScan()
    {
        Debug.Log("Launching Room Scanner...");

        // FIX: This method takes 0 arguments. 
        // It just launches the system app and pauses your game.
        OVRScene.RequestSpaceSetup();
    }

    // This Unity function runs automatically when the app resumes 
    // (e.g., when the user finishes scanning and the game un-pauses)
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Debug.Log("App resumed! Checking for new walls...");
            
            // Tell MRUK to reload the scene data from the Quest
            if (MRUK.Instance != null)
            {
                MRUK.Instance.LoadSceneFromDevice();
            }
        }
    }
}