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
    public Button nextPhaseButton;

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
        if (nextPhaseButton != null)
            nextPhaseButton.onClick.AddListener(OnNextPhase);
    }

    // currentPhase is passed explicitly (instead of read from PhaseManager.Instance here)
    // so the "show next-phase button?" check always uses the phase that was actually
    // just completed, regardless of singleton timing.
    private PhaseData shownPhase;

    public void Show(PhaseData currentPhase)
    {
        gameObject.SetActive(true);
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable   = true;
        canvasGroup.DOFade(1f, 0.4f).SetEase(Ease.OutCubic);

        shownPhase = currentPhase;
        bool hasNextPhase = currentPhase != null && currentPhase.nextPhase != null;
        Debug.Log($"[VictoryPanel] Show — phase='{(currentPhase != null ? currentPhase.phaseName : "NULL")}', " +
                  $"nextPhase={(currentPhase != null && currentPhase.nextPhase != null ? currentPhase.nextPhase.phaseName : "NULL")}, " +
                  $"nextPhaseButton={(nextPhaseButton != null ? "OK" : "NULL")}");
        if (nextPhaseButton != null)
            nextPhaseButton.gameObject.SetActive(hasNextPhase);
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

    void OnNextPhase()
    {
        PhaseData next = shownPhase != null ? shownPhase.nextPhase : null;
        string completedPhaseId = shownPhase != null ? shownPhase.phaseId : null;
        Hide();

        void LoadNext()
        {
            if (next != null)
                PhaseManager.Instance.LoadPhase(next);
        }

        // Se a fase concluída tiver um diálogo de fim (ex.: o Alan explicando o que é
        // um algoritmo após a 1a fase), mostra a história antes de carregar a próxima.
        if (!StoryInterludeBootstrap.TryPlay(completedPhaseId, LoadNext))
            LoadNext();
    }
}
