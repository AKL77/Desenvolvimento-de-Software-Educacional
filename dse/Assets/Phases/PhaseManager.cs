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
    Debug.Log($"Loading phase: {phase.phaseName}, cards: {phase.availableCards.Count}, cardsContainer null: {cardsContainer == null}");
    // rest of your code

    
        currentPhase = phase;

        // Clear existing cards in the hand
        foreach (Transform child in cardsContainer)
            Destroy(child.gameObject);

        // Spawn one card slot per available card
        foreach (CardData card in phase.availableCards)
        {
            GameObject cardObj = Instantiate(cardSlotPrefab, cardsContainer);
            CardView cardView = cardObj.GetComponent<CardView>();
            cardView.Setup(card);
        }

        // Reset the sequence for the new phase
        SequenceManager.Instance.ClearSequence();
        SequenceManager.Instance.BuildSequence(phase.lineCount);
    }
}
