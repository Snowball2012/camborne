using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface ICameraField
{
    Vector2 GetClosestPointOnBorder ( Vector2 pt );
    bool IsPointInside ( Vector2 pt );
}

public class CameraField : ICameraField
{
    public CameraField ( List<List<ICuttableEdge>> loops )
    {
        m_loops = loops;
    }

    //ICameraField

    public Vector2 GetClosestPointOnBorder ( Vector2 pt )
    {
        return GetClosestPointOnBorder_Core( pt ).edge_cp.pt;
    }

    public bool IsPointInside ( Vector2 pt )
    {
        CPRes cp = GetClosestPointOnBorder_Core( pt );

        if ( cp.edge_cp.at_edge_end == null )
            return IsPointInsideByEdge( pt, cp.loop[cp.pos_in_loop], cp.edge_cp );

        // by vertex
        int first_pos = cp.edge_cp.at_edge_end.Value ? cp.pos_in_loop : cp.pos_in_loop - 1;
        int second_pos = ( first_pos + 1 ) % cp.loop.Count;
        if ( first_pos < 0 )
            first_pos = cp.loop.Count - 1;

        return IsPointInsideByVertex( pt - cp.edge_cp.pt,
            cp.loop[first_pos].Eval( 1.0f ).dir,
            cp.loop[second_pos].Eval( 0.0f ).dir );
    }

    //--ICameraField

    public List<List<ICuttableEdge>> Loops
    {
        get { return m_loops; }
        set { m_loops = value; }
    }

    public void DBG_Show( Color color )
    {
        foreach ( var loop in m_loops )
            foreach ( var edge in loop )
                edge.DBG_Show( color );
    }

    private bool TestLeftHemiplane ( Vector2 vec2test, Vector2 line_dir )
    {
        return Vector2.Dot( vec2test, new Vector2( -line_dir.y, line_dir.x ) ) > 0;
    }

    private bool IsPointInsideByEdge ( Vector2 pt, IEdge edge, EdgeCP cp )
    {
        EvalRes eval = edge.Eval( cp.normalized_t );
        return TestLeftHemiplane( pt - cp.pt, eval.dir );
    }

    private bool IsPointInsideByVertex ( Vector2 pt2v, Vector2 dir1, Vector2 dir2 )
    {
        // either in -dir1 right quarter, dir2 left quarter or in dir1, dir2 left hemiplane
        bool hemiplane1 = TestLeftHemiplane( pt2v, dir1 );
        bool hemiplane2 = TestLeftHemiplane( pt2v, dir2 );

        if ( hemiplane1 && hemiplane2 )
            return true;

        return Vector2.Dot( pt2v, dir1 ) < 0 && hemiplane1 || Vector2.Dot( pt2v, dir2 ) > 0 && hemiplane2;
    }

    private struct CPRes
    {
        public EdgeCP edge_cp;
        public List<IEdge> loop;
        public int pos_in_loop;
    }

    private CPRes GetClosestPointOnBorder_Core ( Vector2 pt )
    {
        CPRes res = new CPRes();
        float max_dist2 = Mathf.Infinity;
        foreach ( var loop in m_loops )
        {
            for ( int pos = 0; pos < loop.Count; ++pos )
            {
                EdgeCP cp = loop[pos].GetClosestPoint( pt );
                float distance2 = Vector2.SqrMagnitude( pt - cp.pt );
                if ( distance2 < max_dist2 )
                {
                    max_dist2 = distance2;
                    res.edge_cp = cp;
                    res.loop = loop.ConvertAll( x => (IEdge)x );
                    res.pos_in_loop = pos;
                }
            }
        }
        return res;
    }

    List<List<ICuttableEdge>> m_loops;
}
