using UnityEngine;

public interface IGridCharacter
{
    bool MoveForward();
    void TurnLeft();
    void TurnRight();
    bool IsAtGoal();
    void ResetToStart();
    bool IsWalkable(Vector2Int pos);
    bool IsGoal(Vector2Int pos);
}
