using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Intersection
{
    public Rational face_edge_param;
    public Rational cut_param;
    public bool tool_edge_enters;
}

public class Intersector
{
    private class EdgeVisitor : IEdgeVisitor
    {
        public EdgeVisitor( Intersector intersector )
        {
            m_intersector = intersector;
            m_e1c = null;
            m_e1l = null;
        }

        public void Visit ( CircleEdge circle )
        {
            if ( m_e1c != null || m_e1l != null )
            {
                if ( m_e1c != null )
                    m_res = m_intersector.Intersect( m_e1c, circle );
                else
                    m_res = m_intersector.Intersect( m_e1l, circle, false );
            }
            else
            {
                m_e1c = circle;
            }
        }

        public void Visit ( LineEdge line )
        {
            if ( m_e1c != null || m_e1l != null )
            {
                if ( m_e1c != null )
                    m_res = m_intersector.Intersect( line, m_e1c, true );
                else
                    m_res = m_intersector.Intersect( m_e1l, line );
            }
            else
            {
                m_e1l = line;
            }
        }
        
        public List<Intersection> Res
        { get { return m_res; } }

        Intersector m_intersector;
        CircleEdge m_e1c;
        LineEdge m_e1l;
        List<Intersection> m_res;
    }

    public List<Intersection> Intersect ( IEdge target, IEdge tool )
    {
        EdgeVisitor ic = new EdgeVisitor( this );
        target.OnVisit( ic );
        tool.OnVisit( ic );

        return ic.Res;
    }

    
    public List<Intersection> Intersect ( LineEdge target, LineEdge tool )
    {
        var res = new List<Intersection>();

        // intersect corresponding lines. 1 or zero intersections
        var target_data = target.Data;
        var tool_data = tool.Data;

        Vector2R target_p0 = new Vector2R( target_data.p0 );
        Vector2R target_p1 = new Vector2R( target_data.p1 );

        Vector2R tool_p0 = new Vector2R( tool_data.p0 );
        Vector2R tool_p1 = new Vector2R( tool_data.p1 );

        Vector2R dir1 = target_p1 - target_p0;
        Vector2R dir2 = tool_p1 - tool_p0;

        if ( dir1.SqrMagnitude == Rational.FromLong( 0 )
            || dir2.SqrMagnitude == Rational.FromLong( 0 ) ) // one vector is degenerate
            return res;

        if ( Vector2R.Cross( dir1, dir2 ) == new Rational( 0 ) ) // parallel or collinear
            return res;

        Rational target_t = Vector2R.Cross( tool_p0 - target_p0, dir2 / Vector2R.Cross( dir1, dir2 ) );
        Rational tool_t = Vector2R.Cross( target_p0 - tool_p0, dir1 / Vector2R.Cross( dir2, dir1 ) );

        if ( target_t >= Rational.FromLong( 0 ) && target_t <= Rational.FromLong( 1 )
            && tool_t >= Rational.FromLong( 0 ) && tool_t <= Rational.FromLong( 1 ) )
        {
            if ( !target.Sense )
                target_t = Rational.FromLong( 1 ) - target_t;
            if ( !tool.Sense )
                tool_t = Rational.FromLong( 1 ) - tool_t;

            Intersection i = new Intersection
            {
                face_edge_param = target_t,
                cut_param = tool_t,
                tool_edge_enters = Vector2R.TestLeftHemiplane( dir2, dir1 ) == ( target.Sense == tool.Sense )
            };
            res.Add( i );
        }

        return res;
    }
    public List<Intersection> Intersect ( LineEdge target, CircleEdge tool, bool flip )
    {
        /*
        var target_data = target.Data;
        var tool_data = tool.Data;

        float line_closest_param = target_data.ClosestPointParam( tool_data.center );
        Vector2 line2center = ( target_data.Eval( line_closest_param ) - tool_data.center );
        float line2center_dist = line2center.magnitude;

        var res = new List<Intersection>();
        if ( line2center_dist <= tool_data.radius )
        {
            float base_param = tool_data.GetClosestPoint( target_data.Eval( line_closest_param ) );
            float spread = Mathf.Acos( line2center_dist / tool_data.radius );
            if ( spread < 1.0e-3 )
                spread = 1.0e-3f;

            for ( int i = -1; i <= 1; i += 2 )
            {
                float curve_param = base_param + spread * i;

                float line_param = target_data.ClosestPointParam( tool_data.Eval( curve_param ) );
                if ( line_param > -1.0e-5 && line_param < 1.0 + 1.0e-5 )
                {
                    bool in_edge_found = false;
                    float min_dist = Mathf.PI * 4;
                    curve_param -= Mathf.PI * 2;
                    float corrected_curve_param = curve_param;
                    while ( curve_param < Mathf.PI * 6 )
                    {
                        if ( curve_param >= tool_data.t_start && curve_param <= tool_data.t_end )
                        {
                            corrected_curve_param = curve_param;
                            in_edge_found = true;
                            break;
                        }

                        if ( curve_param < tool_data.t_start && ( tool_data.t_start - curve_param ) < min_dist )
                        {
                            corrected_curve_param = curve_param;
                            min_dist = ( tool_data.t_start - curve_param );
                        }

                        if ( curve_param > tool_data.t_end && ( curve_param - tool_data.t_end ) < min_dist )
                        {
                            corrected_curve_param = curve_param;
                            min_dist = ( curve_param - tool_data.t_end );
                            break;
                        }

                        curve_param += Mathf.PI * 2;
                    }

                    if ( in_edge_found || min_dist < 1.0e-5 )
                    {
                        Intersection intersection = new Intersection
                        {
                            face_edge_param = line_param,
                            cut_param = Mathf.InverseLerp( tool_data.t_start, tool_data.t_end, corrected_curve_param )
                        };
                        if ( !target.Sense )
                            intersection.face_edge_param = 1 - intersection.face_edge_param;
                        if ( !tool.Sense )
                            intersection.cut_param = 1 - intersection.cut_param;

                        if ( !flip )
                        {
                            intersection.tool_edge_enters = Utils.TestLeftHemiplane( line2center, target_data.p1 - target_data.p0 );
                        }
                        else
                        {
                            intersection.tool_edge_enters = !Utils.TestLeftHemiplane( line2center, target_data.p1 - target_data.p0 );
                        }

                        {
                            if ( i > 0 )
                                intersection.tool_edge_enters = !intersection.tool_edge_enters;
                            if ( ! target.Sense )
                                intersection.tool_edge_enters = !intersection.tool_edge_enters;
                            if ( ! tool.Sense )
                                intersection.tool_edge_enters = !intersection.tool_edge_enters;

                            if ( flip )
                            {
                                float temp = intersection.cut_param;
                                intersection.cut_param = intersection.face_edge_param;
                                intersection.face_edge_param = temp;
                            }

                            res.Add( intersection );
                        }
                    }
                }
            }

            if ( res.Count == 2 )
            {

                if ( flip && res[0].face_edge_param > res[1].face_edge_param 
                    || !flip && res[0].cut_param > res[1].cut_param )
                {
                    var temp = res[0];
                    res[0] = res[1];
                    res[1] = temp;
                }
            }

        }
        */
        throw new System.NotImplementedException();
    }
    public List<Intersection> Intersect ( CircleEdge target, CircleEdge tool )
    {
        throw new System.NotImplementedException();
    }
    
}