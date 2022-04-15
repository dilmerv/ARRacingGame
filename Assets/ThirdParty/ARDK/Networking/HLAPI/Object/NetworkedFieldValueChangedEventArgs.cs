// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Networking.HLAPI.Object
{
  public struct NetworkedFieldValueChangedArgs<TValue>:
    IArdkEventArgs
  {
    public readonly NetworkedFieldValueChangedMode Mode;
    public readonly Optional<TValue> Value;

    public NetworkedFieldValueChangedArgs
    (
      NetworkedFieldValueChangedMode mode,
      Optional<TValue> value
    )
    {
      Mode = mode;
      Value = value;
    }
  }
}
