using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class VictoryPanel : MonoBehaviour
{
    public static VictoryPanel Instance;

    [Header("UI")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI messageText;
    public Button restartButton;

    void Awake()
    {
        Instance = this;
        canvasGroup.alpha          = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable   = false;
    }

    void Start()
    {
        restartButton.onClick.AddListener(OnRestart);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable   = true;
        canvasGroup.DOFade(1f, 0.4f).SetEase(Ease.OutCubic);
    }

    public void Hide()
    {
        canvasGroup.DOFade(0f, 0.2f).OnComplete(() =>
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable   = false;
            gameObject.SetActive(false);
        });
    }

    void OnRestart()
    {
        Hide();
        SequenceExecutor.Instance.ResetSequence();
    }
}
