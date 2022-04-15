// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Internals;

using System;
using System.Runtime.InteropServices;

using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.AR.Awareness.Semantics
{
  internal sealed class _NativeSemanticBuffer:
    _NativeAwarenessBufferBase<UInt32>,
    ISemanticBuffer
  {
    public const int BitsPerPixel = sizeof(UInt32) * 8;

    private string[] _channelNames;

    static _NativeSemanticBuffer()
    {
      Platform.Init();
    }

    internal _NativeSemanticBuffer(IntPtr nativeHandle, float worldScale, CameraIntrinsics intrinsics)
      : base
      (
        nativeHandle,
        worldScale,
        GetNativeWidth(nativeHandle),
        GetNativeHeight(nativeHandle),
        IsNativeKeyframe(nativeHandle),
        intrinsics
      )
    {
    }

    public uint ChannelCount
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return _SemanticBuffer_GetNumberChannels(_nativeHandle);

#pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
      }
    }

    public string[] ChannelNames
    {
      get
      {
        if (_channelNames == null)
        {
          if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          {
            var ptrNames = new IntPtr[ChannelCount];
            if (ChannelCount > 0 && _SemanticBuffer_GetNames(_nativeHandle, ptrNames))
            {
              _channelNames = new string[ChannelCount];
              for (int i = 0; i < ChannelCount; i++)
                if (ptrNames[i] != IntPtr.Zero)
                  _channelNames[i] = Marshal.PtrToStringAnsi(ptrNames[i]);
            }
            else
            {
              _channelNames = new string[0];
            }
          }
          else
          {
#pragma warning disable 0162
            throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
          }
        }

        return _channelNames;
      }
    }

    public int GetChannelIndex(string channelName)
    {
      var index = Array.IndexOf(ChannelNames, channelName);

      if (index < 0)
      {
        string suggestion = string.Empty;
        var lowercase = channelName.ToLower();

        if (Array.IndexOf(ChannelNames, lowercase) >= 0)
          suggestion = string.Format("Did you mean \"{0}\"?", lowercase);

        ARLog._ErrorFormat
        (
          "Invalid channelName \"{0}\". {1}",
          channelName,
          suggestion
        );
      }

      return index;
    }

    public UInt32 GetChannelTextureMask(int channelIndex)
    {
      // test for invalid index
      if (channelIndex < 0 || channelIndex >= ChannelNames.Length)
        return 0;

      return 1u << (BitsPerPixel - 1 - channelIndex);
    }

    public UInt32 GetChannelTextureMask(int[] channelIndices)
    {
      UInt32 mask = 0;

      for (int i = 0; i < channelIndices.Length; i++)
        mask |= GetChannelTextureMask(channelIndices[i]);

      return mask;
    }

    public UInt32 GetChannelTextureMask(string channelName)
    {
      var index = GetChannelIndex(channelName);
      return GetChannelTextureMask(index);
    }

    public UInt32 GetChannelTextureMask(string[] channelNames)
    {
      UInt32 mask = 0;

      for (int i = 0; i < channelNames.Length; i++)
        mask |= GetChannelTextureMask(channelNames[i]);

      return mask;
    }

    public bool DoesChannelExistAt(int x, int y, int channelIndex)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        return _SemanticBuffer_DoesChannelExistAt(_nativeHandle, x, y, channelIndex);

#pragma warning disable 0162
      throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
    }

    public bool DoesChannelExistAt(int x, int y, string channelName)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        return _SemanticBuffer_DoesChannelExistAtByName(_nativeHandle, x, y, channelName);

#pragma warning disable 0162
      throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
    }

    public bool DoesChannelExistAt(Vector2 uv, int channelIndex)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        return _SemanticBuffer_DoesChannelExistAtNormalised(_nativeHandle, uv.x, uv.y, channelIndex);

#pragma warning disable 0162
      throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
    }

    public bool DoesChannelExistAt(Vector2 uv, string channelName)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        return
          _SemanticBuffer_DoesChannelExistAtNormalisedByName
          (
            _nativeHandle,
            uv.x,
            uv.y,
            channelName
          );
      }

#pragma warning disable 0162
      throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
    }

    public bool DoesChannelExistAt
    (
      Vector2 point,
      int viewportWidth,
      int viewportHeight,
      int channelIndex
    )
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        return _SemanticBuffer_DoesChannelExistAtViewpoint
          (_nativeHandle, point.x, point.y, viewportWidth, viewportHeight, channelIndex);

#pragma warning disable 0162
      throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
    }

    public bool DoesChannelExistAt
    (
      Vector2 point,
      int viewportWidth,
      int viewportHeight,
      string channelName
    )
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        return _SemanticBuffer_DoesChannelExistAtViewpointByName
          (_nativeHandle, point.x, point.y, viewportWidth, viewportHeight, channelName);

#pragma warning disable 0162
      throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
    }

    public bool DoesChannelExist(int channelIndex)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        return _SemanticBuffer_DoesChannelExist(_nativeHandle, channelIndex);

#pragma warning disable 0162
      throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
    }

    public bool DoesChannelExist(string channelName)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        return _SemanticBuffer_DoesChannelExistByName(_nativeHandle, channelName);

#pragma warning disable 0162
      throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
    }

    /// <inheritdoc />
    public bool CreateOrUpdateTextureARGB32
    (
      ref Texture2D texture,
      int channelIndex,
      FilterMode filterMode = FilterMode.Point
    )
    {
      uint flag = 1u << (BitsPerPixel - 1 - channelIndex);
      return _AwarenessBufferHelper._CreateOrUpdateTextureARGB32
      (
        Data,
        (int)Width,
        (int)Height,
        ref texture,
        filterMode,
        val => (val & flag) != 0 ? 1.0f : 0.0f
      );
    }

    public bool CreateOrUpdateTextureARGB32
    (
      ref Texture2D texture,
      int[] channels,
      FilterMode filterMode = FilterMode.Point
    )
    {
      uint flag = GetChannelTextureMask(channels);
      return _AwarenessBufferHelper._CreateOrUpdateTextureARGB32
      (
        Data,
        (int)Width,
        (int)Height,
        ref texture,
        filterMode,
        val => (val & flag) != 0 ? 1.0f : 0.0f
      );
    }

    /// <inheritdoc />
    public bool CreateOrUpdateTextureRFloat
    (
      ref Texture2D texture,
      FilterMode filterMode = FilterMode.Point
    )
    {
      return _AwarenessBufferHelper._CreateOrUpdateTextureRFloat
      (
        Data,
        (int)Width,
        (int)Height,
        ref texture,
        filterMode
      );
    }

    public override IAwarenessBuffer GetCopy()
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        var newHandle = _SemanticBuffer_GetCopy(_nativeHandle);
        return new _NativeSemanticBuffer(newHandle, _worldScale, Intrinsics);
      }

#pragma warning disable 0162
      throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
    }

    public ISemanticBuffer RotateToScreenOrientation()
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        var newHandle = _SemanticBuffer_RotateToScreenOrientation(_nativeHandle);
        return new _NativeSemanticBuffer(newHandle, _worldScale, Intrinsics);
      }

#pragma warning disable 0162
      throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
    }

    public ISemanticBuffer Interpolate
    (
      IARCamera arCamera,
      int viewportWidth,
      int viewportHeight,
      float backProjectionDistance = AwarenessParameters.DefaultBackProjectionDistance
    )
    {
      var projectionMatrix =
        arCamera.CalculateProjectionMatrix
        (
          Screen.orientation,
          viewportWidth,
          viewportHeight,
          _SemanticBuffer_GetNearDistance(_nativeHandle),
          _SemanticBuffer_GetFarDistance(_nativeHandle)
        );

      var frameViewMatrix = arCamera.GetViewMatrix(Screen.orientation);
      var nativeProjectionMatrix = _UnityMatrixToNarArray(projectionMatrix);
      var nativeFrameViewMatrix = _UnityMatrixToNarArray(frameViewMatrix);

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        var newHandle =
          _SemanticBuffer_Interpolate
          (
            _nativeHandle,
            nativeProjectionMatrix,
            nativeFrameViewMatrix,
            backProjectionDistance
          );

        return new _NativeSemanticBuffer(newHandle, _worldScale, Intrinsics);
      }
      else
      {
#pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
      }
    }

    public ISemanticBuffer FitToViewport
    (
      int viewportWidth,
      int viewportHeight
    )
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        var newHandle = _SemanticBuffer_FitToViewport
        (
          _nativeHandle,
          viewportWidth,
          viewportHeight
        );

        return new _NativeSemanticBuffer(newHandle, _worldScale, Intrinsics);
      }
      else
      {
#pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
      }
    }

    public UInt32 Sample(Vector2 uv)
    {
      var w = (int)Width;
      var h = (int)Height;

      var x = Mathf.Clamp(Mathf.RoundToInt(uv.x * w - 0.5f), 0, w - 1);
      var y = Mathf.Clamp(Mathf.RoundToInt(uv.y * h - 0.5f), 0, h - 1);

      return Data[x + w * y];
    }

    public UInt32 Sample(Vector2 uv, Matrix4x4 transform)
    {
      var w = (int)Width;
      var h = (int)Height;

      var st = transform * new Vector4(uv.x, uv.y, 1.0f, 1.0f);
      var sx = st.x / st.z;
      var sy = st.y / st.z;

      var x = Mathf.Clamp(Mathf.RoundToInt(sx * w - 0.5f), 0, w - 1);
      var y = Mathf.Clamp(Mathf.RoundToInt(sy * h - 0.5f), 0, h - 1);

      return Data[x + w * y];
    }

    protected override void _GetViewMatrix(float[] outViewMatrix)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _SemanticBuffer_GetView(_nativeHandle, outViewMatrix);
#pragma warning disable 0162
      else
        throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
    }

    protected override void _GetIntrinsics(float[] outVector)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _SemanticBuffer_GetIntrinsics(_nativeHandle, outVector);
#pragma warning disable 0162
      else
        throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
    }

    protected override void _OnRelease()
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _SemanticBuffer_Release(_nativeHandle);
#pragma warning disable 0162
      else
        throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
    }

    protected override IntPtr _GetDataAddress()
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        return _SemanticBuffer_GetDataAddress(_nativeHandle);
#pragma warning disable 0162
      throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
    }

    private static uint GetNativeWidth(IntPtr nativeHandle)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        return _SemanticBuffer_GetWidth(nativeHandle);
#pragma warning disable 0162
      throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
    }

    private static uint GetNativeHeight(IntPtr nativeHandle)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        return _SemanticBuffer_GetHeight(nativeHandle);
#pragma warning disable 0162
      throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
    }


    private static bool IsNativeKeyframe(IntPtr nativeHandle)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        return _SemanticBuffer_IsKeyframe(nativeHandle);
#pragma warning disable 0162
      throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _SemanticBuffer_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _SemanticBuffer_GetWidth(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _SemanticBuffer_GetHeight(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _SemanticBuffer_IsKeyframe(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _SemanticBuffer_GetView(IntPtr nativeHandle, float[] outViewMatrix);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _SemanticBuffer_GetIntrinsics(IntPtr nativeHandle, float[] outVector);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _SemanticBuffer_GetDataAddress(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _SemanticBuffer_GetNumberChannels(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern float _SemanticBuffer_GetNearDistance(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern float _SemanticBuffer_GetFarDistance(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _SemanticBuffer_GetNames
    (
      IntPtr nativeHandle,
      IntPtr[] outNames
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _SemanticBuffer_DoesChannelExistAt
    (
      IntPtr nativeHandle,
      Int32 x,
      Int32 y,
      Int32 channelIndex
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _SemanticBuffer_DoesChannelExistAtByName
    (
      IntPtr nativeHandle,
      Int32 x,
      Int32 y,
      string channelName
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _SemanticBuffer_DoesChannelExistAtNormalised
    (
      IntPtr nativeHandle,
      float u,
      float v,
      Int32 channelIndex
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _SemanticBuffer_DoesChannelExistAtNormalisedByName
    (
      IntPtr nativeHandle,
      float u,
      float v,
      string channelName
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _SemanticBuffer_DoesChannelExistAtViewpoint
    (
      IntPtr nativeHandle,
      float pointX,
      float pointY,
      Int32 viewportWidth,
      Int32 viewportHeight,
      Int32 channelIndex
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _SemanticBuffer_DoesChannelExistAtViewpointByName
    (
      IntPtr nativeHandle,
      float pointX,
      float pointY,
      Int32 viewportWidth,
      Int32 viewportHeight,
      string channelName
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _SemanticBuffer_DoesChannelExist
    (
      IntPtr nativeHandle,
      Int32 channelIndex
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _SemanticBuffer_DoesChannelExistByName
    (
      IntPtr nativeHandle,
      string channelName
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _SemanticBuffer_GetCopy(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _SemanticBuffer_RotateToScreenOrientation(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _SemanticBuffer_Interpolate
    (
      IntPtr nativeHandle,
      float[] nativeProjectionMatrix,
      float[] nativeFrameViewMatrix,
      float backProjectionDistance
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _SemanticBuffer_FitToViewport
    (
      IntPtr nativeHandle,
      int viewportWidth,
      int viewportHeight
    );
  }
}
