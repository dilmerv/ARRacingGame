// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Utilities
{
  // Mark only interface for event-args. The only reason to have this interface is to avoid
  // a possible accidental use of an ArdkEventHandler with a non-args object (like passing
  // a MultipeerNetworking object instead of an args that contains a MultipeerNetworking).
  public interface IArdkEventArgs
  {
  }

  public delegate void ArdkEventHandler<TArgs>(TArgs args)
  where
    TArgs: IArdkEventArgs;
}
