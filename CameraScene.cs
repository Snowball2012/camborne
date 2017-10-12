using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct LineSegment
{
    public Vector2 p0, p1;

    public Vector2 Eval( float t ) // unclamped
    {
        return Vector2.LerpUnclamped( p0, p1, t );
    }

    public float ClosestPointParam( Vector2 pt )
    {
        Vector2 tmp = p1 - p0;
        return Vector2.Dot( pt - p0, tmp ) / tmp.SqrMagnitude();
    }

    public void DBG_Show( Color color )
    {
        Debug.DrawLine( new Vector3( p0.x, 0.01f, p0.y ), new Vector3( p1.x, 0.01f, p1.y ), color, 0, false );
    }
}

public struct CircleArc
{
    public float t_start;
    public float t_end;
    public Vector2 center;
    public float radius;

    public float ParamToDefRange( float t )
    {
        const float period = Mathf.PI * 2;
        while ( t > period )
            t -= period;
        while ( t < 0 )
            t += period;
        if ( t > t_end )
            t -= period;
        if ( t < t_start )
            t += period;

        return t;
    }

    public Vector2 Eval( float t ) // unclamped
    {
        return center + ( new Vector2( Mathf.Cos( t ), Mathf.Sin( t ) ) ) * radius;
    }

    public Vector2 EvalDir( float t ) // unclamped
    {
        return new Vector2( -Mathf.Sin( t ), Mathf.Cos( t ) );
    }

    public float GetClosestPoint( Vector2 pt )
    {
        pt -= center;
        return Mathf.Atan2( pt.y, pt.x );
    }

    public void DBG_Show ( Color color )
    {
        const int num_pts = 20; // why not?
        for ( int i = 1; i < num_pts; ++i )
        {
            float start_t = Mathf.Lerp( t_start, t_end, (float)( i - 1 ) / ( (float)num_pts - 1 ) );
            float end_t = Mathf.Lerp( t_start, t_end, (float)( i ) / ( (float)num_pts - 1 ) );
            Vector2 start = Eval( start_t );
            Vector2 end = Eval( end_t );
            Debug.DrawLine( new Vector3( start.x, 0.01f, start.y ), new Vector3( end.x, 0.01f, end.y ), color, 0, false );
        }
    }
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

    public void DBG_Show( Color color )
    {
        (new CircleArc { center = this.center, radius = this.radius, t_start = 0, t_end = Mathf.PI * 2 }).DBG_Show( color );
    }
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
        foreach ( CirclePrimitive circle in m_circles )
            circle.DBG_Show( Color.green );
    }

    public IEnumerable<CirclePrimitive> Circles { get { return m_circles; } }
    public IEnumerable<BoxPrimitive> Boxes { get { return m_boxes; } }
    public BoxPrimitive RoomBound { get { return m_room_bounds; } }

    List<CirclePrimitive> m_circles;
    List<BoxPrimitive> m_boxes;
    BoxPrimitive m_room_bounds;
}
