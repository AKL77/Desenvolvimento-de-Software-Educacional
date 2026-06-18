using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class SequenceZoom : MonoBehaviour
{
    [Header("References")]
    public RectTransform content;
    public Button zoomInButton;
    public Button zoomOutButton;
    public TextMeshProUGUI zoomLabelText;

    [Header("Zoom Levels")]
    public float[] zoomLevels = { 1f, 0.75f, 0.5f };
    private int currentZoomIndex = 0;

    // Pinch tracking
    private float lastPinchDistance = 0f;
    private bool isPinching = false;

    void Start()
    {
        ApplyZoom();
        UpdateButtons();
    }

    void Update()
    {
        HandlePinch();
    }

public void ZoomIn()
{
    Debug.Log("ZoomIn called, current index: " + currentZoomIndex);
    if (currentZoomIndex > 0)
    {
        currentZoomIndex--;
        ApplyZoom();
        UpdateButtons();
    }
}

public void ZoomOut()
{
    Debug.Log("ZoomOut called, current index: " + currentZoomIndex);
    if (currentZoomIndex < zoomLevels.Length - 1)
    {
        currentZoomIndex++;
        ApplyZoom();
        UpdateButtons();
    }
}

    void ApplyZoom()
    {
        float scale = zoomLevels[currentZoomIndex];
        content.localScale = new Vector3(scale, scale, 1f);

        if (zoomLabelText != null)
            zoomLabelText.text = Mathf.RoundToInt(scale * 100f) + "%";
    }

    void UpdateButtons()
    {
        if (zoomInButton != null)
            zoomInButton.interactable = currentZoomIndex > 0;
        if (zoomOutButton != null)
            zoomOutButton.interactable = currentZoomIndex < zoomLevels.Length - 1;
    }

    void HandlePinch()
    {
        if (Touchscreen.current == null) return;

        var touches = Touchscreen.current.touches;
        if (touches.Count < 2) 
        {
            isPinching = false;
            return;
        }

        Vector2 touch0 = touches[0].position.ReadValue();
        Vector2 touch1 = touches[1].position.ReadValue();
        float currentDistance = Vector2.Distance(touch0, touch1);

        if (!isPinching)
        {
            lastPinchDistance = currentDistance;
            isPinching = true;
            return;
        }

        float delta = currentDistance - lastPinchDistance;
        lastPinchDistance = currentDistance;

        // Pinch out = zoom in, pinch in = zoom out
        if (delta > 10f) ZoomIn();
        else if (delta < -10f) ZoomOut();
    }
}
