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
    RepeatUntil,

    // Anexado no fim para não deslocar os índices serializados acima.
    // While = "enquanto a condição for verdadeira" (laço positivo, ao contrário do RepeatUntil).
    While
}

public enum ConditionTarget
{
    None,
    WallAhead,
    EnemyAhead,
    PathClear,
    // Anexado no fim (mesmo motivo do While): índices novos não podem deslocar os antigos.
    AtGoal
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

    // Inverte a condição na avaliação (vale para While e If). Permite frases com "não"
    // — ex.: "enquanto NÃO no objetivo" = While + AtGoal + negateCondition.
    public bool negateCondition = false;
}
