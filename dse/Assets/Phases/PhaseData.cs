using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewPhase", menuName = "Cards/PhaseData")]
public class PhaseData : ScriptableObject
{
    [Header("Phase Info")]
    public string phaseId;
    public string phaseName;

    [Header("Sequence")]
    public int lineCount = 5;

    [Header("Available Cards")]
    public List<CardData> availableCards;

    [Header("Grid")]
    public GridLevelData gridLevel;
}
