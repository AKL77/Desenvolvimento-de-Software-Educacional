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
    // Sinaliza que o objetivo foi atingido. Como o corpo dos laços roda de forma recursiva,
    // a detecção precisa ser global — quem decide sucesso/fracasso é só o nível-topo.
    private bool goalReached = false;

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

        StartCoroutine(ExecuteTopLevel(sequence));
    }

    // Nível-topo: roda as cartas uma vez e decide sucesso/fracasso. Só aqui OnSuccess/OnFailure
    // são chamados — RunCards (recursivo) nunca os chama, para não resetar o personagem nem
    // mostrar vitória no meio de um laço.
    IEnumerator ExecuteTopLevel(List<CardData> sequence)
    {
        isRunning = true;
        goalReached = false;

        yield return StartCoroutine(RunCards(sequence));

        isRunning = false;
        if (goalReached)
            OnSuccess();
        else
            OnFailure();
    }

    IEnumerator RunCards(List<CardData> sequence)
    {
        int i = 0;

        while (i < sequence.Count)
        {
            if (goalReached) yield break;

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
                    bool conditionMet = EvaluateCondition(card);
                    Debug.Log($"If {card.conditionTarget} (negate={card.negateCondition}) → {conditionMet}");
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
                            RunCards(sequence.GetRange(i + 1, endRepeatIdx - i - 1))
                        );
                        if (goalReached) break;
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
                    // Para quando a condição vira verdadeira OU o corpo atinge o objetivo.
                    while (!goalReached && !EvaluateCondition(card) && loopCount < safetyLimit)
                    {
                        yield return StartCoroutine(
                            RunCards(sequence.GetRange(i + 1, endUntilIdx - i - 1))
                        );
                        loopCount++;
                    }
                    if (loopCount >= safetyLimit)
                        Debug.LogWarning("RepeatUntil hit safety limit of 100 iterations");
                    i = endUntilIdx + 1;
                    break;

                case CardAction.While:
                    int endWhileIdx = FindEndRepeat(sequence, i + 1);
                    if (endWhileIdx == -1)
                    {
                        Debug.LogWarning("While card has no matching EndRepeat");
                        i++;
                        break;
                    }
                    int whileSafety = 100;
                    int whileLoops = 0;
                    Debug.Log($"While {card.conditionTarget} (negate={card.negateCondition})");
                    // Laço positivo: roda enquanto a condição for verdadeira; para ao vencer.
                    while (!goalReached && EvaluateCondition(card) && whileLoops < whileSafety)
                    {
                        yield return StartCoroutine(
                            RunCards(sequence.GetRange(i + 1, endWhileIdx - i - 1))
                        );
                        whileLoops++;
                    }
                    if (whileLoops >= whileSafety)
                        Debug.LogWarning("While hit safety limit of 100 iterations");
                    i = endWhileIdx + 1;
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
                goalReached = true;
                yield break;
            }
        }
    }

    bool EvaluateCondition(CardData card)
    {
        Vector2Int ahead = character.GetPositionAhead();
        bool result = card.conditionTarget switch
        {
            ConditionTarget.WallAhead => !character.IsWalkable(ahead),
            ConditionTarget.PathClear =>  character.IsWalkable(ahead),
            ConditionTarget.AtGoal    =>  character.IsAtGoal(),
            ConditionTarget.EnemyAhead => false, // sem inimigos no jogo ainda
            _ => false
        };
        return card.negateCondition ? !result : result;
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
                sequence[i].cardAction == CardAction.RepeatUntil ||
                sequence[i].cardAction == CardAction.While) depth++;
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
        PhaseManager pm = PhaseManager.Instance;
        Debug.Log($"[SequenceExecutor] OnSuccess — PhaseManager.Instance.ID={(pm != null ? pm.GetInstanceID().ToString() : "NULL")}, currentPhase={(pm != null && pm.currentPhase != null ? pm.currentPhase.phaseName : "NULL")}");
        VictoryPanel.Instance?.Show(pm != null ? pm.currentPhase : null);
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
