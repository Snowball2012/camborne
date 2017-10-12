using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Intersector
{
    private class ImplCollector : IEdgeImplementationUser
    {
        public ImplCollector( Intersector intersector )
        {
            m_intersector = intersector;
            m_e1c = null;
            m_e1l = null;
        }
        Intersector m_intersector;

        public void UseImpl ( CircleEdge circle )
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

        public void UseImpl ( LineEdge line )
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
        
        CircleEdge m_e1c;
        LineEdge m_e1l;

        public List<Intersection> Res
        { get { return m_res; } }
        List<Intersection> m_res;
    }

    public List<Intersection> Intersect ( IEdge target, IEdge tool )
    {
        ImplCollector ic = new ImplCollector( this );
        target.UseImpl( ic );
        tool.UseImpl( ic );

        return ic.Res;
    }

    private float Cross ( Vector2 v1, Vector2 v2 )
    {
        return v1.x * v2.y - v1.y * v2.x;
    }

    private bool TestLeftHemiplane ( Vector2 vec2test, Vector2 line_dir )
    {
        return Vector2.Dot( vec2test, new Vector2( -line_dir.y, line_dir.x ) ) > 0;
    }

    public List<Intersection> Intersect ( LineEdge target, LineEdge tool )
    {
        var res = new List<Intersection>();

        // intersect corresponding lines. 1 or zero intersections
        var target_data = target.Data;
        var tool_data = tool.Data;
        Vector2 dir1 = target_data.p1 - target_data.p0;
        Vector2 dir2 = tool_data.p1 - tool_data.p0;

        if ( Cross( dir1, dir2 ) == 0 ) // parallel or collinear
            return res;

        float target_t = Cross( tool_data.p0 - target_data.p0, dir2 / Cross( dir1, dir2 ) );
        float tool_t = Cross( target_data.p0 - tool_data.p0, dir1 / Cross( dir2, dir1 ) );

        if ( target_t >= -1.0e-5 && target_t <= 1.0 + 1.0e-5
            && tool_t >= -1.0e-5 && tool_t <= 1.0 + 1.0e-5 )
        {
            if ( !target.Sense )
                target_t = 1.0f - target_t;
            if ( !tool.Sense )
                tool_t = 1.0f - tool_t;

            Intersection i = new Intersection();
            i.face_edge_param = target_t;
            i.cut_param = tool_t;
            i.tool_edge_enters = TestLeftHemiplane( dir2, dir1 ) == ( target.Sense == tool.Sense );
            res.Add( i );
        }

        return res;
    }
    public List<Intersection> Intersect ( LineEdge target, CircleEdge tool, bool flip )
    {
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
            if ( spread < 1.0e-7 )
                spread = 1.0e-7f;

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
                        Intersection intersection = new Intersection();
                        intersection.face_edge_param = line_param;
                        intersection.cut_param = Mathf.InverseLerp( tool_data.t_start, tool_data.t_end, corrected_curve_param );
                        if ( !target.Sense )
                            intersection.face_edge_param = 1 - intersection.face_edge_param;
                        if ( !tool.Sense )
                            intersection.cut_param = 1 - intersection.cut_param;

                        if ( !flip )
                        {
                            intersection.tool_edge_enters = TestLeftHemiplane( line2center, target_data.p1 - target_data.p0 );
                        }
                        else
                        {
                            intersection.tool_edge_enters = !TestLeftHemiplane( line2center, target_data.p1 - target_data.p0 );
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
                throw new System.NotImplementedException();

        }
        
        return res;
    }
    public List<Intersection> Intersect ( CircleEdge target, CircleEdge tool )
    {
        return new List<Intersection>();
        throw new System.NotImplementedException();
    }
    
}