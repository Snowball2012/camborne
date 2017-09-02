using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct LineSegment
{
    public Vector2 p0, p1;
}

public struct CircleArc
{
    float t_start;
    float t_end;
    public Vector2 center;
    public float radius;
}

public struct BoxPrimitive
{
    public Vector2 center;
    public Vector2 x_dir;
    public float a;
    public float b;

    public void DBG_Show( Color color )
    {
        Vector2 p0, p1;
        Vector2 y_dir = new Vector2( -x_dir.y, x_dir.x );
        Vector2 av = x_dir * a / 2;
        Vector2 bv = y_dir * b / 2;

        p0 = center + av - bv;
        p1 = center + av + bv;
        DBG_DrawSegment( p0, p1, color );
        p0 = p1;
        p1 = center - av + bv;
        DBG_DrawSegment( p0, p1, color );
        p0 = p1;
        p1 = center - av - bv;
        DBG_DrawSegment( p0, p1, color );
        p0 = p1;
        p1 = center + av - bv;
        DBG_DrawSegment( p0, p1, color );
    }

    private void DBG_DrawSegment( Vector2 p0, Vector2 p1, Color color )
    {
        Debug.DrawLine( new Vector3( p0.x, 0.01f, p0.y ), new Vector3( p1.x, 0.01f, p1.y ), color, 0, false );
    }
}

public struct CirclePrimitive
{
    public Vector2 center;
    public float radius;
}

public class CameraScene
{
    public CameraScene( List<CirclePrimitive> circles, List<BoxPrimitive> boxes, BoxPrimitive room_bounds )
    {
        m_room_bounds = room_bounds;
        m_circles = circles;
        m_boxes = boxes;
    }

    public void DBG_Show()
    {
        m_room_bounds.DBG_Show( Color.yellow );
        foreach ( BoxPrimitive box in m_boxes )
            box.DBG_Show( Color.green );
    }

    public IEnumerable<CirclePrimitive> Circles { get { return m_circles; } }
    public IEnumerable<BoxPrimitive> Boxes { get { return m_boxes; } }
    public BoxPrimitive RoomBound { get { return m_room_bounds; } }

    List<CirclePrimitive> m_circles;
    List<BoxPrimitive> m_boxes;
    BoxPrimitive m_room_bounds;
}
