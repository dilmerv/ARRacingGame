using System;

using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.Awareness
{
  public class ContextAwarenessArgs<TBuffer>:
    IArdkEventArgs
  where TBuffer: class, IAwarenessBuffer, IDisposable
  {
    /// The context awareness processor.
    public readonly AwarenessBufferProcessor<TBuffer> Sender;

    public ContextAwarenessArgs(AwarenessBufferProcessor<TBuffer> sender)
    {
      Sender = sender;
    }
  }

  public class ContextAwarenessStreamUpdatedArgs<TBuffer>:
    ContextAwarenessArgs<TBuffer>
  where TBuffer: class, IAwarenessBuffer, IDisposable
  {
    /// Whether the contents of the buffer has been updated.
    public readonly bool IsKeyFrame;

    public ContextAwarenessStreamUpdatedArgs
    (
      AwarenessBufferProcessor<TBuffer> sender,
      bool isKeyFrame
    )
      : base(sender)
    {
      IsKeyFrame = isKeyFrame;
    }
  }
}
