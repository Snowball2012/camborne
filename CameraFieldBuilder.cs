using System.Collections;
using System.Collections.Generic;
using UnityEngine;


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


    // Update is called once per frame
    void Update () {
        CirclePrimitive player_primitive = new CirclePrimitive();
        player_primitive.center = Utils.ConvertTo2d( player.gameObject.transform.position );
        player_primitive.radius = player.radius * player.gameObject.transform.localScale.magnitude;

        CirclePrimitive target_primitive = new CirclePrimitive();
        target_primitive.center = Utils.ConvertTo2d( target.gameObject.transform.position );
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

    public Face2D Build ( CameraScene cameraScene, CirclePrimitive player, CirclePrimitive target )
    {
        // 3 steps:
        // 1. Make face from room bounds
        // 2. For each primitive make the occlusion field and cut it from the face
        // 3. Cut middle zone from the face, in this zone both the player and the target are visible,
        //    but there aren't any camera angle to see them both
        Face2D res = MakeStartField( cameraScene.RoomBound );

        BooleanOperator bop = new BooleanOperator();

        foreach ( var circle_obstacle in cameraScene.Circles )
            res = bop.Intersect( res, MakeOcclusionLoop( player, circle_obstacle ) );

        res = bop.Intersect( res, MakeOcclusionLoop( player, target ) );

        //foreach ( var circle_obstacle in cameraScene.Circles )
        //    res = CutFromField( res, MakeOcclusionLoop( target, circle_obstacle ) );

        res = bop.Intersect( res, MakeMiddleZone( player, target ) );

        return res;
    }
    
    public CirclePrimitive MakeMiddlePrimitive( CirclePrimitive player, CirclePrimitive target )
    {
        CirclePrimitive res = new CirclePrimitive();
        Vector2 p2t = target.center - player.center;
        res.radius = ( ( p2t.magnitude + player.radius * 5 + target.radius*3 ) / 2 );
        Vector2 p2t_norm = p2t.normalized;
        res.center = player.center - p2t_norm * player.radius * 5 + p2t_norm * res.radius;
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

    private Face2D MakeStartField ( BoxPrimitive room_bounds )
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

        return new Face2D( new List<List<ICuttableEdge>> { start_loop } );
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
}
