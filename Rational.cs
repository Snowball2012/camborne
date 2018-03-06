using System;
using UnityEngine;

public struct Rational
{
    private long m_n;
    private long m_d;

    public Rational( float num )
    {
        m_d = 10000;
        m_n = (long) (num * 10000.0f);

        Simplify();
    }
    
    public Rational( long n )
    {
        m_d = 1;
        m_n = n;
    }

    public Rational( long n, long d )
    {
        if ( d == 0 )
            throw new SystemException();
        if ( d < 0 )
        {
            d *= -1;
            n *= -1;
        }
        m_n = n;
        m_d = d;

        Simplify();
    }

    public float ToFloat
    {
        get { return (float) ((double)m_n / (double)m_d); }
    }

    public static explicit operator float( Rational n )
    {
        return n.ToFloat;
    }

    public static Rational FromLong( long a )
    {
        return new Rational( a );
    }

    public static Rational operator - ( Rational lhs )
    {
        return new Rational( -lhs.m_n, lhs.m_d );
    }

    public static Rational operator +( Rational lhs, Rational rhs )
    {
        return new Rational( lhs.m_n * rhs.m_d + lhs.m_d * rhs.m_n, lhs.m_d * rhs.m_d );
    }

    public static Rational operator - ( Rational lhs, Rational rhs )
    {
        return new Rational( lhs.m_n * rhs.m_d - lhs.m_d * rhs.m_n, lhs.m_d * rhs.m_d );
    }

    public static Rational operator * ( Rational lhs, Rational rhs )
    {
        return new Rational( lhs.m_n * rhs.m_n, lhs.m_d * rhs.m_d );
    }

    public static Rational operator / ( Rational lhs, Rational rhs )
    {
        return new Rational( lhs.m_n * rhs.m_d, lhs.m_d * rhs.m_n );
    }
    public static bool operator == ( Rational lhs, Rational rhs )
    {
        return lhs.m_n == rhs.m_n && lhs.m_d == rhs.m_d;
    }
    public static bool operator != ( Rational lhs, Rational rhs )
    {
        return lhs.m_n != rhs.m_n || lhs.m_d != rhs.m_d;
    }

    public static bool operator < ( Rational lhs, Rational rhs )
    {
        return ( lhs - rhs ).m_n < 0;
    }
    public static bool operator > ( Rational lhs, Rational rhs )
    {
        return ( lhs - rhs ).m_n > 0;
    }
    public static bool operator <= ( Rational lhs, Rational rhs )
    {
        return ( lhs - rhs ).m_n <= 0;
    }
    public static bool operator >= ( Rational lhs, Rational rhs )
    {
        return ( lhs - rhs ).m_n >= 0;
    }

    private void Simplify ( )
    {
        long[] v = new long[2];

        v[0] = Math.Abs( m_n );

        if ( v[0] == 0 )
        {
            m_d = 1;
            return;
        }

        if ( v[0] == 1 )
            return;

        v[1] = m_d;

        int bigger_idx = v[1] > v[0] ? 1 : 0;

        while ( v[1 - bigger_idx] != 0 )
        {
            v[bigger_idx] = v[bigger_idx] % v[1 - bigger_idx];
            bigger_idx = 1 - bigger_idx;
        }

        m_n /= v[bigger_idx];
        m_d /= v[bigger_idx];
    }
}

public struct Vector2R
{
    public Rational x;
    public Rational y;

    public Vector2R( Rational x, Rational y )
    {
        this.x = x;
        this.y = y;
    }

    public Vector2R( Vector2 flt_vec )
    {
        x = new Rational( flt_vec.x );
        y = new Rational( flt_vec.y );
    }

    public Rational SqrMagnitude
    {
        get { return x * x + y * y; }
    }

    public static Vector2R operator + ( Vector2R lhs, Vector2R rhs )
    {
        return new Vector2R( lhs.x + rhs.x, lhs.y + rhs.y );
    }
    public static Vector2R operator - ( Vector2R lhs, Vector2R rhs )
    {
        return new Vector2R( lhs.x - rhs.x, lhs.y - rhs.y );
    }

    public static Vector2R operator * ( Rational coef, Vector2R rhs )
    {
        return new Vector2R( coef * rhs.x, coef * rhs.y );
    }

    public static Vector2R operator / ( Vector2R lhs, Rational coef )
    {
        return new Vector2R( lhs.x / coef, lhs.y / coef );
    }

    public static Rational Cross ( Vector2R lhs, Vector2R rhs )
    {
        return lhs.x * rhs.y - lhs.y * rhs.x;
    }

    public static Rational Dot ( Vector2R lhs, Vector2R rhs )
    {
        return lhs.x * rhs.x + lhs.y * rhs.y;
    }

    public static bool TestLeftHemiplane ( Vector2R vec2test, Vector2R line_dir )
    {
        return Dot( vec2test, new Vector2R( -line_dir.y, line_dir.x ) ) > Rational.FromLong( 0 );
    }

}