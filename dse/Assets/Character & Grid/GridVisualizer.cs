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

    // Quando setado, a fase desenha um único PNG composto (grama+estrada+goal) como fundo
    // atrás do grid, e os tiles param de desenhar seu próprio visual (viram só posicionadores).
    [Header("Optional composite background")]
    public Sprite phaseBackground;

    private RectTransform _container;
    private float _tileW;
    private float _tileH;
    private Image _phaseBackgroundImage;
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

        EnsurePhaseBackground();

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
                GridTileType type = levelData.GetTile(pos);
                int roadMask = type != GridTileType.Blocked ? RoadConnectivityMask(pos) : 0;
                tile.Setup(type, roadMask, showTileVisuals: phaseBackground == null);
                _tiles.Add(tile);
            }
        }
    }

    // Creates (once) a full-panel Image behind the tiles showing the composite phase map.
    // Stretched 0->1 so it always shares the MapPanel rect, keeping it aligned to the cells.
    private void EnsurePhaseBackground()
    {
        if (phaseBackground == null)
        {
            if (_phaseBackgroundImage != null)
                _phaseBackgroundImage.gameObject.SetActive(false);
            return;
        }

        if (_phaseBackgroundImage == null)
        {
            var go = new GameObject("PhaseBackground", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(_container, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.SetAsFirstSibling(); // behind the tiles
            _phaseBackgroundImage = go.GetComponent<Image>();
            _phaseBackgroundImage.raycastTarget = false;
        }

        _phaseBackgroundImage.gameObject.SetActive(true);
        _phaseBackgroundImage.sprite = phaseBackground;
        _phaseBackgroundImage.transform.SetAsFirstSibling();
    }

    // 1=up, 2=down, 4=left, 8=right — set when that neighbor is walkable/goal,
    // so the road sprite picked in GridTileView lines up with the actual path.
    private int RoadConnectivityMask(Vector2Int pos)
    {
        int mask = 0;
        if (IsRoadTile(pos + Vector2Int.up))    mask |= 1;
        if (IsRoadTile(pos + Vector2Int.down))  mask |= 2;
        if (IsRoadTile(pos + Vector2Int.left))  mask |= 4;
        if (IsRoadTile(pos + Vector2Int.right)) mask |= 8;
        return mask;
    }

    private bool IsRoadTile(Vector2Int pos) =>
        levelData.InBounds(pos) && levelData.GetTile(pos) != GridTileType.Blocked;

    // Returns anchoredPosition of the center of a grid cell, relative to _container.
    public Vector2 GetTileAnchoredPosition(Vector2Int gridPos) =>
        new Vector2(gridPos.x * _tileW + _tileW * 0.5f,
                    gridPos.y * _tileH + _tileH * 0.5f);
}
