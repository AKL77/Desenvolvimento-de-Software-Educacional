using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Builds and displays a grid of tiles inside a RectTransform (MapPanel).
// Call GetTileAnchoredPosition to get where the character should sit.
// DefaultExecutionOrder(-100) guarantees BuildGrid() runs before GridCharacter.Start(),
// which reads tile size via GetTileAnchoredPosition on its own Start.
[DefaultExecutionOrder(-100)]
public class GridVisualizer : MonoBehaviour
{
    [Header("Data")]
    public GridLevelData levelData;

    [Header("Prefab")]
    public GridTileView tilePrefab;

    private RectTransform _container;
    private float _tileW;
    private float _tileH;
    private readonly List<GridTileView> _tiles = new();

    void Awake()
    {
        _container = GetComponent<RectTransform>();
    }

    void Start()
    {
        Canvas.ForceUpdateCanvases();
        BuildGrid();
    }

    // Rect only reflects the resolved Canvas Scaler / anchor layout once the layout
    // pass has run, so rebuild whenever the panel's resolved size actually changes
    // (e.g. Game view resized in the Editor, or window resized at runtime).
    void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled || levelData == null || tilePrefab == null) return;
        BuildGrid();
        GridCharacter.Instance?.SnapToCurrentPosition();
    }

    public void BuildGrid()
    {
        foreach (var t in _tiles)
            if (t != null) Destroy(t.gameObject);
        _tiles.Clear();

        Rect r = _container.rect;
        _tileW = r.width  / levelData.width;
        _tileH = r.height / levelData.height;

        for (int row = 0; row < levelData.height; row++)
        {
            for (int col = 0; col < levelData.width; col++)
            {
                GridTileView tile = Instantiate(tilePrefab, _container);
                RectTransform rt = tile.GetComponent<RectTransform>();

                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot     = Vector2.zero;
                rt.sizeDelta = new Vector2(_tileW, _tileH);
                rt.anchoredPosition = new Vector2(col * _tileW, row * _tileH);

                var pos = new Vector2Int(col, row);
                tile.Setup(levelData.GetTile(pos));
                _tiles.Add(tile);
            }
        }
    }

    // Returns anchoredPosition of the center of a grid cell, relative to _container.
    public Vector2 GetTileAnchoredPosition(Vector2Int gridPos) =>
        new Vector2(gridPos.x * _tileW + _tileW * 0.5f,
                    gridPos.y * _tileH + _tileH * 0.5f);
}
