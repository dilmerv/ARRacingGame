// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.Utilities.Collections;

namespace Niantic.ARDK.AR
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ParentMergeInfo
    {
      public IntPtr parent;
      public IntPtr* children;
      public UInt32 childrenSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct RawMergedAnchorInfo
    {
      public ParentMergeInfo* parents;
      public UInt32 parentCount;

      public void Release()
      {
        for (int i = 0; i < parentCount; i++)
        {
          var childrenSize = parents[i].childrenSize;
          for (int j = 0; j < childrenSize; j++)
            _NativeARAnchor._ReleaseImmediate(parents[i].children[j]);

          _NativeARAnchor._ReleaseImmediate(parents[i].parent);
        }

        parents = null;
        parentCount = 0;
      }

      // TODO: Use this on _NativeARSession.
      public MergedAnchorInfo ConsumeToMakeMergedAnchorInfo()
      {
        #pragma warning disable 0162
        if (NativeAccess.Mode != NativeAccess.ModeType.Native)
          _ARAnchorFactory._testOnly_DefaultAnchorType = AnchorType.Plane;
        #pragma warning restore 0162

        var mergedAnchors = new Dictionary<IARPlaneAnchor, IARPlaneAnchor[]>();
        for (int i = 0; i < (int)parentCount; i++)
        {
          IntPtr parentNativeHandle = parents[i].parent;
          var childrenSize = parents[i].childrenSize;
          IARPlaneAnchor[] childrenAsPlanes = new IARPlaneAnchor[childrenSize];

          for (int j = 0; j < childrenSize; j++)
          {
            IntPtr childNativeHandle = parents[i].children[j];
            var child = (IARPlaneAnchor)_ARAnchorFactory._FromNativeHandle(childNativeHandle);
            childrenAsPlanes[j] = child;
          }

          var parent = (IARPlaneAnchor)_ARAnchorFactory._FromNativeHandle(parentNativeHandle);
          mergedAnchors.Add(parent, childrenAsPlanes);
        }

        // The _NativeARPlaneAnchors have taken over ownership for the native handles and we're no
        // longer responsible for disposing them, so 0 out all our references.
        parents = null;
        parentCount = 0;

        return new MergedAnchorInfo(mergedAnchors);
      }
    }

    internal struct MergedAnchorInfo:
      IDisposable
    {
      private Dictionary<IARPlaneAnchor, IARPlaneAnchor[]> _mergedAnchors;
      
      internal _ReadOnlyDictionary<IARPlaneAnchor, IARPlaneAnchor[]> MergedAnchors { get; private set; }

      internal MergedAnchorInfo(Dictionary<IARPlaneAnchor, IARPlaneAnchor[]> mergedAnchors)
        : this()
      {
        _mergedAnchors = mergedAnchors;
        MergedAnchors = new _ReadOnlyDictionary<IARPlaneAnchor, IARPlaneAnchor[]>(mergedAnchors);
      }

      public void Dispose()
      {
          foreach (var merge in _mergedAnchors)
          {
            foreach (var child in  merge.Value)
              child.Dispose();

            merge.Key.Dispose();
          }

          _mergedAnchors = null;
          MergedAnchors = null;
      }
    }
}
