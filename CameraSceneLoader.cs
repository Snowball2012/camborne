using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

// loads static geometry to CameraScene
public class CameraSceneLoader : MonoBehaviour {

    // There should be an object named "room_bounds".
    // Poor design choice, but it's mostly a dummy class for prototyping purposes
    public Transform scene_geometry_parent;
    public CameraScene Scene { get { return m_scene; } }


    // draws the scene every frame
    public bool dbg = false;

    // Use this for initialization
    
    void Start ()
    {
        List<BoxPrimitive> boxes = new List<BoxPrimitive>( scene_geometry_parent.childCount );
        List<CirclePrimitive> circles = new List<CirclePrimitive>( scene_geometry_parent.childCount );

        NullableBox room_bounds = null;

		for ( int i = 0; i < scene_geometry_parent.childCount; ++i )
        {
            GameObject child = scene_geometry_parent.GetChild( i ).gameObject;
            BoxCollider box = child.GetComponent<BoxCollider>();
            CapsuleCollider capsule = child.GetComponent<CapsuleCollider>();

            if ( child.name.Equals( "room_bounds" ) )
            {
                Assert.IsNotNull( box, "room_bounds does not have box collider attached to it!" );

                room_bounds = new NullableBox();
                room_bounds.box = CreatePrimitive( child.transform, box );
            }
            else if ( !child.name.Equals( "floor" ) )
            { 
                if ( box != null )
                {
                    boxes.Add( CreatePrimitive( child.transform, box ) );
                }
                else if ( capsule != null )
                {
                    circles.Add( CreatePrimitive( child.transform, capsule ) );
                }
            }
        }
 
        Assert.IsNotNull( room_bounds, "No room_bounds object in hierarchy!" );

        m_scene = new CameraScene( circles, boxes, room_bounds.box );
	}

    void Update()
    {
        if ( dbg )
            m_scene.DBG_Show();
    }


    // private
    class NullableBox
    {
        public BoxPrimitive box;
    }


    Vector2 ConvertTo2d( Vector3 vec )
    {
        return new Vector2( vec.x, vec.z );
    }


    BoxPrimitive CreatePrimitive( Transform tf, BoxCollider collider )
    {
        BoxPrimitive box = new BoxPrimitive();
        
        box.center = ConvertTo2d( tf.position );
        box.a = collider.size.x;
        box.b = collider.size.z;
        Vector3 euler_angles = tf.rotation.eulerAngles;
        float angle = euler_angles.y;
        box.x_dir.x = Mathf.Cos( angle );
        box.x_dir.y = Mathf.Sin( angle );

        return box;
    }


    CirclePrimitive CreatePrimitive ( Transform tf, CapsuleCollider collider )
    {
        CirclePrimitive res = new CirclePrimitive();

        res.center = ConvertTo2d( tf.position );
        res.radius = collider.radius;

        return res;
    }


    private CameraScene m_scene = null;
}
