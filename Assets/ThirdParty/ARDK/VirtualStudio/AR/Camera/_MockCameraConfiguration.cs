using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.AR
{
  internal static class _MockCameraConfiguration
  {
    // Camera control keys
    private const string FPS_KEY = "ARDK_Mock_FPS";
    private const string MOVESPEED_KEY = "ARDK_Mock_Movespeed";
    private const string LOOKSPEED_KEY = "ARDK_Mock_Lookspeed";
    private const string SCROLLDIRECTION_KEY = "ARDK_Mock_ScrollDirection";
    
    private const int _DefaultFps = 30;
    private const float _DefaultMoveSpeed = 10f;
    private const int _DefaultLookSpeed = 180;
    private const int _DefaultScrollDirection = -1;

    internal static int FPS
    {
      get { return PlayerPrefs.GetInt(FPS_KEY, _DefaultFps); }
      set { PlayerPrefs.SetInt(FPS_KEY, value);}
    }

    internal static float MoveSpeed
    {
      get { return PlayerPrefs.GetFloat(MOVESPEED_KEY, _DefaultMoveSpeed); }
      set { PlayerPrefs.SetFloat(MOVESPEED_KEY, value);}
    }

    internal static int LookSpeed
    {
      get { return PlayerPrefs.GetInt(LOOKSPEED_KEY, _DefaultLookSpeed); }
      set { PlayerPrefs.SetInt(LOOKSPEED_KEY, value);}
    }
    
    internal static int ScrollDirection
    {
      get { return PlayerPrefs.GetInt(SCROLLDIRECTION_KEY, _DefaultScrollDirection); }
      set { PlayerPrefs.SetInt(SCROLLDIRECTION_KEY, value);}
    }
  }
}
