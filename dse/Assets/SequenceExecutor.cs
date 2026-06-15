using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SequenceExecutor : MonoBehaviour
{
    public static SequenceExecutor Instance;

    [Header("References")]
    // drag either MockGridCharacter or GridCharacter here in the Inspector
    public MonoBehaviour characterReference;

    [Header("Execution Settings")]
    public float stepDelay = 0.1f;

    private IGridCharacter character;
    private bool isRunning = false;

    void Awake()
    {
        Instance = this;
        character = characterReference as IGridCharacter;

        if (character == null)
            Debug.LogError("characterReference does not implement IGridCharacter!");
    }

    public void RunSequence()
    {
        if (isRunning)
        {
            Debug.Log("Sequence already running");
            return;
        }

        List<CardData> sequence = SequenceManager.Instance.GetSequence();

        if (sequence.Count == 0)
        {
            Debug.Log("No cards in sequence");
            return;
        }

        StartCoroutine(ExecuteSequence(sequence));
    }

    IEnumerator ExecuteSequence(List<CardData> sequence)
    {
        isRunning = true;
        int i = 0;

        while (i < sequence.Count)
        {
            CardData card = sequence[i];
            Debug.Log($"Executing card {i}: {card.displayName}");

            switch (card.cardAction)
            {
                case CardAction.MoveForward:
                    character.MoveForward();
                    yield return new WaitForSeconds(stepDelay);
                    i++;
                    break;

                case CardAction.TurnLeft:
                    character.TurnLeft();
                    yield return new WaitForSeconds(stepDelay);
                    i++;
                    break;

                case CardAction.TurnRight:
                    character.TurnRight();
                    yield return new WaitForSeconds(stepDelay);
                    i++;
                    break;

                case CardAction.If:
                    bool conditionMet = EvaluateCondition(card.conditionTarget);
                    Debug.Log($"If {card.conditionTarget} → {conditionMet}");
                    if (!conditionMet)
                        i = SkipToElseOrEndIf(sequence, i + 1);
                    else
                        i++;
                    break;

                case CardAction.Else:
                    // reached Else normally means If was true, skip to EndIf
                    i = SkipToEndIf(sequence, i + 1);
                    break;

                case CardAction.EndIf:
                    i++;
                    break;

                case CardAction.Repeat:
                    int endRepeatIdx = FindEndRepeat(sequence, i + 1);
                    if (endRepeatIdx == -1)
                    {
                        Debug.LogWarning("Repeat card has no matching EndRepeat");
                        i++;
                        break;
                    }
                    Debug.Log($"Repeat {card.repeatValue} times");
                    for (int rep = 0; rep < card.repeatValue; rep++)
                    {
                        Debug.Log($"Repeat iteration {rep + 1}/{card.repeatValue}");
                        yield return StartCoroutine(
                            ExecuteSequence(sequence.GetRange(i + 1, endRepeatIdx - i - 1))
                        );
                    }
                    i = endRepeatIdx + 1;
                    break;

                case CardAction.RepeatUntil:
                    int endUntilIdx = FindEndRepeat(sequence, i + 1);
                    if (endUntilIdx == -1)
                    {
                        Debug.LogWarning("RepeatUntil card has no matching EndRepeat");
                        i++;
                        break;
                    }
                    int safetyLimit = 100;
                    int loopCount = 0;
                    Debug.Log($"RepeatUntil {card.conditionTarget}");
                    while (!EvaluateCondition(card.conditionTarget) && loopCount < safetyLimit)
                    {
                        yield return StartCoroutine(
                            ExecuteSequence(sequence.GetRange(i + 1, endUntilIdx - i - 1))
                        );
                        loopCount++;
                    }
                    if (loopCount >= safetyLimit)
                        Debug.LogWarning("RepeatUntil hit safety limit of 100 iterations");
                    i = endUntilIdx + 1;
                    break;

                case CardAction.EndRepeat:
                    i++;
                    break;

                default:
                    i++;
                    break;
            }

            // check goal after every step
            if (character.IsAtGoal())
            {
                OnSuccess();
                yield break;
            }
        }

        OnFailure();
    }

    bool EvaluateCondition(ConditionTarget condition)
    {
        // in mock mode IsWalkable always returns true
        // so WallAhead is always false and PathClear is always true
        return condition switch
        {
            ConditionTarget.WallAhead => !character.IsWalkable(Vector2Int.zero), // mock always false
            ConditionTarget.PathClear =>  character.IsWalkable(Vector2Int.zero), // mock always true
            _ => false
        };
    }

    int SkipToElseOrEndIf(List<CardData> sequence, int from)
    {
        int depth = 0;
        for (int i = from; i < sequence.Count; i++)
        {
            if (sequence[i].cardAction == CardAction.If) depth++;
            if (sequence[i].cardAction == CardAction.Else && depth == 0) return i + 1;
            if (sequence[i].cardAction == CardAction.EndIf)
            {
                if (depth == 0) return i + 1;
                depth--;
            }
        }
        return sequence.Count;
    }

    int SkipToEndIf(List<CardData> sequence, int from)
    {
        int depth = 0;
        for (int i = from; i < sequence.Count; i++)
        {
            if (sequence[i].cardAction == CardAction.If) depth++;
            if (sequence[i].cardAction == CardAction.EndIf)
            {
                if (depth == 0) return i + 1;
                depth--;
            }
        }
        return sequence.Count;
    }

    int FindEndRepeat(List<CardData> sequence, int from)
    {
        int depth = 0;
        for (int i = from; i < sequence.Count; i++)
        {
            if (sequence[i].cardAction == CardAction.Repeat ||
                sequence[i].cardAction == CardAction.RepeatUntil) depth++;
            if (sequence[i].cardAction == CardAction.EndRepeat)
            {
                if (depth == 0) return i;
                depth--;
            }
        }
        return -1;
    }

    void OnSuccess()
    {
        isRunning = false;
        Debug.Log("SUCCESS — goal reached!");
        // TODO: trigger win UI, load next phase
    }

    void OnFailure()
    {
        isRunning = false;
        Debug.Log("FAILED — resetting character");
        character.ResetToStart();
        // sequence stays intact so player can edit and try again
    }
    
    public void ResetSequence()
{
    if (isRunning)
    {
        StopAllCoroutines();
        isRunning = false;
    }

    character.ResetToStart();
    SequenceManager.Instance.ClearSequence();
    SequenceManager.Instance.BuildSequence(PhaseManager.Instance.currentPhase.lineCount);

    Debug.Log("Sequence reset");
}
}
