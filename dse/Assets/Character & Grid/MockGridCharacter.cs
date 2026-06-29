using UnityEngine;

public class MockGridCharacter : MonoBehaviour, IGridCharacter
{
    public static MockGridCharacter Instance;

    [Header("Mock State")]
    public Vector2Int gridPosition = Vector2Int.zero;
    public FacingDirection facing = FacingDirection.Up;
    public Vector2Int goalPosition = new Vector2Int(3, 3);

    void Awake()
    {
        Instance = this;
    }

    public bool MoveForward()
    {
        Vector2Int next = gridPosition + FacingToVector();
        Debug.Log($"MoveForward → from {gridPosition} to {next} (facing {facing})");
        gridPosition = next;
        return true;
    }

    public void TurnLeft()
    {
        facing = facing switch
        {
            FacingDirection.Up    => FacingDirection.Left,
            FacingDirection.Left  => FacingDirection.Down,
            FacingDirection.Down  => FacingDirection.Right,
            FacingDirection.Right => FacingDirection.Up,
            _ => facing
        };
        Debug.Log($"TurnLeft → now facing {facing}");
    }

    public void TurnRight()
    {
        facing = facing switch
        {
            FacingDirection.Up    => FacingDirection.Right,
            FacingDirection.Right => FacingDirection.Down,
            FacingDirection.Down  => FacingDirection.Left,
            FacingDirection.Left  => FacingDirection.Up,
            _ => facing
        };
        Debug.Log($"TurnRight → now facing {facing}");
    }

    public bool IsAtGoal()
    {
        bool atGoal = gridPosition == goalPosition;
        if (atGoal) Debug.Log("GOAL REACHED!");
        return atGoal;
    }

    public void ResetToStart()
    {
        gridPosition = Vector2Int.zero;
        facing = FacingDirection.Up;
        Debug.Log("Character reset to start");
    }

    public bool IsWalkable(Vector2Int pos)
    {
        return true; // nothing is blocked in mock
    }

    public bool IsGoal(Vector2Int pos)
    {
        return pos == goalPosition;
    }

    public Vector2Int GetPositionAhead() => gridPosition + FacingToVector();

    Vector2Int FacingToVector()
    {
        return facing switch
        {
            FacingDirection.Up    => Vector2Int.up,
            FacingDirection.Down  => Vector2Int.down,
            FacingDirection.Left  => Vector2Int.left,
            FacingDirection.Right => Vector2Int.right,
            _ => Vector2Int.zero
        };
    }
    
    
}
