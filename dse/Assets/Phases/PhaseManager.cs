using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class PhaseManager : MonoBehaviour
{
    public static PhaseManager Instance;

    [Header("Current Phase")]
    public PhaseData currentPhase;

    [Header("References")]
    public Transform cardsContainer;    // the scroll content inside CardsPanel
    public GameObject cardSlotPrefab;
    [Header("Carousel")]
    public CardCarousel cardCarousel;

    [Header("Grid")]
    public GridVisualizer gridVisualizer;

    void Awake()
    {
        // Há um PhaseManager duplicado (legado) no SidePanel, que começa inativo e só
        // acorda quando UIManager.SwitchToCompact o ativa — sobrescrevendo o Instance bom
        // (cujo currentPhase já foi carregado) por um cujo currentPhase é null. Guard de
        // singleton: o primeiro a acordar (o do objeto UI, ativo no load) vence; qualquer
        // duplicado se autodestroi e nunca toca no Instance.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[PhaseManager] Duplicado em '{name}' (id={GetInstanceID()}) ignorado; mantendo Instance id={Instance.GetInstanceID()}.");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        Debug.Log("PhaseManager Start — SequenceManager.Instance is null: " + (SequenceManager.Instance == null));

        // gridVisualizer pode não ter sido linkado no Inspector (o campo foi adicionado
        // depois que a cena foi configurada). Resolve sozinho — só existe um na cena.
        if (gridVisualizer == null)
            gridVisualizer = FindFirstObjectByType<GridVisualizer>();

        if (currentPhase != null)
            LoadPhase(currentPhase);

        Debug.Log($"[PhaseManager] Start done — instanceID={GetInstanceID()}, Instance.ID={(Instance != null ? Instance.GetInstanceID().ToString() : "NULL")}, currentPhase={(currentPhase != null ? currentPhase.phaseName : "NULL")}");
    }

    public void LoadPhase(PhaseData phase)
    {
        Debug.Log($"[PhaseManager] LoadPhase('{phase.phaseName}') — availableCards.Count={phase.availableCards.Count}, cardsContainer={(cardsContainer != null ? cardsContainer.name : "NULL")}");
        currentPhase = phase;

        foreach (Transform child in cardsContainer)
            Destroy(child.gameObject);

        float cardWidth = 150f;
        float spacing = 10f;

        for (int i = 0; i < phase.availableCards.Count; i++)
        {
            CardData data = phase.availableCards[i];
            GameObject cardObj = Instantiate(cardSlotPrefab, cardsContainer);
            cardObj.name = $"CardSlot_{i}_{(data != null ? data.cardId : "null")}";
            CardView cardView = cardObj.GetComponent<CardView>();
            cardView.Setup(data);

            RectTransform rect = cardObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(0, 0.5f);
            rect.pivot = new Vector2(0, 0.5f);
            rect.anchoredPosition = new Vector2(i * (cardWidth + spacing), 0);
            rect.sizeDelta = new Vector2(cardWidth, 180f);

            Debug.Log($"[PhaseManager] Card {i} ('{data?.displayName}') placed at anchoredPosition={rect.anchoredPosition}, activeInHierarchy={cardObj.activeInHierarchy}");
        }

        Debug.Log("Carousel found: " + (cardCarousel != null) + ", card count: " + phase.availableCards.Count);
        if (cardCarousel != null)
            cardCarousel.Refresh(phase.availableCards.Count);

        Debug.Log($"[PhaseManager] gridLevel={(phase.gridLevel != null ? phase.gridLevel.name : "NULL")}, gridVisualizer={(gridVisualizer != null ? "OK" : "NULL")}");
        if (phase.gridLevel != null && gridVisualizer != null)
        {
            gridVisualizer.levelData = phase.gridLevel;
            gridVisualizer.BuildGrid();

            if (GridCharacter.Instance != null)
            {
                GridCharacter.Instance.levelData = phase.gridLevel;
                GridCharacter.Instance.ResetToStart();
            }
        }

        SequenceManager.Instance.ClearSequence();
        SequenceManager.Instance.BuildSequence(phase.lineCount);
    }
}
