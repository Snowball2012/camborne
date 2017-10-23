using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleDirector : MonoBehaviour {
    
    public CameraOperator cam_operator;
    public Transform anchor;
    public CameraFieldBuilder field_builder;
    public float time;
    public Transform lookat;
    // Use this for initialization
    void Start ()
    {
    }

    // Update is called once per frame
    void Update ()
    {
        var player = field_builder.player;
        var target = field_builder.target;
        CirclePrimitive player_primitive = new CirclePrimitive();
        player_primitive.center = ConvertTo2d( player.gameObject.transform.position );
        player_primitive.radius = player.radius * player.gameObject.transform.localScale.magnitude;

        CirclePrimitive target_primitive = new CirclePrimitive();
        target_primitive.center = ConvertTo2d( target.gameObject.transform.position );
        target_primitive.radius = target.radius * target.gameObject.transform.localScale.magnitude;

        Face2D field = field_builder.Build( field_builder.scene_loader.Scene, player_primitive, target_primitive );

        Vector2 anchor_pos = ConvertTo2d( anchor.position );
        if ( ! field.IsPointInside( anchor_pos ) )
        {
            anchor_pos = field.GetClosestPointOnBorder( anchor_pos );            
        }

        var new_lookat = field_builder.MakeMiddlePrimitive( player_primitive, target_primitive ).center;

        lookat.position = new Vector3( new_lookat.x, lookat.position.y, new_lookat.y );

        cam_operator.RefreshDestination( new Vector3( anchor_pos.x, anchor.position.y, anchor_pos.y ), Vector3.zero, 0, time);
        cam_operator.DBG_Show(0);
    }


    Vector2 ConvertTo2d ( Vector3 vec )
    {
        return new Vector2( vec.x, vec.z );
    }
}
