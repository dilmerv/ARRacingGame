// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Networking.HLAPI.Object
{
  public enum NetworkedFieldValueChangedMode
  {
    Unknown, // This should never happen, but it is here so a "default" would be an invalid value.
    Receiver, // We are on the receiving side of a value change.
    Sender // We actually caused the value change and are sending the change to other peers.
  }
}
