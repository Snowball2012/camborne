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

public interface IEdgeImplementationUser
{
    void UseImpl ( CircleEdge circle );
    void UseImpl ( LineEdge line );
}

public interface IEdge
{
    EvalRes Eval ( float normalized_t );
    EdgeCP GetClosestPoint ( Vector2 pt );
    void UseImpl ( IEdgeImplementationUser user );
}

public interface ICuttableEdge : IEdge
{
    ICuttableEdge Cut ( float t, bool first_part );
}

public class CircleEdge : ICuttableEdge
{
    CircleEdge ( CircleArc data, bool sense )
    {
        m_data = data;
        m_sense = sense;
    }

    public EvalRes Eval ( float t )
    {
        throw new System.NotImplementedException();
    }

    public EdgeCP GetClosestPoint ( Vector2 pt )
    {
        throw new System.NotImplementedException();
    }

    public ICuttableEdge Cut ( float t, bool first_part )
    {
        throw new System.NotImplementedException();
    }

    public void UseImpl ( IEdgeImplementationUser user )
    {
        user.UseImpl( this );
    }

    public CircleArc Data
    {
        get { return m_data; }
    }

    private bool m_sense;
    private CircleArc m_data;
}

public class LineEdge : ICuttableEdge
{
    public LineEdge ( LineSegment data, bool sense )
    {
        m_data = data;
        m_sense = sense;
    }

    public EvalRes Eval ( double t )
    {
        throw new System.NotImplementedException();
    }

    public EdgeCP GetClosestPoint ( Vector2 pt )
    {
        throw new System.NotImplementedException();
    }

    public ICuttableEdge Cut ( float t, bool first_part )
    {
        throw new System.NotImplementedException();
    }

    public void UseImpl ( IEdgeImplementationUser user )
    {
        user.UseImpl( this );
    }

    public EvalRes Eval ( float normalized_t )
    {
        throw new System.NotImplementedException();
    }

    private LineSegment m_data;
    private bool m_sense;
}