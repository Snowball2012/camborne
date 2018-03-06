using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class BooleanOperator
{
    private const int DeadloopMaxIters = 1000;

    private ICetonia m_dbg = null;

    private void EnableDebug ( )
    {
        var go = GameObject.Find( "cetonia" );
        if ( go != null )
            m_dbg = go.GetComponent<Cetonia>();
    }

    private void SendTestLine()
    {
        DBGLine2d test_line;
        test_line.thickness = 0.1;
        test_line.p1 = new Vector2( 0, 0 );
        test_line.p2 = new Vector2( 1, 1 );
        test_line.color = Color.red;
        if ( m_dbg != null )
        {
            m_dbg.StartRecording();
            m_dbg.SendLine( test_line );
            m_dbg.Flush();
        }
    }

    private void DBG_ShowFaceCetonia( Face2D face, Color color )
    {
        m_dbg.StartRecording();

        foreach ( var loop in face.Loops )
            DBG_ShowLoopCetonia( loop, color, 0.05f );

        m_dbg.Flush();
    }

    private void DBG_ShowLoopCetonia( IList<ICuttableEdge> loop, Color color, float thickness )
    {
        foreach ( var edge in loop )
        {
            m_dbg.SendLine( edge.DBG_ShowCetonia( color, thickness ) );
        }
    }

    public Face2D Intersect( Face2D target, List<ICuttableEdge> tool )
    {
        // 1. for each edge determine if intersection number is even, then find these intersections.
        // Then make new loops.
        // TODO : loops without intersections 

        IList<TopologyIntersection> intersections = /*FilterIntersections(*/ FindAllIntersections( target, tool )/*, target, tool )*/;

        List<bool> loop_has_intersections = new List<bool>( target.Loops.Count );
        for ( int i = 0; i < target.Loops.Count; ++i )
            loop_has_intersections.Add( false );

        List<List<ICuttableEdge>> new_loops = new List<List<ICuttableEdge>>();

        if ( intersections.Count == 0 )
        {
            // test point on tool loop, if in target, add it to the target
            if ( target.IsPointInside( tool[0].Eval( 0 ).pt ) )
            {
                new_loops.Add( tool );
                foreach ( var loop in target.Loops )
                    new_loops.Add( loop );
                return new Face2D( new_loops );
            }
            else
            {
                return target;
            }
        }

        BopMap bop_map = new BopMap();
        {
            bop_map.edge2intersections = MakeIntersectionMap( intersections, target.Loops, tool );
            bop_map.target = target.Loops;
            bop_map.tool_edges = tool;
        };

        // find valid intersection and start from it
        bool unprocessed_found = false;

        int deadloop = 0;
        do
        {
            unprocessed_found = false;
            foreach ( var intersection in intersections )
            {
                loop_has_intersections[intersection.LoopIdx] = true;
                if ( intersection.Valid )
                {
                    unprocessed_found = true;
                    var new_loop = MakeLoop( intersection, bop_map );
                    if ( new_loop != null )
                    {
                        if ( new_loop.Count >= 2 )
                        {
                            for ( int i = 0; i < new_loop.Count; ++i )
                                new_loop[i].Connect( new_loop[( i + new_loop.Count - 1 ) % new_loop.Count], true );

                            new_loops.Add( new_loop );
                        }
                    }
                }
                if ( deadloop++ > DeadloopMaxIters )
                {
                    Debug.LogError( "deadloop" );
                    throw new UnityException( "deadloop" );
                }
            }
            if ( deadloop++ > DeadloopMaxIters )
            {
                Debug.LogError( "deadloop" );
                throw new UnityException( "deadloop" );
            }

        } while ( unprocessed_found );

        for ( int i = 0; i < loop_has_intersections.Count; ++i )
        {
            if ( !loop_has_intersections[i] )
                new_loops.Add( target.Loops[i] ); // todo: test if inside
        }

        return new Face2D( new_loops );
    }

    private class TopologyIntersection
    {
        public TopologyIntersection ( int loop_idx, int target_edge_idx, int tool_edge_idx, Intersection geom )
        {
            m_loop_idx = loop_idx;
            m_target_edge_idx = target_edge_idx;
            m_tool_edge_idx = tool_edge_idx;
            m_geom = geom;
            m_valid = true;
        }

        public int LoopIdx
        { get { return m_loop_idx; } }
        int m_loop_idx;

        public int TargetEdgeIdx
        { get { return m_target_edge_idx; } }
        int m_target_edge_idx;

        public int ToolEdgeIdx
        { get { return m_tool_edge_idx; } }
        int m_tool_edge_idx;

        public bool ToolEdgeEnters
        { get { return m_geom.tool_edge_enters; } }

        public bool Valid
        { get { return m_valid; } set { m_valid = value; } }
        private bool m_valid;

        public Intersection Geom
        { get { return m_geom; } }
        Intersection m_geom;

        public void SetCutParam ( Rational cut_param )
        {
            m_geom.cut_param = cut_param;
        }
    }

    private class BopMap
    {
        public IDictionary<ICuttableEdge, IList<TopologyIntersection>> edge2intersections;
        public IList<List<ICuttableEdge>> target;
        public IList<ICuttableEdge> tool_edges;
    }

    private struct LoopSegment
    {
        public TopologyIntersection start;
        public TopologyIntersection end;
        public IEnumerable<ICuttableEdge> edges;
    }

    List<ICuttableEdge> MakeLoop ( TopologyIntersection start_from, BopMap map )
    {
        var res = new List<ICuttableEdge>();

        TopologyIntersection last = start_from;
        int deadloop = 0;
        do
        {
            if ( deadloop++ > DeadloopMaxIters )
            {
                Debug.LogError( "deadloop" );
                throw new UnityException( "deadloop" );
            }
            LoopSegment seg = MakeLoopSegment( last, map );
            last = seg.end;
            res.AddRange( seg.edges );
        } while ( last != start_from );

        // TODO: filter degen loops
        float total_len = 0;
        foreach ( var edge in res )
            total_len += edge.GetLen();

        // strict filter. such loops are useless for camera
        return res.Count == 0 || total_len < 1.0e-2 ? null : res;
    }

    private class EdgeIter : IEnumerator<ICuttableEdge>
    {
        public EdgeIter ( IList<ICuttableEdge> loop, int pos_in_loop )
        {
            m_start_idx = pos_in_loop;
            m_edges = loop;
            Reset();
        }

        public ICuttableEdge Current
        {
            get
            {
                return m_edges[m_cur_idx];
            }
        }

        object IEnumerator.Current
        {
            get { throw new System.NotImplementedException(); }
        }

        public void Dispose ( )
        {
            m_last_try = false;
        }

        public bool MoveNext ( )
        {
            m_cur_idx++;
            if ( m_cur_idx >= m_edges.Count )
                m_cur_idx = 0;

            if ( m_cur_idx == m_start_idx )
                m_last_try = true;
            return ( m_cur_idx != m_start_idx ) != m_last_try;
        }

        public void Reset ( )
        {
            m_cur_idx = m_start_idx;
            m_last_try = false;
        }

        IList<ICuttableEdge> m_edges;
        int m_start_idx;
        int m_cur_idx;
        bool m_last_try = false;
    }

    LoopSegment MakeLoopSegment ( TopologyIntersection start_from, BopMap map )
    {
        if ( start_from.ToolEdgeEnters )
            return MakeLoopSegment( start_from, map, true, new EdgeIter( map.tool_edges, start_from.ToolEdgeIdx ) );
        else
            return MakeLoopSegment( start_from, map, false, new EdgeIter( map.target[start_from.LoopIdx], start_from.TargetEdgeIdx ) );
    }

    LoopSegment MakeLoopSegment ( TopologyIntersection start_from, BopMap map, bool tool_seg, IEnumerator<ICuttableEdge> edge_iterator )
    {
        LoopSegment seg;
        seg.start = null;
        var seg_edges = new List<ICuttableEdge>();
        seg.end = null;
        edge_iterator.Reset();

        int deadloop = 0;
        do
        {
            var cur_edge = edge_iterator.Current;
            var cur_edge_intersections = map.edge2intersections[cur_edge];

            TopologyIntersection start = null;
            TopologyIntersection end = null;

            if ( cur_edge_intersections != null && cur_edge_intersections.Count > 0 )
            {
                if ( cur_edge_intersections.Contains( start_from ) && seg.start == null )
                {
                    seg.start = start_from;
                    start = start_from;
                }

                if ( !( start != null && cur_edge_intersections.Count == 1 ) )
                {
                    bool stop_at_next_is = start == null;

                    foreach ( var intersection in cur_edge_intersections )
                    {
                        if ( stop_at_next_is )
                        {
                            end = intersection;
                            break;
                        }
                        else
                            stop_at_next_is = start.Equals( intersection );
                    }
                }
            }

            if ( start != null || end != null )
                cur_edge = CutEdgeWithIntersections( cur_edge, tool_seg, start, end );
            
            if ( cur_edge.GetLen() > 1.0e-3 )
                seg_edges.Add( cur_edge );

            if ( end != null )
            {
                seg.end = end;
                break;
            }

            if ( deadloop++ > DeadloopMaxIters )
            {
                Debug.Log( "deadloop" );
                throw new UnityException( "deadloop" );
            }

        } while ( edge_iterator.MoveNext() );

        seg.edges = seg_edges;

        seg.start.Valid = false;
        seg.end.Valid = false;

        return seg;
    }

    private ICuttableEdge CutEdgeWithIntersections ( ICuttableEdge edge, bool is_tool, TopologyIntersection start, TopologyIntersection end )
    {
        ICuttableEdge res = edge;

        if ( start != null )
            res = res.Cut( (is_tool ? start.Geom.cut_param : start.Geom.face_edge_param).ToFloat, false );

        if ( end != null )
        {
            float cut_param_end = ( is_tool ? end.Geom.cut_param : end.Geom.face_edge_param ).ToFloat;
            if ( start != null )
            {
                EvalRes end_pt = edge.Eval( cut_param_end );
                cut_param_end = res.GetClosestPoint( end_pt.pt ).normalized_t;
            }
            res = res.Cut( cut_param_end, true );
        }

        return res;
    }

    private class TopologyIntersectionCompare_ByTool : IComparer<TopologyIntersection>
    {
        public int Compare ( TopologyIntersection x, TopologyIntersection y )
        {
            if ( x.Geom.cut_param == y.Geom.cut_param )
                return 0;
            return x.Geom.cut_param < y.Geom.cut_param ? -1 : 1;
        }
    }

    private class TopologyIntersectionCompare_ByTarget : IComparer<TopologyIntersection>
    {
        public int Compare ( TopologyIntersection x, TopologyIntersection y )
        {
            if ( x.Geom.face_edge_param == y.Geom.face_edge_param )
                return 0;
            return x.Geom.face_edge_param < y.Geom.face_edge_param ? -1 : 1;
        }
    }

    private Dictionary<ICuttableEdge, IList<TopologyIntersection>> MakeIntersectionMap ( IEnumerable<TopologyIntersection> intersections, IList<List<ICuttableEdge>> face_loops, IList<ICuttableEdge> tool )
    {
        var res = new Dictionary<ICuttableEdge, IList<TopologyIntersection>>();

        foreach ( var loop in face_loops )
            foreach ( var edge in loop )
                res[edge] = new List<TopologyIntersection>();

        foreach ( var edge in tool )
            res[edge] = new List<TopologyIntersection>();

        foreach ( var intersection in intersections )
        {
            var face_edge = face_loops[intersection.LoopIdx][intersection.TargetEdgeIdx];
            var tool_edge = tool[intersection.ToolEdgeIdx];
            res[face_edge].Add( intersection );
            res[tool_edge].Add( intersection );
        }

        // dirty cast. i think it's acceptable here since we've just created this list
        foreach ( var edge in tool )
            ( (List<TopologyIntersection>)res[edge] ).Sort( new TopologyIntersectionCompare_ByTool() );


        return res;
    }

    private List<TopologyIntersection> FindAllIntersections ( Face2D target, List<ICuttableEdge> edges2cut )
    {
        Intersector intersector = new Intersector();

        List<TopologyIntersection> res_list = new List<TopologyIntersection>();

        for ( int loop_idx = 0; loop_idx < target.Loops.Count; ++loop_idx )
        {
            var loop = target.Loops[loop_idx];
            for ( int target_edge_idx = 0; target_edge_idx < loop.Count; ++target_edge_idx )
            {
                List<TopologyIntersection> sorted_is = new List<TopologyIntersection>();
                for ( int tool_edge_idx = 0; tool_edge_idx < edges2cut.Count; ++tool_edge_idx )
                {
                    var intersections = intersector.Intersect( loop[target_edge_idx], edges2cut[tool_edge_idx] );
                    foreach ( var intersection in intersections )
                    {
                        TopologyIntersection res = new TopologyIntersection(
                            loop_idx, target_edge_idx, tool_edge_idx, intersection );

                        sorted_is.Add( res );
                    }
                }

                sorted_is.Sort( new TopologyIntersectionCompare_ByTarget() );
                res_list.AddRange( sorted_is );
            }
        }

        return res_list;
    }

    private List<TopologyIntersection> FilterIntersections ( IList<TopologyIntersection> intersections, Face2D target, IList<ICuttableEdge> tool )
    {
        // TODO: kill it with fire! No, but seriously, refactor this.

        // check all face loop intersections for topological consistency. input should already be sorted.
        if ( intersections.Count == 0 )
            return new List<TopologyIntersection>();

        IDictionary<ICuttableEdge, List<TopologyIntersection>> edge2intersections = new Dictionary<ICuttableEdge, List<TopologyIntersection>>();

        int last_loop_idx = intersections[0].LoopIdx;
        bool last_enters = intersections[0].ToolEdgeEnters;
        bool first_in_loop_enters = last_enters;

        /*
        {
            int cur_loop_first_edge_idx = 0;

            // 1. Go through every loop and move tool param if ambigious
            for ( int i = 1; i <= intersections.Count; ++i )
            {
                TopologyIntersection intersection = null;
                TopologyIntersection prev = intersections[i - 1];

                if ( i == intersections.Count )
                {
                    intersection = intersections[cur_loop_first_edge_idx];
                }
                else
                {
                    intersection = intersections[i];
                    int cur_loop = intersection.LoopIdx;

                    if ( cur_loop != last_loop_idx )
                    {
                        intersection = intersections[cur_loop_first_edge_idx];
                        cur_loop_first_edge_idx = i;
                        last_loop_idx = cur_loop;
                    }
                    else
                    {
                        intersection = intersections[i];
                    }
                }
                if ( ( intersection.ToolEdgeIdx == prev.ToolEdgeIdx )
                    && ( ( prev.TargetEdgeIdx + 1 ) % target.Loops[prev.LoopIdx].Count == intersection.TargetEdgeIdx )
                    && ( prev.ToolEdgeEnters != intersection.ToolEdgeEnters ) )
                {
                    ICuttableEdge first_edge = target.Loops[prev.LoopIdx][prev.TargetEdgeIdx];
                    ICuttableEdge second_edge = target.Loops[intersection.LoopIdx][intersection.TargetEdgeIdx];
                    bool tool_is_order = Utils.TestLeftHemiplane( second_edge.Eval( 0 ).dir, first_edge.Eval( 1 ).dir ) == prev.ToolEdgeEnters;
                    if ( tool_is_order != ( prev.Geom.cut_param < intersection.Geom.cut_param ) )
                    {
                        float temp = prev.Geom.cut_param;
                        prev.SetCutParam( intersection.Geom.cut_param );
                        intersection.SetCutParam( temp );
                    }
                }
            }
        }*/

        last_loop_idx = intersections[0].LoopIdx;
        last_enters = intersections[0].ToolEdgeEnters;
        first_in_loop_enters = last_enters;

        edge2intersections[tool[intersections[0].ToolEdgeIdx]] = new List<TopologyIntersection>
        {
            intersections[0]
        };

        for ( int i = 1; i < intersections.Count; ++i )
        {
            TopologyIntersection intersection = intersections[i];
            int cur_loop = intersection.LoopIdx;
            bool tool_edge_enters = intersection.ToolEdgeEnters;

            if ( cur_loop != last_loop_idx )
            {
                if ( tool_edge_enters == first_in_loop_enters ) // inconsistent
                    intersection.Valid = false;

                first_in_loop_enters = tool_edge_enters;
                last_loop_idx = cur_loop;
            }
            else if ( tool_edge_enters == last_enters ) // inconsistent
            {
                intersection.Valid = false;
            }

            if ( intersection.Valid )
            {
                if ( !edge2intersections.ContainsKey( tool[intersection.ToolEdgeIdx] ) )
                    edge2intersections[tool[intersection.ToolEdgeIdx]] = new List<TopologyIntersection>();

                edge2intersections[tool[intersection.ToolEdgeIdx]].Add( intersection );
            }
            last_enters = tool_edge_enters;
        }
        // check last loop
        if ( last_enters == first_in_loop_enters )
            intersections[intersections.Count - 1].Valid = false;

        // now filter by tool
        bool first_init = true;
        TopologyIntersection first_tool_is = null;
        foreach ( var edge in tool )
        {
            if ( !edge2intersections.ContainsKey( edge ) )
                continue;

            edge2intersections[edge].Sort( new TopologyIntersectionCompare_ByTool() );

            foreach ( var intersection in edge2intersections[edge] )
            {
                if ( !intersection.Valid )
                    continue;
                if ( first_init )
                {
                    first_in_loop_enters = last_enters = intersection.ToolEdgeEnters;
                    first_init = false;
                    first_tool_is = intersection;
                }
                else
                {
                    bool tool_enters = intersection.ToolEdgeEnters;
                    if ( tool_enters == last_enters ) // inconsistent
                    {
                        intersection.Valid = false;
                    }
                    last_enters = tool_enters;
                }
            }
        }

        if ( last_enters == first_in_loop_enters && first_tool_is != null )
            first_tool_is.Valid = false;

        var res = new List<TopologyIntersection>
        {
            Capacity = intersections.Count
        };
        foreach ( var i in intersections )
            if ( i.Valid )
                res.Add( i );

        return res;
    }
}
