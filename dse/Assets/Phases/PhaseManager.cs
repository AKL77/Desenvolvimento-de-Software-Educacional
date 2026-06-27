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

void Awake()
{
    Instance = this;
}

void Start()
{
    Debug.Log("PhaseManager Start — SequenceManager.Instance is null: " + (SequenceManager.Instance == null));
    if (currentPhase != null)
        LoadPhase(currentPhase);
}
public void LoadPhase(PhaseData phase)
{
    currentPhase = phase;

    foreach (Transform child in cardsContainer)
        Destroy(child.gameObject);

    float cardWidth = 150f;
    float spacing = 10f;

    for (int i = 0; i < phase.availableCards.Count; i++)
    {
        GameObject cardObj = Instantiate(cardSlotPrefab, cardsContainer);
        CardView cardView = cardObj.GetComponent<CardView>();
        cardView.Setup(phase.availableCards[i]);

        RectTransform rect = cardObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(0, 0.5f);
        rect.pivot = new Vector2(0, 0.5f);
        rect.anchoredPosition = new Vector2(i * (cardWidth + spacing), 0);
        rect.sizeDelta = new Vector2(cardWidth, 180f);
    }

    Debug.Log("Carousel found: " + (cardCarousel != null) + ", card count: " + phase.availableCards.Count);
    if (cardCarousel != null)
        cardCarousel.Refresh(phase.availableCards.Count);

    SequenceManager.Instance.ClearSequence();
    SequenceManager.Instance.BuildSequence(phase.lineCount);
}
}
