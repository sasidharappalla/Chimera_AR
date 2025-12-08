using UnityEngine;
using System.Collections.Generic;

public class ChimeraPainter : MonoBehaviour
{
    [Header("Assignments")]
    public Transform controllerAnchor; 
    public GameObject decalPrefab;
    public float maxDistance = 2.5f;

    [Header("Visuals")]
    public Material laserMaterial; 
    public Material currentMaterial; 

    [Header("Paintings Gallery")]
    // NEW: Drag your Photo Materials here!
    public Material[] paintingPresets; 
    private int currentPaintingIndex = 0;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip clickClip;
    public AudioClip paintClip;

    // Internal Variables
    private GameObject reticle;
    private LineRenderer laserLine;
    private List<GameObject> stampedObjects = new List<GameObject>();
    
    // Modes
    private bool isContinuousMode = false; 
    private Vector3 lastPaintPosition;     
    
    // Tile Size Variables
    // Sizes: 5cm, 10cm, 20cm, 40cm
    private float[] availableSizes = { 0.05f, 0.1f, 0.2f, 0.4f }; 
    private int currentSizeIndex = 2; 
    private float currentTileSize;    

    void Start()
    {
        // Setup Reticle
        reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        reticle.transform.localScale = Vector3.one * 0.05f;
        Destroy(reticle.GetComponent<Collider>());
        reticle.SetActive(false);

        // Setup Laser
        laserLine = gameObject.AddComponent<LineRenderer>();
        laserLine.useWorldSpace = true;
        laserLine.startWidth = 0.005f;
        laserLine.endWidth = 0.005f;
        if (laserMaterial != null) laserLine.material = laserMaterial;
        
        if (currentMaterial == null) 
            currentMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        currentTileSize = availableSizes[currentSizeIndex];
    }

    void Update()
    {
        // --- LEFT CONTROLLER TOOLS ---
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
            ClearAllPaint();

        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
        {
            isContinuousMode = !isContinuousMode;
            PlayFeedback(isContinuousMode ? 0.5f : 0.1f);
        }

        // --- RIGHT CONTROLLER PAINTING ---
        RaycastHit hit;
        laserLine.SetPosition(0, controllerAnchor.position);

        if (Physics.Raycast(controllerAnchor.position, controllerAnchor.forward, out hit, maxDistance))
        {
            laserLine.SetPosition(1, hit.point);
            
            reticle.SetActive(true);
            reticle.transform.position = hit.point + (hit.normal * 0.02f); 
            reticle.transform.rotation = Quaternion.LookRotation(hit.normal);
            reticle.GetComponent<Renderer>().material.color = currentMaterial.color;
            reticle.transform.localScale = Vector3.one * (currentTileSize * 0.5f);

            // CLICK LOGIC
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                // 1. SCAN BUTTON
                if (hit.collider.CompareTag("UI_Button"))
                {
                    PlayFeedback(0.5f);
                    if (FindFirstObjectByType<RoomScanManager>())
                        FindFirstObjectByType<RoomScanManager>().StartRoomScan();
                    return;
                }
                // 2. STANDARD COLOR BUTTONS
                else if (hit.collider.CompareTag("UI_Material"))
                {
                    MaterialButton btn = hit.collider.GetComponent<MaterialButton>();
                    if (btn != null)
                    {
                        if (btn.buttonSound != null) PlayCustomSound(btn.buttonSound);
                        else PlayFeedback(0.2f);

                        currentMaterial = btn.myMaterial;
                        laserLine.material.color = currentMaterial.color; 
                    }
                    return;
                }
                // 3. SIZE BUTTON
                else if (hit.collider.CompareTag("UI_Size"))
                {
                    ToggleSize();
                    return;
                }
                // 4. NEW: PAINTING TOGGLE BUTTON
                else if (hit.collider.CompareTag("UI_Painting"))
                {
                    TogglePainting();
                    return;
                }
            }

            // PAINTING LOGIC
            bool hittingUI = hit.collider.CompareTag("UI_Button") || hit.collider.CompareTag("UI_Material") || hit.collider.CompareTag("UI_Size") || hit.collider.CompareTag("UI_Painting");

            if (isContinuousMode)
            {
                if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                {
                    if (hittingUI) return; 

                    if (Vector3.Distance(hit.point, lastPaintPosition) > (currentTileSize * 0.5f))
                    {
                        Paint(hit.point, hit.normal);
                        lastPaintPosition = hit.point; 
                    }
                }
            }
            else
            {
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                {
                    if (!hittingUI) Paint(hit.point, hit.normal);
                }
            }
        }
        else
        {
            laserLine.SetPosition(1, controllerAnchor.position + (controllerAnchor.forward * maxDistance));
            reticle.SetActive(false);
        }

        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
            UndoLast();
    }

    void ToggleSize()
    {
        PlayFeedback(0.5f);
        currentSizeIndex++;
        if (currentSizeIndex >= availableSizes.Length) currentSizeIndex = 0;
        currentTileSize = availableSizes[currentSizeIndex];
        Debug.Log("Switched to Size: " + currentTileSize);
    }

    // NEW FUNCTION: Cycle through paintings
    void TogglePainting()
    {
        PlayFeedback(0.5f);
        
        // Safety Check
        if (paintingPresets.Length == 0) return;

        // Move to next painting
        currentPaintingIndex++;
        if (currentPaintingIndex >= paintingPresets.Length) currentPaintingIndex = 0;

        // Apply it
        currentMaterial = paintingPresets[currentPaintingIndex];
        
        // Update Laser color (use white if texture is complex)
        laserLine.material.color = Color.white; 
        
        Debug.Log("Switched Painting: " + currentMaterial.name);
    }

    void Paint(Vector3 position, Vector3 normal)
    {
        float vibe = isContinuousMode ? 0.1f : 1.0f;
        OVRInput.SetControllerVibration(0.1f, vibe, OVRInput.Controller.RTouch);
        
        if (!isContinuousMode && sfxSource != null && paintClip != null) 
            sfxSource.PlayOneShot(paintClip);

        float randomLayer = Random.Range(0.02f, 0.04f);
        Vector3 spawnPos = position + (normal * randomLayer);
        GameObject newPaint = Instantiate(decalPrefab, spawnPos, Quaternion.LookRotation(normal));
        
        newPaint.GetComponent<Renderer>().material = currentMaterial;
        newPaint.transform.localScale = new Vector3(currentTileSize, currentTileSize, 0.001f);

        stampedObjects.Add(newPaint);
    }
    
    // ... (Helpers) ...
    void ClearAllPaint() {
        PlayFeedback(1.0f);
        foreach (GameObject obj in stampedObjects) Destroy(obj);
        stampedObjects.Clear();
    }
    void PlayFeedback(float intensity) {
        OVRInput.SetControllerVibration(0.1f, intensity, OVRInput.Controller.RTouch);
        if (sfxSource != null && clickClip != null) sfxSource.PlayOneShot(clickClip);
    }
    void PlayCustomSound(AudioClip clip) {
        OVRInput.SetControllerVibration(0.1f, 0.2f, OVRInput.Controller.RTouch);
        if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
    }
    void UndoLast() {
        if (stampedObjects.Count > 0) {
            PlayFeedback(0.3f);
            Destroy(stampedObjects[stampedObjects.Count - 1]);
            stampedObjects.RemoveAt(stampedObjects.Count - 1);
        }
    }
}