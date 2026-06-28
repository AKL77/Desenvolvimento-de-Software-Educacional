using UnityEngine;
using DG.Tweening;

// Real IGridCharacter — draws a triangle on the grid and animates movement.
// Assign this to SequenceExecutor.characterReference instead of MockGridCharacter.
// IMPORTANT: SequenceExecutor.stepDelay must be >= moveDuration (recommend 0.4f).
public class GridCharacter : MonoBehaviour, IGridCharacter
{
    public static GridCharacter Instance;

    [Header("Data")]
    public GridLevelData levelData;

    [Header("References")]
    public GridVisualizer visualizer;
    public RectTransform characterRect;   // the triangle RectTransform

    [Header("Animation")]
    public float moveDuration = 0.3f;

    private Vector2Int _position;
    private FacingDirection _facing;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ResetToStart();
    }

    // ── IGridCharacter ──────────────────────────────────────────────

    public bool MoveForward()
    {
        Vector2Int next = _position + FacingToVector(_facing);

        if (!IsWalkable(next))
        {
            Debug.Log($"MoveForward blocked at {next}");
            return false;
        }

        _position = next;
        Vector2 target = visualizer.GetTileAnchoredPosition(_position);
        characterRect.DOAnchorPos(target, moveDuration).SetEase(Ease.OutCubic);

        Debug.Log($"MoveForward → {_position} (facing {_facing})");
        return true;
    }

    public void TurnLeft()
    {
        _facing = _facing switch
        {
            FacingDirection.Up    => FacingDirection.Left,
            FacingDirection.Left  => FacingDirection.Down,
            FacingDirection.Down  => FacingDirection.Right,
            FacingDirection.Right => FacingDirection.Up,
            _                     => _facing
        };
        ApplyFacingRotation();
        Debug.Log($"TurnLeft → now facing {_facing}");
    }

    public void TurnRight()
    {
        _facing = _facing switch
        {
            FacingDirection.Up    => FacingDirection.Right,
            FacingDirection.Right => FacingDirection.Down,
            FacingDirection.Down  => FacingDirection.Left,
            FacingDirection.Left  => FacingDirection.Up,
            _                     => _facing
        };
        ApplyFacingRotation();
        Debug.Log($"TurnRight → now facing {_facing}");
    }

    public bool IsAtGoal() => _position == levelData.goalPosition;

    // Re-anchors the triangle at its current grid cell without resetting position/facing.
    // Called by GridVisualizer after a rebuild caused by a panel resize.
    public void SnapToCurrentPosition()
    {
        characterRect.DOKill();
        characterRect.anchoredPosition = visualizer.GetTileAnchoredPosition(_position);
    }

    public void ResetToStart()
    {
        _position = levelData.startPosition;
        _facing   = levelData.startFacing;

        characterRect.DOKill();
        characterRect.anchoredPosition = visualizer.GetTileAnchoredPosition(_position);
        ApplyFacingRotation(instant: true);

        Debug.Log($"Character reset → {_position}, facing {_facing}");
    }

    public bool IsWalkable(Vector2Int pos)
    {
        if (!levelData.InBounds(pos)) return false;
        return levelData.GetTile(pos) != GridTileType.Blocked;
    }

    public bool IsGoal(Vector2Int pos) => pos == levelData.goalPosition;

    // ── Helpers ─────────────────────────────────────────────────────

    static Vector2Int FacingToVector(FacingDirection f) => f switch
    {
        FacingDirection.Up    => Vector2Int.up,
        FacingDirection.Down  => Vector2Int.down,
        FacingDirection.Left  => Vector2Int.left,
        FacingDirection.Right => Vector2Int.right,
        _                     => Vector2Int.zero
    };

    // Z-rotation so the triangle (which points up at 0°) matches facing.
    static float FacingToZRotation(FacingDirection f) => f switch
    {
        FacingDirection.Up    =>   0f,
        FacingDirection.Left  =>  90f,
        FacingDirection.Down  => 180f,
        FacingDirection.Right => -90f,
        _                     =>   0f
    };

    void ApplyFacingRotation(bool instant = false)
    {
        float z = FacingToZRotation(_facing);
        if (instant)
            characterRect.localEulerAngles = new Vector3(0, 0, z);
        else
            characterRect.DOLocalRotate(new Vector3(0, 0, z), moveDuration * 0.5f);
    }
}
