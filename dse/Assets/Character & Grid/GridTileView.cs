using UnityEngine;
using UnityEngine.UI;

public class GridTileView : MonoBehaviour
{
    [Header("UI")]
    public Image background;
    public GameObject goalIcon;

    [Header("Colors")]
    public Color walkableColor = new Color(1f, 1f, 1f, 0.15f);
    public Color blockedColor  = new Color(0f, 0f, 0f, 0.55f);
    public Color goalColor     = new Color(1f, 0.85f, 0f, 0.85f);

    public void Setup(GridTileType type)
    {
        if (background != null)
            background.color = type switch
            {
                GridTileType.Walkable => walkableColor,
                GridTileType.Blocked  => blockedColor,
                GridTileType.Goal     => goalColor,
                _                     => walkableColor
            };

        if (goalIcon != null)
            goalIcon.SetActive(type == GridTileType.Goal);
    }
}
