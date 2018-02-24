using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

using ctConnectionHandle = System.UInt64;

// geometry debugger
public struct DBGLine2d
{
    public Vector2 p1, p2;
    public Color color;
    public double thickness;
}

public interface ICetonia
{
    void StartRecording ( );
    void Flush ( );
    void SendLine ( DBGLine2d line );
}

public class Cetonia : MonoBehaviour, ICetonia {

    private enum ctError
    {
        CT_OK = 0,
        CT_GeneralError = 1,
        CT_NotImplemented = 2,
        CT_InvalidArgument = 3
    }

    private struct ctVector2d
    {
        public double x;
        public double y;
    }

    private struct ctColor
    {
        public double r;
        public double g;
        public double b;
    }

    private struct ctTokenLine2d
    {
        public ctVector2d p1, p2;
        public ctColor color;
        public double thickness;
    }


    [DllImport( "Assets/dlls/cetonia.dll" )]
    private static extern ctError ctCreateConnection ( ref ctConnectionHandle connection );
    [DllImport( "Assets/dlls/cetonia.dll" )]
    private static extern ctError ctCloseConnection ( ctConnectionHandle connection );
    [DllImport( "Assets/dlls/cetonia.dll" )]
    private static extern ctError ctBeginRecording ( ctConnectionHandle connection, System.UInt64 estimated_size );
    [DllImport( "Assets/dlls/cetonia.dll" )]
    private static extern ctError ctFlush ( ctConnectionHandle connection );
    [DllImport( "Assets/dlls/cetonia.dll" )]
    private static extern ctError ctSendLine2d ( ctConnectionHandle connection, ref ctTokenLine2d line );

    private ctConnectionHandle m_connection;
    private bool m_valid;
    
    void Awake () {
        var res = ctCreateConnection( ref m_connection );
        m_valid = res == ctError.CT_OK;
	}

    void OnDestroy ()
    {
        if ( m_valid )
            ctCloseConnection( m_connection );
    }

    public void StartRecording ( )
    {
        if ( !m_valid )
            Debug.Log( "connection to cetonia is invalid" );

        ctBeginRecording( m_connection, 1024 );
    }

    public void Flush ( )
    {
        if ( !m_valid )
            Debug.Log( "connection to cetonia is invalid" );

        ctFlush( m_connection );
    }

    public void SendLine ( DBGLine2d line )
    {
        if ( !m_valid )
            Debug.Log( "connection to cetonia is invalid" );

        ctTokenLine2d ct_line;
        ct_line.thickness = line.thickness;

        ctVector2d pt;
        pt.x = line.p1.x;
        pt.y = line.p1.y;
        ct_line.p1 = pt;
        pt.x = line.p2.x;
        pt.y = line.p2.y;
        ct_line.p2 = pt;
        ct_line.color.r = line.color.r;
        ct_line.color.g = line.color.g;
        ct_line.color.b = line.color.b;

        ctSendLine2d( m_connection, ref ct_line );
    }
}
