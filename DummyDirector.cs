using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyDirector : MonoBehaviour {

    public CameraOperator cam_operator;
    public List<Transform> dst_pos;
    public float time;
	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
        if ( Input.anyKeyDown )
        {
            cam_operator.RefreshDestination( dst_pos[m_pos].position, Vector3.zero, 0, time );
            m_pos = ( m_pos + 1 ) % dst_pos.Count;
        }
        cam_operator.DBG_Show(0);
    }

    private int m_pos = 0;
}
