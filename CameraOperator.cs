using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;


public class DenormalizedHermiteSpline
{
    public void Set ( Vector3 p0, Vector3 d0, Vector3 p1, Vector3 d1, float param_len )
    {
        Assert.IsTrue( param_len > 0, "Param length for hermite spline should be strictly positive" );

        this.p0 = p0;
        this.p1 = p1;
        this.d0 = d0;
        this.d1 = d1;

        // hermite splines are defined on [0 .. 1] so we should normalize derivatives to preserve original param_len
        this.d0 /= param_len;
        this.d1 /= param_len;

        t0 = 0;
        t1 = param_len;
    }

    public Vector3 Evaluate ( float t )
    {
        t = Mathf.Clamp( t, t0, t1 );
        t /= t1 - t0;

        float t_3 = t * t * t;
        float t_2 = t * t;

        return p0 * ( 2 * t_3 - 3 * t_2 + 1 ) + d0 * ( t_3 - 2 * t_2 + t )
             + p1 * ( -2 * t_3 + 3 * t_2 )    + d1 * ( t_3 - t_2 );
    }

    public Vector3 EvaluateDeriv ( float t )
    {
        t = Mathf.Clamp( t, t0, t1 );
        t /= t1 - t0;

        float t_3 = t * t * t;
        float t_2 = t * t;

        return ( p0 * ( 6 * t_2 - 6 * t )  + d0 * ( 3 * t_2 - 4 * t + 1 )
               + p1 * ( -6 * t_2 + 6 * t ) + d1 * ( 3 * t_2 - 2 * t ) ) * ( t1 - t0 );
    }

    private Vector3 p0, p1, d0, d1;

    private float t0, t1;
}

// Simple hermite spline - based camera operator. Takes destination point, direction vector and target FoV, builds hermite spline to reach desired state and moves the camera along the curve 
public class CameraOperator : MonoBehaviour
{
    void Start ( )
    {
        if ( m_spline == null )
            m_spline = new DenormalizedHermiteSpline();

        m_spline.Set( transform.position, Vector3.zero, transform.position, Vector3.zero, 1.0f );
        m_local_time = 0;
        m_finish_time = 0;
    }

    void Update ( )
    {
        if ( m_local_time < m_finish_time )
        {
            m_local_time += Time.deltaTime;
            transform.position = m_spline.Evaluate( m_local_time );
        }

        // Todo: change direction and FoV
    }

    public void RefreshDestination ( Vector3 pos, Vector3 dir, float fov, float time_to_reach )
    {
        Vector3 cur_speed;
        {
            if ( m_local_time < m_finish_time )
                cur_speed = m_spline.EvaluateDeriv( m_local_time );
            else
                cur_speed = Vector3.zero;
        }

        m_spline.Set( transform.position, cur_speed, pos, Vector3.zero, time_to_reach );

        m_finish_time = time_to_reach;
        m_local_time = 0;
    }

    private DenormalizedHermiteSpline m_spline = null;
    private float m_local_time = 0;
    private float m_finish_time = 0;
}
