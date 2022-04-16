using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.Remote
{
  internal static class _RemoteBufferConfiguration
  {
    private const string _REMOTE_IMAGE_COMPRESSION = "ARDK_Image_Compression";
    private const string _REMOTE_IMAGE_FRAMERATE = "ARDK_Image_Framerate";
    private const string _REMOTE_AWARENESS_FRAMERATE = "ARDK_Awareness_Framerate";

    private const int _DefaultImageCompression = 30;
    private const int _DefaultImageFramerate = 12;
    private const int _DefaultAwarenessFramerate = 10;

    public static int ImageCompression
    {
      get
      {
        return PlayerPrefs.GetInt(_REMOTE_IMAGE_COMPRESSION, _DefaultImageCompression);
      }
      set
      {
        PlayerPrefs.SetInt(_REMOTE_IMAGE_COMPRESSION, value);
      }
    }

    public static int ImageFramerate
    {
      get
      {
        return PlayerPrefs.GetInt(_REMOTE_IMAGE_FRAMERATE, _DefaultImageFramerate);
      }
      set
      {
        PlayerPrefs.SetInt(_REMOTE_IMAGE_FRAMERATE, value);
      }
    }

    public static int AwarenessFramerate
    {
      get
      {
        return PlayerPrefs.GetInt(_REMOTE_AWARENESS_FRAMERATE, _DefaultAwarenessFramerate);
      }
      set
      {
        PlayerPrefs.SetInt(_REMOTE_AWARENESS_FRAMERATE, value);
      }
    }
  }
}
