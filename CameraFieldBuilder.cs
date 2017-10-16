using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;


public struct Intersection
{
    public float face_edge_param;
    public float cut_param;
    public bool tool_edge_enters;
}


public class CameraFieldBuilder : MonoBehaviour {

    public CameraSceneLoader scene_loader;
    public CapsuleCollider player;
    public CapsuleCollider target;

    public bool show_occlusion_fields = false;
    public bool show_player = false;
    public bool show_visibility_field = false;
    public bool show_middle_zone = false;
    // Use this for initialization
    void Start () {
		
	}

    Vector2 ConvertTo2d ( Vector3 vec )
    {
        return new Vector2( vec.x, vec.z );
    }

    // Update is called once per frame
    void Update () {
        CirclePrimitive player_primitive = new CirclePrimitive();
        player_primitive.center = ConvertTo2d( player.gameObject.transform.position );
        player_primitive.radius = player.radius * player.gameObject.transform.localScale.magnitude;

        CirclePrimitive target_primitive = new CirclePrimitive();
        target_primitive.center = ConvertTo2d( target.gameObject.transform.position );
        target_primitive.radius = target.radius * target.gameObject.transform.localScale.magnitude;

        if ( show_player )
        {
            player_primitive.DBG_Show( Color.red );
            target_primitive.DBG_Show( Color.red );
        }

        if ( show_occlusion_fields )
        {
            foreach ( var circle_obstacle in scene_loader.Scene.Circles )
            {
                var loop = MakeOcclusionLoop( player_primitive, circle_obstacle );
                foreach ( var edge in loop )
                    edge.DBG_Show( Color.blue );
            }
        }

        if ( show_visibility_field )
        {
            Build( scene_loader.Scene, player_primitive, target_primitive ).DBG_Show( Color.magenta );
        }

        if ( show_middle_zone )
        {
            var loop = MakeMiddleZone( player_primitive, target_primitive );
            foreach ( var edge in loop )
                edge.DBG_Show( Color.yellow );
        }
    }

    public CameraField Build ( CameraScene cameraScene, CirclePrimitive player, CirclePrimitive target )
    {
        // 3 steps:
        // 1. Make face from room bounds
        // 2. For each primitive make the occlusion field and cut it from the face
        CameraField res = MakeStartField( cameraScene.RoomBound );

        foreach ( var circle_obstacle in cameraScene.Circles )
            res = CutFromField( res, MakeOcclusionLoop( player, circle_obstacle ) );

        res = CutFromField( res, MakeOcclusionLoop( player, target ) );

        //foreach ( var circle_obstacle in cameraScene.Circles )
        //    res = CutFromField( res, MakeOcclusionLoop( target, circle_obstacle ) );

        res = CutFromField( res, MakeMiddleZone( player, target ) );

        return res;
    }
    
    public CirclePrimitive MakeMiddlePrimitive( CirclePrimitive player, CirclePrimitive target )
    {
        CirclePrimitive res = new CirclePrimitive();
        Vector2 p2t = target.center - player.center;
        res.radius = ( ( p2t.magnitude + player.radius * 10 + target.radius ) / 2 );
        Vector2 p2t_norm = p2t.normalized;
        res.center = player.center - p2t_norm * player.radius * 5 + p2t_norm * res.radius;
        res.radius *= 1.4f;
        return res;
    }

    private List<ICuttableEdge> MakeMiddleZone ( CirclePrimitive player, CirclePrimitive target )
    {
        // 4 circle edges
        CircleArc arc = new CircleArc();
        CirclePrimitive primitive = MakeMiddlePrimitive( player, target );

        arc.radius = primitive.radius;
        arc.center = primitive.center;
        arc.t_start = 0;
        arc.t_end = Mathf.PI * 2;

        var res = new List<ICuttableEdge>();

        var circle_edge = new CircleEdge( arc, false );
        for ( int i = 0; i < 100; ++i )
        {
            res.Add( new LineEdge( new LineSegment { p0 = circle_edge.Eval( 1.0f * i / 100 ).pt, p1 = circle_edge.Eval( 1.0f * ( i + 1 ) / 100 ).pt }, true ) );
        }

        return res;
    }

    private CameraField MakeStartField ( BoxPrimitive room_bounds )
    {
        List<ICuttableEdge> start_loop = new List<ICuttableEdge>();

        Vector2 y_dir = new Vector2( -room_bounds.x_dir.y, room_bounds.x_dir.x );
        Vector2 av = room_bounds.x_dir * room_bounds.a / 2;
        Vector2 bv = y_dir * room_bounds.b / 2;

        start_loop.Add(
            new LineEdge(
                new LineSegment
                {
                    p0 = room_bounds.center - av - bv,
                    p1 = room_bounds.center + av - bv
                }, true ) );

        start_loop.Add(
            new LineEdge(
                new LineSegment
                {
                    p0 = room_bounds.center + av - bv,
                    p1 = room_bounds.center + av + bv
                }, true ) );

        start_loop.Add(
            new LineEdge(
                new LineSegment
                {
                    p0 = room_bounds.center + av + bv,
                    p1 = room_bounds.center - av + bv
                }, true ) );

        start_loop.Add(
            new LineEdge(
                new LineSegment
                {
                    p0 = room_bounds.center - av + bv,
                    p1 = room_bounds.center - av - bv
                }, true ) );

        return new CameraField( new List<List<ICuttableEdge>> { start_loop } );
    }

    private List<ICuttableEdge> MakeOcclusionLoop ( CirclePrimitive player, CirclePrimitive obstacle )
    {
        Vector2 center2center = player.center - obstacle.center;

        float cp_param = Mathf.Atan2( center2center.y, center2center.x );
        float param_spread = Mathf.Acos( Mathf.Min( ( player.radius + obstacle.radius ) / center2center.magnitude, 1.0f ) );
        CircleEdge circle_edge = new CircleEdge(
            new CircleArc
            {
                center = obstacle.center,
                radius = obstacle.radius,
                t_start = cp_param - param_spread,
                t_end = cp_param + param_spread
            },
            false );

        var circle_end = circle_edge.Eval( 0 );
        LineEdge edge1 = new LineEdge(
            new LineSegment
            {
                p0 = circle_end.pt,
                p1 = circle_end.pt - circle_end.dir * 100 
            },
            false );

        circle_end = circle_edge.Eval( 1 );
        LineEdge edge2 = new LineEdge(
            new LineSegment
            {
                p0 = circle_end.pt,
                p1 = circle_end.pt + circle_end.dir * 100
            },
            true );

        List<ICuttableEdge> res = new List<ICuttableEdge>();
        res.Add( edge1 );

        for ( int i = 0; i < 5; ++i )
        {
            res.Add( new LineEdge( new LineSegment { p0 = circle_edge.Eval( 1.0f * i / 5 ).pt, p1 = circle_edge.Eval( 1.0f * ( i + 1 ) / 5 ).pt }, true ) );
        }
        res.Add( edge2 );

        return res;
    }

    private class TopologyIntersection
    {
        public TopologyIntersection ( int loop_idx, int edge_idx, int tool_edge_idx, Intersection geom )
        {
            m_loop_idx = loop_idx;
            m_edge_idx = edge_idx;
            m_tool_edge_idx = tool_edge_idx;
            m_geom = geom;
            m_valid = true;
        }

        public int LoopIdx
        { get { return m_loop_idx; } }
        int m_loop_idx;

        public int EdgeIdx
        { get { return m_edge_idx; } }
        int m_edge_idx;

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

        public void SetCutParam( float cut_param )
        {
            m_geom.cut_param = cut_param;
        }
    }

    private CameraField CutFromField ( CameraField field, List<ICuttableEdge> edges2cut )
    {
        // 1. for each edge determine if intersection number is even, then find these intersections.
        // Then make new loops.
        // TODO : loops without intersections 

        IList<TopologyIntersection> intersections = FilterIntersections( FindAllIntersections( field, edges2cut ), field, edges2cut );

        List<bool> used_loops = new List<bool>( field.Loops.Count );
        for ( int i = 0; i < field.Loops.Count; ++i )
            used_loops.Add( false );

        List<List<ICuttableEdge>> new_loops = new List<List<ICuttableEdge>>();

        if ( intersections.Count == 0 )
        {
            // test point on tool loop, if in field, add it to the field
            if ( field.IsPointInside( edges2cut[0].Eval( 0 ).pt ) )
            {
                new_loops.Add( edges2cut );
                foreach ( var loop in field.Loops )
                    new_loops.Add( loop );
                return new CameraField( new_loops );
            }
            else
            {
                return field;
            }
        }

        BopMap bop_map = new BopMap();
        {
            bop_map.edge2intersections = MakeIntersectionMap( intersections, field.Loops, edges2cut );
            bop_map.target = field.Loops;
            bop_map.tool_edges = edges2cut;
        };

        // find valid intersection and start from it
        bool unprocessed_found = false;

        int deadloop = 0;
        do
        {
            unprocessed_found = false;
            foreach ( var intersection in intersections )
            {
                used_loops[intersection.LoopIdx] = true;
                if ( intersection.Valid )
                {
                    unprocessed_found = true;
                    var new_loop = MakeLoop( intersection, bop_map );
                    if ( new_loop != null )
                        new_loops.Add( new_loop );
                }
                if ( deadloop++ > 1000 )
                {
                    Debug.LogError( "deadloop" );
                    break;
                }
            }
            if ( deadloop++ > 1000 )
            {
                Debug.LogError( "deadloop" );
                break;
            }

        } while ( unprocessed_found );

        for ( int i = 0; i < used_loops.Count; ++i )
        {
            if ( !used_loops[i] )
                new_loops.Add( field.Loops[i] );
        }

        return new CameraField( new_loops );
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
            if ( deadloop++ > 1000 )
            {
                Debug.LogError( "deadloop" );
                break;
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
        public EdgeIter( IList<ICuttableEdge> loop, int pos_in_loop )
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
        {
            return MakeLoopSegment( start_from, map, true, new EdgeIter( map.tool_edges, start_from.ToolEdgeIdx ) );
        }
        else
        {
            return MakeLoopSegment( start_from, map, false, new EdgeIter( map.target[start_from.LoopIdx], start_from.EdgeIdx ) );
        }
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

            // todo : check for degeneracy
            if ( cur_edge.GetLen() > 1.0e-6 )
                seg_edges.Add( cur_edge );

            if ( end != null )
            {
                seg.end = end;
                break;
            }

            if ( deadloop++ > 1000 )
            {
                Debug.Log( "deadloop" );
                break;
            }

        } while ( edge_iterator.MoveNext() );

        seg.edges = seg_edges;

        seg.start.Valid = false;
        seg.end.Valid = false;

        return seg;
    }

    private ICuttableEdge CutEdgeWithIntersections ( ICuttableEdge edge, bool tool_seg, TopologyIntersection start, TopologyIntersection end )
    {
        ICuttableEdge res = edge;

        if ( start != null )
            res = res.Cut( tool_seg ? start.Geom.cut_param : start.Geom.face_edge_param, false );

        if ( end != null )
        {
            float cut_param_end = tool_seg ? end.Geom.cut_param : end.Geom.face_edge_param;
            if ( start != null )
            {
                EvalRes end_pt = edge.Eval( cut_param_end );
                cut_param_end = res.GetClosestPoint( end_pt.pt ).normalized_t;
            }
            res = res.Cut( cut_param_end, true );
        }

        return res;
    }

    private class TopologyIntersectionComparer : IComparer<TopologyIntersection>
    {
        public int Compare ( TopologyIntersection x, TopologyIntersection y )
        {
            if ( x.Geom.cut_param == y.Geom.cut_param )
                return 0;
            return x.Geom.cut_param < y.Geom.cut_param ? -1 : 1;
        }
    }

    private class TopologyIntersectionComparer_ByTarget : IComparer<TopologyIntersection>
    {
        public int Compare ( TopologyIntersection x, TopologyIntersection y )
        {
            if ( x.Geom.face_edge_param == y.Geom.face_edge_param )
                return 0;
            return x.Geom.face_edge_param < y.Geom.face_edge_param ? -1 : 1;
        }
    }

    private Dictionary<ICuttableEdge, IList<TopologyIntersection>> MakeIntersectionMap( IEnumerable<TopologyIntersection> intersections, IList<List<ICuttableEdge>> face_loops, IList<ICuttableEdge> tool )
    {
        var res = new Dictionary<ICuttableEdge, IList<TopologyIntersection>>();

        foreach ( var loop in face_loops )
            foreach ( var edge in loop )
                res[edge] = new List<TopologyIntersection>();

        foreach ( var edge in tool )
            res[edge] = new List<TopologyIntersection>();

        foreach ( var intersection in intersections )
        {
            var face_edge = face_loops[intersection.LoopIdx][intersection.EdgeIdx];
            var tool_edge = tool[intersection.ToolEdgeIdx];
            res[face_edge].Add( intersection );
            res[tool_edge].Add( intersection );
        }

        // dirty cast. i think it's acceptable here since we've just created this list
        foreach ( var edge in tool )
            ( (List<TopologyIntersection>)res[edge] ).Sort( new TopologyIntersectionComparer() );


        return res;
    }

    private List<TopologyIntersection> FindAllIntersections( CameraField field, List<ICuttableEdge> edges2cut )
    {
        Intersector intersector = new Intersector();

        List<TopologyIntersection> res_list = new List<TopologyIntersection>();

        for ( int loop_idx = 0; loop_idx < field.Loops.Count; ++loop_idx )
        {
            var loop = field.Loops[loop_idx];
            for ( int edge_idx = 0; edge_idx < loop.Count; ++edge_idx )
            {
                List<TopologyIntersection> sorted_is = new List<TopologyIntersection>();
                for ( int tool_edge_idx = 0; tool_edge_idx < edges2cut.Count; ++tool_edge_idx )
                {
                    var intersections = intersector.Intersect( loop[edge_idx], edges2cut[tool_edge_idx] );
                    foreach ( var intersection in intersections )
                    {
                        TopologyIntersection res = new TopologyIntersection(
                            loop_idx, edge_idx, tool_edge_idx, intersection );

                        sorted_is.Add( res );
                    }
                }

                sorted_is.Sort( new TopologyIntersectionComparer_ByTarget() );
                res_list.AddRange( sorted_is );
            }
        }

        return res_list;
    }

    private List<TopologyIntersection> FilterIntersections ( IList<TopologyIntersection> intersections, CameraField field, IList<ICuttableEdge> tool )
    {
        // check all face loop intersections for topological consistency. input should already be sorted.
        if ( intersections.Count == 0 )
            return new List<TopologyIntersection>();

        IDictionary<ICuttableEdge, List<TopologyIntersection>> edge2intersections = new Dictionary<ICuttableEdge, List<TopologyIntersection>>();

        int last_loop_idx = intersections[0].LoopIdx;
        bool last_enters = intersections[0].ToolEdgeEnters;
        bool first_in_loop_enters = last_enters;

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
                    && ( ( prev.EdgeIdx + 1 ) % field.Loops[prev.LoopIdx].Count == intersection.EdgeIdx )
                    && ( prev.ToolEdgeEnters != intersection.ToolEdgeEnters ) )
                {
                    ICuttableEdge first_edge = field.Loops[prev.LoopIdx][prev.EdgeIdx];
                    ICuttableEdge second_edge = field.Loops[intersection.LoopIdx][intersection.EdgeIdx];
                    bool tool_is_order = CameraField.TestLeftHemiplane( second_edge.Eval( 0 ).dir, first_edge.Eval( 1 ).dir ) == prev.ToolEdgeEnters;
                    if ( tool_is_order != ( prev.Geom.cut_param < intersection.Geom.cut_param ) )
                    {
                        float temp = prev.Geom.cut_param;
                        prev.SetCutParam( intersection.Geom.cut_param );
                        intersection.SetCutParam( temp );
                    }
                }
            }
        }

        last_loop_idx = intersections[0].LoopIdx;
        last_enters = intersections[0].ToolEdgeEnters;
        first_in_loop_enters = last_enters;

        edge2intersections[tool[intersections[0].ToolEdgeIdx]] = new List<TopologyIntersection>();
        edge2intersections[tool[intersections[0].ToolEdgeIdx]].Add( intersections[0] );

        int items_filtered = 0;
        for ( int i = 1; i < intersections.Count; ++i )
        {
            TopologyIntersection intersection = intersections[i];
            int cur_loop = intersection.LoopIdx;
            bool tool_edge_enters = intersection.ToolEdgeEnters;

            if ( cur_loop != last_loop_idx )
            {
                if ( tool_edge_enters == first_in_loop_enters ) // inconsistent
                {
                    intersection.Valid = false;
                    items_filtered++;
                }
                first_in_loop_enters = tool_edge_enters;
                last_loop_idx = cur_loop;
            }
            else
            {
                if ( tool_edge_enters == last_enters ) // inconsistent
                {
                    intersection.Valid = false;
                    items_filtered++;
                }
            }

            if ( intersection.Valid )
            {
                if ( ! edge2intersections.ContainsKey( tool[intersection.ToolEdgeIdx] ) )
                    edge2intersections[tool[intersection.ToolEdgeIdx]] = new List<TopologyIntersection>();

                edge2intersections[tool[intersection.ToolEdgeIdx]].Add( intersection );
            }
            last_enters = tool_edge_enters;
        }
        // check last loop
        if ( last_enters == first_in_loop_enters )
        {
            intersections[intersections.Count - 1].Valid = false;
            items_filtered++;
        }

        // now filter by tool
        bool first_init = true;
        TopologyIntersection first_tool_is = null;
        foreach ( var edge in tool )
        {
            if ( ! edge2intersections.ContainsKey( edge ) )
                continue;

            edge2intersections[edge].Sort( new TopologyIntersectionComparer() );

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
                        items_filtered++;
                    }
                    last_enters = tool_enters;
                }
            }
        }

        if ( last_enters == first_in_loop_enters && first_tool_is != null )
        {
            first_tool_is.Valid = false;
            items_filtered++;
        }

        Debug.Log( new StringBuilder( "intersections filtered in bop: " ).Append( items_filtered ).ToString() );

        var res = new List<TopologyIntersection>();
        res.Capacity = intersections.Count;
        foreach ( var i in intersections )
            if ( i.Valid )
                res.Add( i );

        return res;
    }
}
