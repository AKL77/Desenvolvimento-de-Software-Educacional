using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class SequenceManager : MonoBehaviour
{
    public static SequenceManager Instance;

    [Header("Prefabs")]
    public GameObject cardSlotPrefab;
    public GameObject sequenceLinePrefab;

    [Header("References")]
    public Transform sequenceContainer;

    private List<DropZone> lines = new List<DropZone>();

void Awake()
{
    Instance = this;
    Debug.Log("SequenceManager Awake on: " + gameObject.name);
    Debug.Log("sequenceContainer: " + (sequenceContainer == null ? "NULL" : sequenceContainer.name));
    Debug.Log("sequenceLinePrefab: " + (sequenceLinePrefab == null ? "NULL" : sequenceLinePrefab.name));
    Debug.Log("cardSlotPrefab: " + (cardSlotPrefab == null ? "NULL" : cardSlotPrefab.name));
}

    public bool HasActiveSequence()
    {
        return lines.Count > 0;
    }

    public void BuildSequence(int lineCount)
{
    if (sequenceContainer == null)
    {
        Debug.LogError("sequenceContainer is null on " + gameObject.name);
        return;
    }
    if (sequenceLinePrefab == null)
    {
        Debug.LogError("sequenceLinePrefab is null on " + gameObject.name);
        return;
    }

    foreach (Transform child in sequenceContainer)
        Destroy(child.gameObject);
    lines.Clear();

    for (int i = 0; i < lineCount; i++)
    {
        GameObject lineObj = Instantiate(sequenceLinePrefab, sequenceContainer);
        DropZone dropZone = lineObj.GetComponent<DropZone>();
        if (dropZone == null)
        {
            Debug.LogError("No DropZone on SequenceLine!");
            continue;
        }
        dropZone.Setup(i);
        lines.Add(dropZone);
    }

    // Force layout recalculation
    LayoutRebuilder.ForceRebuildLayoutImmediate(
        sequenceContainer.GetComponent<RectTransform>()
    );
}
    
    public List<CardData> GetSequence()
    {
        List<CardData> sequence = new List<CardData>();
        foreach (DropZone line in lines)
        {
            if (line.placedCard != null)
                sequence.Add(line.placedCard);
        }
        return sequence;
    }

    public void ClearSequence()
    {
        foreach (DropZone line in lines)
            line.ClearCard();
    }
}
