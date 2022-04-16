using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.Rendering;

using UnityEngine;

namespace Niantic.ARDK.AR.Awareness.Semantics
{
  public class SemanticBufferProcessor: 
    AwarenessBufferProcessor<ISemanticBuffer>, 
    ISemanticBufferProcessor
  {
    // The currently active AR session
    private IARSession _session;

    // The render target descriptor used to determine the viewport resolution
    private RenderTarget _viewport;

    /// Allocates a new semantic buffer processor. By default, the
    /// awareness buffer will be fit to the main camera's viewport.
    public SemanticBufferProcessor()
    {
      _viewport = UnityEngine.Camera.main;
      ARSessionFactory.SessionInitialized += OnARSessionInitialized;
    }
    
    /// Allocates a new semantic buffer processor.
    /// @param viewport Determines the target viewport to fit the awareness buffer to.
    public SemanticBufferProcessor(RenderTarget viewport)
    {
      _viewport = viewport;
      ARSessionFactory.SessionInitialized += OnARSessionInitialized;
    }
    
    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);
      
      ARSessionFactory.SessionInitialized -= OnARSessionInitialized;
      if (_session != null)
        _session.FrameUpdated -= OnFrameUpdated;
    }
    
    private void OnARSessionInitialized(AnyARSessionInitializedArgs args)
    {
      if (_session != null)
        _session.FrameUpdated -= OnFrameUpdated;

      _session = args.Session;
      _session.FrameUpdated += OnFrameUpdated;
    }

    private void OnFrameUpdated(FrameUpdatedArgs args)
    {
      var frame = args.Frame;
      if (frame == null)
        return;

#if UNITY_EDITOR 
      var orientation = Screen.width > Screen.height
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;
#else
      var orientation = Screen.orientation;
#endif
      
      ProcessFrame
      (
        buffer: frame.Semantics,
        arCamera: frame.Camera,
        targetResolution: _viewport.GetResolution(forOrientation: orientation),
        targetOrientation: orientation
      );
    }

    /// Assigns a new render target descriptor for the semantics processor.
    /// The render target defines the viewport attributes to correctly fit
    /// the semantics buffer. 
    public void AssignViewport(RenderTarget target)
    {
      _viewport = target;
    }

    /// The number of classes available.
    public uint ChannelCount
    {
      get => AwarenessBuffer?.ChannelCount ?? 0;
    }

    /// Returns the possible semantic classes that a pixel can be interpreted.
    public string[] Channels
    {
      get => AwarenessBuffer?.ChannelNames;
    }

    /// <inheritdoc />
    public uint GetSemantics(int viewportX, int viewportY)
    {
      var semanticsBuffer = AwarenessBuffer;
      if (semanticsBuffer == null)
        return 0u;
      
      // Get normalized coordinates
      var x = viewportX + 0.5f;
      var y = viewportY + 0.5f;
      var resolution = _viewport.GetResolution(Screen.orientation);
      var uv = new Vector3(x / resolution.width, y / resolution.height, 1.0f);

      return AwarenessBuffer.Sample(uv, SamplerTransform);
    }

    /// <inheritdoc />
    public int[] GetChannelIndicesAt(int viewportX, int viewportY)
    {
      var semanticsBuffer = AwarenessBuffer;
      if (semanticsBuffer == null)
        return Array.Empty<int>();

      var buffer = AwarenessBuffer;
      int count = (int)ChannelCount;
      var semantics = GetSemantics(viewportX, viewportY);

      var result = new List<int>(capacity: count);
      for (int i = 0; i < count; i++)
      {
        var mask = buffer.GetChannelTextureMask(i);
        if ((semantics & mask) != 0u)
          result.Add(i);
      }

      return result.ToArray();
    }

    /// <inheritdoc />
    public string[] GetChannelNamesAt(int viewportX, int viewportY)
    {
      var semanticsBuffer = AwarenessBuffer;
      if (semanticsBuffer == null)
        return Array.Empty<string>();

      var buffer = AwarenessBuffer;
      var channels = Channels;
      var semantics = GetSemantics(viewportX, viewportY);

      return (
        from channel in channels
        let mask = buffer.GetChannelTextureMask(channel)
        where (semantics & mask) != 0u
        select channel).ToArray();
    }

    /// <inheritdoc />
    public bool DoesChannelExistAt(int viewportX, int viewportY, int channelIndex)
    {
      var semantics = GetSemantics(viewportX, viewportY);
      var bitMask = AwarenessBuffer.GetChannelTextureMask(channelIndex);

      return (semantics & bitMask) != 0u;
    }

    /// <inheritdoc />
    public bool DoesChannelExistAt(int viewportX, int viewportY, string channelName)
    {
      var semantics = GetSemantics(viewportX, viewportY);
      var bitMask = AwarenessBuffer.GetChannelTextureMask(channelName);
      
      return (semantics & bitMask) != 0u;
    }
    
    public void CopyToAlignedTextureARGB32(int channel, ref Texture2D texture, ScreenOrientation orientation)
    {
      if (AwarenessBuffer == null)
        return;
      
      // Get a typed buffer
      ISemanticBuffer semanticsBuffer = AwarenessBuffer;

      // Acquire the affine transform for the buffer
      var transform = SamplerTransform;

      // Determine the bit mask for the requested semantic classes
      var bitMask = AwarenessBuffer.GetChannelTextureMask(channel);

      // Call base method
      CreateOrUpdateTextureARGB32
      (
        ref texture,
        orientation,
        
        // The sampler function needs to be defined such that given a destination
        // texture coordinate, what color needs to be written to that position?
        // We sample the typed awareness buffer and apply the channel bitmask.
        // White means the pixel contains at least one of the requested semantic
        // classes. The resulting texture is display aligned, that's why we sample
        // using the buffer's affine matrix.
        sampler: uv => (semanticsBuffer.Sample(uv, transform) & bitMask) != 0u
          ? Color.white
          : Color.clear
      );
    }
    
    public void CopyToAlignedTextureARGB32(int[] channels, ref Texture2D texture, ScreenOrientation orientation)
    {
      if (AwarenessBuffer == null)
        return;
      
      // Get a typed buffer
      ISemanticBuffer semanticsBuffer = AwarenessBuffer;

      // Acquire the affine transform for the buffer
      var transform = SamplerTransform;

      // Determine the bit mask for the requested semantic classes
      var bitMask = AwarenessBuffer.GetChannelTextureMask(channels);

      // Call base method
      CreateOrUpdateTextureARGB32
      (
        ref texture,
        orientation,
        
        // The sampler function needs to be defined such that given a destination
        // texture coordinate, what color needs to be written to that position?
        // We sample the typed awareness buffer and apply the channel bitmask.
        // White means the pixel contains at least one of the requested semantic
        // classes. The resulting texture is display aligned, that's why we sample
        // using the buffer's affine matrix.
        sampler: uv => (semanticsBuffer.Sample(uv, transform) & bitMask) != 0u
          ? Color.white
          : Color.clear
      );
    }
  }
}
