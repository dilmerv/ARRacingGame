using UnityEngine;

namespace Niantic.ARDK.Helpers
{
  internal class SavedRenderingSettings
  {
    /// The previous Game target frame rate before this script initialized -- used to restore
    /// the previous frame rate when the script is destroyed.
    private int _targetFrameRate;

    /// The previous Game sleep timeout before this script initialized -- used to restore
    /// the previous sleep timeout when the script is destroyed.
    private int _sleepTimeout;

    /// We need to override VSyncCount as well to use Application.targetFrameRate
    /// https://docs.unity3d.com/ScriptReference/QualitySettings-vSyncCount.html
    private int _vSyncCount;

    public SavedRenderingSettings(int targetFrameRate, int sleepTimeout, int vSyncCount)
    {
      _targetFrameRate = targetFrameRate;
      _sleepTimeout = sleepTimeout;
      _vSyncCount = vSyncCount;
    }

    public void Apply()
    {
      QualitySettings.vSyncCount = _vSyncCount;
      Application.targetFrameRate = _targetFrameRate;
      Screen.sleepTimeout = _sleepTimeout;
    }
  }

}
