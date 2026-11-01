using UnityEngine;
using UnityEngine.UI;

// Slightly uneven brush strokes to match the shop's painted borders.
public sealed class SkinLockedMark : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var shadow = new Color32(74, 8, 10, 230);
        var red = (Color32)color;

        Stroke(vh, new Vector2(-28, 26), new Vector2(25, -27), 12, shadow);
        Stroke(vh, new Vector2(-25, -27), new Vector2(27, 25), 12, shadow);
        Stroke(vh, new Vector2(-27, 27), new Vector2(24, -26), 8, red);
        Stroke(vh, new Vector2(-24, -26), new Vector2(26, 24), 8, red);
        Stroke(vh, new Vector2(-21, 26), new Vector2(23, -19), 2, new Color32(246, 118, 104, 210));
    }

    private static void Stroke(VertexHelper vh, Vector2 start, Vector2 end, float width, Color32 tint)
    {
        var normal = new Vector2(-(end.y - start.y), end.x - start.x).normalized * width * 0.5f;
        int first = vh.currentVertCount;
        Add(vh, start - normal, tint);
        Add(vh, start + normal * 0.85f, tint);
        Add(vh, end + normal, tint);
        Add(vh, end - normal * 0.8f, tint);
        vh.AddTriangle(first, first + 1, first + 2);
        vh.AddTriangle(first, first + 2, first + 3);
    }

    private static void Add(VertexHelper vh, Vector2 point, Color32 tint)
    {
        var vertex = UIVertex.simpleVert;
        vertex.color = tint;
        vertex.position = point;
        vh.AddVert(vertex);
    }
}
