// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

using Niantic.ARDK.AR;
using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Networking
{
  /// <summary>
  /// Represents a single peer/user in a networking session.
  /// Each peer is identified by a GUID
  /// </summary>
  internal class _Peer:
    IPeer
  {
    private static readonly _WeakValueDictionary<Guid, _Peer> _allPeers =
      new _WeakValueDictionary<Guid, _Peer>();

    private static readonly Func<Guid, _Peer> _createNativePeer =
      (identifier) => new _Peer(identifier);

    internal static _Peer FromIdentifier(Guid identifier)
    {
      return _allPeers.GetOrAdd(identifier, _createNativePeer);
    }

    internal static _Peer FromNativeHandle(IntPtr nativeHandle)
    {
      if (nativeHandle == IntPtr.Zero)
        throw new ArgumentException("Must not be zero!", nameof(nativeHandle));

      Guid identifier;
      _NARPeerInfo_GetIdentifier(nativeHandle, out identifier);
      _NARPeerInfo_Release(nativeHandle);

      return FromIdentifier(identifier);
    }

    /// <inheritdoc />
    public Guid Identifier { get; private set; }

    protected _Peer(Guid identifier)
    {
      Identifier = identifier;
    }

    /// <summary>
    /// Directly release a native peer without creating an object.
    /// </summary>
    internal static void _ReleasePeer(IntPtr rawPeer)
    {
      // Do not call into native if this is run during testing
#pragma warning disable CS0162
      if (NativeAccess.Mode == NativeAccess.ModeType.Testing)
        return;
#pragma warning restore CS0162

      _NARPeerInfo_Release(rawPeer);
    }

    /// <summary>
    /// Compares if a peer is equal to this through GUID
    /// @param info Peer to compare against
    /// </summary>
    public bool Equals(IPeer info)
    {
      return info != null && Identifier.Equals(info.Identifier);
    }

    /// <summary>
    /// Compares an object to this through GUID
    /// @param obj Some object to compare against
    /// </summary>
    public override bool Equals(object obj)
    {
      return Equals(obj as IPeer);
    }

    /// <summary>
    /// Returns the hashcode for this instance
    /// </summary>
    public override int GetHashCode()
    {
      return Identifier.GetHashCode();
    }

    public override string ToString()
    {
      return string.Format("Peer: {0}", Identifier);
    }

    /// <summary>
    /// ToString implementation that limits the guid length to a supplied number, for easy printing
    /// </summary>
    public string ToString(int count)
    {
      return string.Format("Peer: {0}", Identifier.ToString().Substring(0, count));
    }

    // Private pointers and callbacks to handle native code

    private IntPtr _nativeHandle = IntPtr.Zero;

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARPeerInfo_GetIdentifier(IntPtr nativeHandle, out Guid identifier);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARPeerInfo_Release(IntPtr nativeHandle);
  }
}
