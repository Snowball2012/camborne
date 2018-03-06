using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct EvalRes
{
    public Vector2 pt;
    public Vector2 dir;
}

public struct EdgeCP
{
    public Vector2 pt;
    public float normalized_t;
    public bool? at_edge_end;
}

public interface IEdgeVisitor
{
    void Visit ( CircleEdge circle );
    void Visit ( LineEdge line );
}

public interface IEdge
{
    EvalRes Eval ( float normalized_t );
    EdgeCP GetClosestPoint ( Vector2 pt );
    float GetLen ( );
    void OnVisit ( IEdgeVisitor visitor );
}

public interface ICuttableEdge : IEdge
{
    ICuttableEdge Cut ( float t, bool first_part );

    void Connect ( ICuttableEdge other_edge, bool at_start );

    void DBG_Show ( Color color );
    DBGLine2d DBG_ShowCetonia ( Color color, float thickness );
}

public class CircleEdge : ICuttableEdge
{
    public CircleEdge ( CircleArc data, bool sense )
    {
        m_data = data;
        if ( m_data.t_start < 0 )
        {
            m_data.t_start += Mathf.PI * 2;
            m_data.t_end += Mathf.PI * 2;
        }
        if ( m_data.t_start > Mathf.PI * 2 )
        {
            m_data.t_start -= Mathf.PI * 2;
            m_data.t_end -= Mathf.PI * 2;
        }
        m_sense = sense;
    }

    public EvalRes Eval ( float t )
    {
        if ( ! m_sense )
            t = 1.0f - t;
        float unclamped_t = Mathf.Lerp( m_data.t_start, m_data.t_end, t );

        EvalRes res = new EvalRes
        {
            pt = m_data.Eval( unclamped_t ),
            dir = m_data.EvalDir( unclamped_t )
        };
        if ( ! m_sense )
            res.dir *= -1;

        return res;
    }

    public EdgeCP GetClosestPoint ( Vector2 pt )
    {
        EdgeCP res = new EdgeCP
        {
            normalized_t = m_data.GetClosestPoint( pt )
        };

        bool in_edge_found = false;
        bool edge_end = false;
        float min_dist = Mathf.PI * 4;
        res.normalized_t -= Mathf.PI * 2;
        while ( res.normalized_t < Mathf.PI * 6 )
        {
            if ( res.normalized_t >= m_data.t_start && res.normalized_t <= m_data.t_end )
            {
                in_edge_found = true;
                break;
            }

            if ( res.normalized_t < m_data.t_start && ( m_data.t_start - res.normalized_t ) < min_dist )
                min_dist = ( m_data.t_start - res.normalized_t );

            if ( res.normalized_t > m_data.t_end && ( res.normalized_t - m_data.t_end ) < min_dist )
            {
                edge_end = true;
                break;
            }

            res.normalized_t += Mathf.PI * 2;
        }
        if ( ! in_edge_found )
        {
            res.at_edge_end = m_sense == edge_end;
            res.normalized_t = res.at_edge_end.Value ? 1 : 0;
        }
        else
        {
            res.normalized_t = Mathf.InverseLerp( m_data.t_start, m_data.t_end, res.normalized_t );
            if ( !m_sense )
                res.normalized_t = 1 - res.normalized_t;
        }

        res.pt = Eval( res.normalized_t ).pt;

        return res;
    }

    public ICuttableEdge Cut ( float t, bool leave_first_part )
    {
        if ( !m_sense )
            t = 1 - t;
        float unclamped_t = Mathf.Lerp( m_data.t_start, m_data.t_end, t );

        return new CircleEdge( new CircleArc
        {
            center = m_data.center,
            radius = m_data.radius,
            t_start = leave_first_part == m_sense ? m_data.t_start : unclamped_t,
            t_end = leave_first_part == m_sense ? unclamped_t : m_data.t_end
        }, m_sense );
    }

    public float GetLen ( )
    {
        return ( m_data.t_end - m_data.t_start ) * m_data.radius;
    }

    public void OnVisit ( IEdgeVisitor user )
    {
        user.Visit( this );
    }

    public void DBG_Show ( Color color )
    {
        m_data.DBG_Show( color );
    }

    public DBGLine2d DBG_ShowCetonia ( Color color, float thickness )
    {
        throw new System.NotImplementedException();
    }

    public void Connect ( ICuttableEdge other_edge, bool at_start )
    {
        throw new System.NotImplementedException();
    }

    public CircleArc Data
    { get { return m_data; } }
    public bool Sense
    { get { return m_sense; } }

    private CircleArc m_data;
    private bool m_sense;
}

public class LineEdge : ICuttableEdge
{
    public LineEdge ( LineSegment data, bool sense )
    {
        m_data = data;
        m_sense = sense;
    }

    public EdgeCP GetClosestPoint ( Vector2 pt )
    {
        EdgeCP res = new EdgeCP
        {
            normalized_t = m_data.ClosestPointParam( pt )
        };
        if ( res.normalized_t <= 0 || res.normalized_t >= 1 )
            res.at_edge_end = res.normalized_t > 0;

        if ( !m_sense )
        {
            res.normalized_t = 1 - res.normalized_t;

            if ( res.at_edge_end.HasValue )
                res.at_edge_end = !res.at_edge_end;
        }

        if ( res.at_edge_end.HasValue )
            res.normalized_t = res.at_edge_end.Value ? 1 : 0;

        res.pt = Eval( res.normalized_t ).pt;
        return res;
    }

    public ICuttableEdge Cut ( float t, bool first_part )
    {
        Vector2 pt = Eval( t ).pt;

        return new LineEdge( new LineSegment
        {
            p0 = first_part == m_sense ? m_data.p0 : pt,
            p1 = first_part == m_sense ? pt : m_data.p1
        }, m_sense );
    }

    public float GetLen ( )
    {
        return ( m_data.p0 - m_data.p1 ).magnitude;
    }

    public void OnVisit ( IEdgeVisitor user )
    {
        user.Visit( this );
    }

    public EvalRes Eval ( float t )
    {
        if ( !m_sense )
            t = 1.0f - t;

        EvalRes res = new EvalRes
        {
            pt = m_data.p0 * ( 1.0f - t ) + m_data.p1 * t,
            dir = ( m_sense ? res.dir = m_data.p1 - m_data.p0 : m_data.p0 - m_data.p1 ).normalized
        };

        return res;
    }

    public void DBG_Show ( Color color )
    {
        m_data.DBG_Show( color );
    }

    public DBGLine2d DBG_ShowCetonia ( Color color, float thickness )
    {
        DBGLine2d line2d = new DBGLine2d();
        line2d.color = color;
        line2d.p1 = m_data.p0;
        line2d.p2 = m_data.p1;
        line2d.thickness = thickness;
        return line2d;
    }

    private class EdgeVisitor : IEdgeVisitor
    {
        private LineEdge m_edge;
        private bool at_begin;
        public EdgeVisitor( LineEdge edge, bool at_begin )
        {
            m_edge = edge;
            this.at_begin = at_begin;
        }

        public void Visit ( CircleEdge circle )
        {
            throw new System.NotImplementedException();
        }

        public void Visit ( LineEdge line )
        {
            var data = m_edge.Data;

            if ( at_begin )
                data.p0 = line.Data.p1;
            else
                data.p1 = line.Data.p0;

            m_edge.Data = data;
        }
    }

    public void Connect ( ICuttableEdge other_edge, bool at_start )
    {
        other_edge.OnVisit( new EdgeVisitor( this, at_start ) );
    }

    public LineSegment Data
    {
        get { return m_data; }
        set { m_data = value; }
    }
    public bool Sense
    { get { return m_sense; } }

    private LineSegment m_data;
    private bool m_sense;
}