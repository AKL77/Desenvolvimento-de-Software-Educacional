using UnityEngine;
using UnityEngine.UI;

public class GridTileView : MonoBehaviour
{
    [Header("UI")]
    public Image background;
    public GameObject goalIcon;
    public Image roadIcon;

    [Header("Colors")]
    public Color walkableColor = new Color(1f, 1f, 1f, 0.15f);
    public Color blockedColor  = new Color(0f, 0f, 0f, 0.55f);
    public Color goalColor     = new Color(1f, 0.85f, 0f, 0.85f);

    // Base art orientation (0 deg, before any rotation):
    //   roadStraightH/roadStraightV are dedicated art, never rotated
    //   roadCorner  connects Left+Down
    //   roadDeadEnd connects Up only
    //   roadT       connects Up+Left+Right (missing Down)
    // Rotating by +90 (CCW, Unity's positive Z) maps Up->Left->Down->Right->Up.
    [Header("Road Sprites")]
    public Sprite roadStraightH;
    public Sprite roadStraightV;
    public Sprite roadCorner;
    public Sprite roadDeadEnd;
    public Sprite roadT;

    private const int RoadUp    = 1;
    private const int RoadDown  = 2;
    private const int RoadLeft  = 4;
    private const int RoadRight = 8;

    // showTileVisuals=false: o tile vira apenas posicionador (usado quando a fase tem um
    // fundo composto único — GridVisualizer.phaseBackground — desenhando estrada/grama/goal).
    public void Setup(GridTileType type, int roadMask = 0, bool showTileVisuals = true)
    {
        if (!showTileVisuals)
        {
            if (background != null) background.color = Color.clear;
            if (goalIcon != null) goalIcon.SetActive(false);
            if (roadIcon != null) roadIcon.gameObject.SetActive(false);
            return;
        }

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

        if (roadIcon != null)
            ApplyRoad(roadMask);
    }

    private void ApplyRoad(int mask)
    {
        bool up    = (mask & RoadUp)    != 0;
        bool down  = (mask & RoadDown)  != 0;
        bool left  = (mask & RoadLeft)  != 0;
        bool right = (mask & RoadRight) != 0;
        int count = (up ? 1 : 0) + (down ? 1 : 0) + (left ? 1 : 0) + (right ? 1 : 0);

        Sprite sprite = null;
        float rotation = 0f;

        switch (count)
        {
            case 1:
                sprite = roadDeadEnd;
                if (up) rotation = 0f;
                else if (left) rotation = 90f;
                else if (down) rotation = 180f;
                else rotation = 270f; // right
                break;
            case 2:
                if (up && down) { sprite = roadStraightV; rotation = 0f; }
                else if (left && right) { sprite = roadStraightH; rotation = 0f; }
                else
                {
                    sprite = roadCorner;
                    if (left && down) rotation = 0f;
                    else if (down && right) rotation = 90f;
                    else if (up && right) rotation = 180f;
                    else rotation = 270f; // left && up
                }
                break;
            case 3:
                sprite = roadT;
                if (!down) rotation = 0f;
                else if (!right) rotation = 90f;
                else if (!up) rotation = 180f;
                else rotation = 270f; // !left
                break;
            case 4:
                sprite = roadT;
                rotation = 0f;
                break;
        }

        roadIcon.gameObject.SetActive(sprite != null);
        if (sprite != null)
        {
            roadIcon.sprite = sprite;
            roadIcon.transform.localEulerAngles = new Vector3(0, 0, rotation);
        }
    }
}
