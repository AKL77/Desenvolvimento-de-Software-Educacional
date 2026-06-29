using UnityEngine;

public enum CardType
{
    Movement,
    Condition,
    Loop
}

public enum CardAction
{
    // Movement
    MoveForward,
    TurnLeft,
    TurnRight,

    // Condition
    If,
    Else,
    EndIf,

    // Loop
    Repeat,
    EndRepeat,
    RepeatUntil
}

public enum ConditionTarget
{
    None,
    WallAhead,
    EnemyAhead,
    PathClear,
    // add more as needed
}

[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/CardData")]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    public string cardId;
    public string displayName;
    public CardType cardType;
    public CardAction cardAction;

    [Header("Visuals")]
    public Sprite artwork;
    public Color cardColor = Color.white;
    [TextArea] public string description;

    [Header("Loop only")]
    public int repeatValue = 2;
    
    [Header("Condition / RepeatUntil")]
    public ConditionTarget conditionTarget = ConditionTarget.None;
}
