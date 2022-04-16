// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Networking.HLAPI.Object
{
  /// <summary>
  /// A field that will be replicated over the network.
  /// </summary>
  /// <typeparam name="TValue"></typeparam>
  public interface INetworkedField<TValue>:
    INetworkedDataHandler
  {
    /// <summary>
    /// The value of the field.
    /// </summary>
    Optional<TValue> Value { get; set; }

    /// <summary>
    /// Sets the value only if the local peer is the sender for this field.
    /// </summary>
    /// <param name="newValue">The new value for the field.</param>
    void SetIfSender(TValue newValue);

    /// <summary>
    /// Fired on all peers when the value changes.
    /// </summary>
    event ArdkEventHandler<NetworkedFieldValueChangedArgs<TValue>> ValueChanged;

    /// <summary>
    /// Fired on the sender when the value changes.
    /// </summary>
    event ArdkEventHandler<NetworkedFieldValueChangedArgs<TValue>> ValueChangedIfSender;

    /// <summary>
    /// Fired on the receiver when the value changes.
    /// </summary>
    event ArdkEventHandler<NetworkedFieldValueChangedArgs<TValue>> ValueChangedIfReceiver;
  }
}
