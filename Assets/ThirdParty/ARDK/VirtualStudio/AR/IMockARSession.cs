// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.SLAM;

namespace Niantic.ARDK.VirtualStudio.AR
{
  public interface IMockARSession:
    IARSession
  {
    void UpdateFrame(IARFrame frame);

    void UpdateAnchor(IARAnchor anchor);
    void MergeAnchors(IARAnchor parent, IARAnchor[] children);

    void AddMap(IARMap map);
    void UpdateMap(IARMap map);
  }
}
