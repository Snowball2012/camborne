using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public static Vector2 ConvertTo2d ( Vector3 vec )
    {
        return new Vector2( vec.x, vec.z );
    }

    public static float Cross ( Vector2 v1, Vector2 v2 )
    {
        return v1.x * v2.y - v1.y * v2.x;
    }

    public static bool TestLeftHemiplane ( Vector2 vec2test, Vector2 line_dir )
    {
        return Vector2.Dot( vec2test, new Vector2( -line_dir.y, line_dir.x ) ) > 0;
    }
}

