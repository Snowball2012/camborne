using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public struct Intersection
{
    public float face_edge_param;
    public float cut_param;
}

public class Intersector
{
    public List<Intersection> Intersect ( IEdge edge1, IEdge edge2 )
    {
        throw new System.NotImplementedException();
    }
}

public class CameraFieldBuilder : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public ICameraField Build ( CameraScene cameraScene, CirclePrimitive player, CirclePrimitive target )
    {
        // 3 steps:
        // 1. Make face from room bounds
        // 2. For each primitive make the occlusion field and cut it from the face
        return new CameraField( null );
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

    private struct TopologyIntersection
    {
        public int loop_idx;
        public int edge_idx;
        public int tool_edge_idx;

        public bool tool_edge_enters;

        public Intersection geom;
    }

    private CameraField CutFromField ( CameraField field, List<ICuttableEdge> edges2cut )
    {
        // 1. for each edge determine if intersection number is even, then find these intersections.
        // Then make new loops.

        List<TopologyIntersection> intersections = FindAllIntersections( field, edges2cut );


        return new CameraField(null);
    }

    private List<TopologyIntersection> FindAllIntersections( CameraField field, List<ICuttableEdge> edges2cut )
    {
        Intersector intersector = new Intersector();

        List<TopologyIntersection> res_list = new List<TopologyIntersection>();

        for ( int tool_edge_idx = 0; tool_edge_idx < edges2cut.Count; ++tool_edge_idx )
        {
            for ( int loop_idx = 0; loop_idx < field.Loops.Count; ++loop_idx )
            {
                var loop = field.Loops[loop_idx];
                for ( int edge_idx = 0; edge_idx < loop.Count; ++edge_idx )
                {
                    var intersections = intersector.Intersect( loop[edge_idx], edges2cut[tool_edge_idx] );
                    TopologyIntersection res;
                    res.loop_idx = loop_idx;
                    res.edge_idx = edge_idx;
                    res.tool_edge_idx = tool_edge_idx;
                    foreach ( var intersection in intersections )
                    {
                        res.geom = intersection;
                        Vector2 face_edge_dir = loop[edge_idx].Eval( intersection.face_edge_param ).dir;
                        res.tool_edge_enters =
                            Vector2.Dot(
                                new Vector2( -face_edge_dir.y, face_edge_dir.x ),
                                edges2cut[tool_edge_idx].Eval( intersection.cut_param ).dir
                                ) > 0;
                        res_list.Add( res );
                    }
                }
            }
        }

        return res_list;
    }

    private List<TopologyIntersection> FilterIntersections ( List<TopologyIntersection> intersections )
    {
        // check all face loop intersections for topological consistency. input should already be sorted.

        if ( intersections.Count == 0 )
            return intersections;

        int last_loop_idx = intersections[0].loop_idx;
        bool last_enters = intersections[0].tool_edge_enters;

        // TODO: finish
        throw new System.NotImplementedException();
        return intersections;
    }
}
