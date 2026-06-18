using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // install via Package Manager or Asset Store

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public RectTransform mapPanel;
    public GameObject sidePanel;
    public GameObject cardsPanel;
    public GameObject shrinkButton; // button shown on the fullscreen map
    public Button mapButton; // assign in Inspector

    [Header("Map compact layout")]
public Vector2 compactAnchorMin = new Vector2(0.45f, 0.45f);
public Vector2 compactAnchorMax = new Vector2(1f, 1f);

    private bool isCompact = false;


public void SwitchToCompact()
{
    isCompact = true;
    mapButton.interactable = false;
    shrinkButton.SetActive(false);

    // PhaseManager already built the sequence on Start()
    // only rebuild if returning from a different phase
    if (!SequenceManager.Instance.HasActiveSequence())
        SequenceManager.Instance.BuildSequence(PhaseManager.Instance.currentPhase.lineCount);

    DOVirtual.DelayedCall(0.2f, () => {
        sidePanel.SetActive(true);
        cardsPanel.SetActive(true);
    });

    mapPanel.DOAnchorMin(compactAnchorMin, 0.4f).SetEase(Ease.OutCubic);
    mapPanel.DOAnchorMax(compactAnchorMax, 0.4f).SetEase(Ease.OutCubic);
mapPanel.offsetMax = new Vector2(0, -20); // top and right padding

    DOVirtual.DelayedCall(0.4f, () => {
        mapButton.interactable = true;
    });
}

    public void SwitchToFullscreen()
    {
    	if (!isCompact) return;
    	isCompact = false;
    	mapButton.interactable = false; // not needed in fullscreen

        sidePanel.SetActive(false);
        cardsPanel.SetActive(false);
        shrinkButton.SetActive(true);

        mapPanel.DOAnchorMin(Vector2.zero, 0.4f).SetEase(Ease.OutCubic);
        mapPanel.DOAnchorMax(Vector2.one,  0.4f).SetEase(Ease.OutCubic);
    }
}
