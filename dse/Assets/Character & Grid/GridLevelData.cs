using UnityEngine;

[CreateAssetMenu(fileName = "NewGridLevel", menuName = "Grid/GridLevelData")]
public class GridLevelData : ScriptableObject
{
    [Header("Grid Size")]
    public int width = 5;
    public int height = 5;

    [Header("Tiles (row-major, bottom-left origin)")]
    public GridTileType[] tiles;

    [Header("Character")]
    public Vector2Int startPosition = Vector2Int.zero;
    public FacingDirection startFacing = FacingDirection.Up;

    [Header("Goal")]
    public Vector2Int goalPosition = new Vector2Int(2, 4);

    public GridTileType GetTile(Vector2Int pos)
    {
        if (!InBounds(pos)) return GridTileType.Blocked;
        return tiles[pos.y * width + pos.x];
    }

    public bool InBounds(Vector2Int pos) =>
        pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;

    [ContextMenu("Reset All Tiles to Walkable")]
    public void ResetGrid()
    {
        tiles = new GridTileType[width * height];
        for (int i = 0; i < tiles.Length; i++)
            tiles[i] = GridTileType.Walkable;
        tiles[goalPosition.y * width + goalPosition.x] = GridTileType.Goal;
    }
}
