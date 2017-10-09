using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleDirector : MonoBehaviour {
    
        public CameraOperator cam_operator;
        public Transform anchor;
        public float time;
        // Use this for initialization
        void Start ()
        {
        }

        // Update is called once per frame
        void Update ()
        {
            cam_operator.RefreshDestination(anchor.position, Vector3.zero, 0, time);
            cam_operator.DBG_Show(0);
        }
    
}
