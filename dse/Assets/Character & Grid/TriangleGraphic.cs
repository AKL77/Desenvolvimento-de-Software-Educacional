using UnityEngine;
using UnityEngine.UI;

// Draws a solid triangle pointing upward inside its RectTransform.
// Rotate the RectTransform to change facing direction.
[RequireComponent(typeof(CanvasRenderer))]
public class TriangleGraphic : Graphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;

        // Triangle: top-center, bottom-left, bottom-right
        Vector2 top   = new Vector2(r.center.x,        r.yMax);
        Vector2 botL  = new Vector2(r.xMin,            r.yMin);
        Vector2 botR  = new Vector2(r.xMax,            r.yMin);

        UIVertex v = UIVertex.simpleVert;
        v.color = color;

        v.position = top;   vh.AddVert(v);
        v.position = botL;  vh.AddVert(v);
        v.position = botR;  vh.AddVert(v);

        vh.AddTriangle(0, 1, 2);
    }
}
