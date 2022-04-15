// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

using AOT;

using Niantic.ARDK.Internals;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.VirtualStudio.Remote
{
  /// The internet version of RemoteConnection handles remoting by creating a network stack to ARBE.
  /// All work is done on the native side and provides a synchronized device role API.
  internal sealed unsafe class _InternetRemoteConnection:
    IDisposable
  {
    private void* _nativePtr;
    private SafeGCHandle<_InternetRemoteConnection> _ourHandle;

    private HashSet<_DeviceHandle> _deviceHandles = new HashSet<_DeviceHandle>();
    private _LocalDeviceInfo _localDeviceInfo;
    private _RemoteConnectionStatus _status;

    public delegate void MessageReceivedCallback(_DeviceHandle sender, Guid tag, byte[] data);

    /// <summary>
    /// Invoked when a message comes from another device.
    /// </summary>
    public event MessageReceivedCallback ReceivedMessage = (sender, tag, data) => {};

    /// <summary>
    /// The set of devices currently in the device group.
    /// </summary>
    /// <remarks>
    /// This is a copy of the internal set.
    /// TODO(bpeake): Change this to a IReadOnlyCollection once Unity can upgrade.
    /// </remarks>
    public HashSet<_DeviceHandle> Devices
    {
      get
      {
        return new HashSet<_DeviceHandle>(_deviceHandles);
      }
    }

    /// <summary>
    /// The local device info.
    /// </summary>
    public _LocalDeviceInfo LocalDeviceInfo
    {
      get
      {
        return _localDeviceInfo;
      }
    }

    /// <summary>
    /// The device group pin currently connected to. Null if not connected.
    /// </summary>
    public string DeviceGroupPin
    {
      get
      {
        var pinLength = _NAR_RemoteConnection_GetDeviceGroupPinLength(_nativePtr);

        if (pinLength <= 0)
          return null;

        var pinBuffer = new byte[(int)pinLength];

        fixed (byte* pinBufferPtr = pinBuffer)
          _NAR_RemoteConnection_GetDeviceGroupPin(_nativePtr, pinBufferPtr, pinLength);

        return Encoding.UTF8.GetString(pinBuffer);
      }
    }

    /// <summary>
    /// The status of the remote connection.
    /// </summary>
    public _RemoteConnectionStatus Status
    {
      get
      {
        return _status;
      }
    }

    /// <summary>
    /// Creates a new V2 remote connection.
    /// </summary>
    public _InternetRemoteConnection()
    {
      var stageIdentifier = Guid.NewGuid();
      var localDevice = new _DeviceHandle { Identifier = Guid.NewGuid() };

      _nativePtr =
        _NAR_RemoteConnection_Init
        (
          &stageIdentifier,
          ServerConfiguration.ARBE.Endpoint,
          &localDevice
        );

      // TODO(bpeake) make a version of this that does not get pinned in memory from a callback.
      _UpdateLoop.Tick += Update;
    }

    ~_InternetRemoteConnection()
    {
      ReleaseUnmanagedResources(true);
    }

    private bool _isDisposed;
    public void Dispose()
    {
      if (_isDisposed)
        return;

      _isDisposed = true;
      GC.SuppressFinalize(this);

      _CallbackQueue.QueueCallback
      (
        () =>
        {
           Disconnect();
           ReleaseUnmanagedResources(false);
        }
      );
    }

    /// <summary>
    /// Connects to a device group.
    /// </summary>
    /// <param name="pin">The pin of the group to join.</param>
    public void Connect(string pin)
    {
      var bytes = Encoding.UTF8.GetBytes(pin);

      // Store a native reference to this object, but do not have that reference pin it to memory.
      _ourHandle = SafeGCHandle.Alloc(this);

      _NAR_RemoteConnection_SetDidReceiveMessageCallback
      (
        _nativePtr,
        _ourHandle.ToIntPtr(),
        InternalMessageReceived
      );

      fixed (byte* bytesPtr = bytes)
        _NAR_RemoteConnection_Connect(_nativePtr, bytesPtr, (ulong)bytes.LongLength);

      // TODO(awang): Because of different message handling flows, this will be called in different
      //   places for different implementations. Unify them somehow?
      _EasyConnection.Initialize();
    }

    /// <summary>
    /// Checks if the device handle is a connected device.
    /// </summary>
    public bool HasDevice(_DeviceHandle deviceHandle)
    {
      return _NAR_RemoteConnection_DoesDeviceExist(_nativePtr, &deviceHandle);
    }

    /// <summary>
    /// Disconnects from the device group.
    /// </summary>
    public void Disconnect()
    {
      _NAR_RemoteConnection_SetDidReceiveMessageCallback(_nativePtr, IntPtr.Zero, null);

      _ourHandle.Free();
      _ourHandle = default(SafeGCHandle<_InternetRemoteConnection>);

      _NAR_RemoteConnection_Disconnect(_nativePtr);
    }

    /// <summary>
    /// Gets a device by a role.
    /// </summary>
    /// <param name="role">Role to find the corresponding device for.</param>
    /// <returns>The device handle that is that role, or none if no device has that role.</returns>
    public _DeviceHandle? GetDeviceWithRole(_RemoteDeviceRole role)
    {
      _DeviceHandle foundDevice;
      _NAR_RemoteConnection_GetDeviceFromRole(_nativePtr, role, &foundDevice);

      if (foundDevice.Identifier == default(Guid))
        return null;

      return foundDevice;
    }

    /// <summary>
    /// Gets the role of a particular device.
    /// </summary>
    public _RemoteDeviceRole GetRoleOfDevice(_DeviceHandle deviceHandle)
    {
      return _NAR_RemoteConnection_GetRoleFromDevice(_nativePtr, &deviceHandle);
    }

    /// <summary>
    /// Registers a message tag as being valid with the system.
    /// </summary>
    public void RegisterMessage(Guid tag)
    {
      _NAR_RemoteConnection_RegisterMessage(_nativePtr, &tag);
    }

    /// <summary>
    /// Sends a message to a particular device.
    /// </summary>
    /// <param name="tag">The tag to send it with.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="destination">The target device to send to.</param>
    public void SendMessage
    (
      Guid tag,
      TransportType transportType,
      byte[] message,
      _DeviceHandle destination
    )
    {
      fixed (byte* buffer = message)
      {
        _NAR_RemoteConnection_SendMessage
        (
          _nativePtr,
          &tag,
          buffer,
          (UInt64)message.LongLength,
          &destination,
          (UInt32)transportType
        );
      }
    }

    [MonoPInvokeCallback(typeof(_NAR_RemoteConnection_MessageReceiveCallback))]
    private static void InternalMessageReceived
    (
      IntPtr handle,
      void* sender,
      void* tag,
      void* buffer,
      ulong size
    )
    {
      var remoteConnection = SafeGCHandle.TryGetInstance<_InternetRemoteConnection>(handle);

      if (remoteConnection == null)
        throw new InvalidOperationException("Native gave back a bad application handle.");

      _DeviceHandle senderHandle = *(_DeviceHandle*)sender;
      Guid tagPtr = *(Guid*)tag;

      var data = new byte[size];
      Marshal.Copy((IntPtr)buffer, data, 0, checked((int)size));

      _CallbackQueue.QueueCallback(() => remoteConnection.ReceivedMessage(senderHandle, tagPtr, data));

    }

    private void Update()
    {
      if (_nativePtr == null)
        return;

      _NAR_RemoteConnection_SyncMSValues(_nativePtr);

      _deviceHandles.Clear();
      var deviceCount = _NAR_RemoteConnection_DeviceCount(_nativePtr);
      _DeviceHandle* devices = stackalloc _DeviceHandle[deviceCount];
      deviceCount = _NAR_RemoteConnection_DeviceBuffer(_nativePtr, devices, deviceCount);

      for (var i = 0; i < deviceCount; i++)
        _deviceHandles.Add(devices[i]);

      fixed (_LocalDeviceInfo* localDeviceInfoPtr = &_localDeviceInfo)
        _NAR_RemoteConnection_GetLocalDevice(_nativePtr, localDeviceInfoPtr);

      fixed (_RemoteConnectionStatus* statusPtr = &_status)
        _NAR_RemoteConnection_GetStatus(_nativePtr, statusPtr);

      _NAR_RemoteConnection_ProcessNetworkMessages(_nativePtr);
    }

    private void ReleaseUnmanagedResources(bool isFromGC)
    {
      if (_nativePtr != null)
      {
        // Forces a copy of the ptr value, rather than a reference to this, which would prevent GC.
        var nativePtrCpy = _nativePtr;

        // Actually dtor on the main thread.
        _CallbackQueue.QueueCallback(() => _NAR_RemoteConnection_Release(nativePtrCpy));

        // When getting called from GC, we know that there cannot be an update callback still.
        if (!isFromGC)
          _CallbackQueue.QueueCallback(() => _UpdateLoop.Tick -= Update);

        _nativePtr = null;
      }
    }

    [DllImport(_ARDKLibrary.libraryName)]
    [SuppressUnmanagedCodeSecurity]
    private static extern void* _NAR_RemoteConnection_Init
    (
      Guid* stageIdentifier,
      string tcpAddress,
      _DeviceHandle* localDeviceHandle
    );

    [DllImport(_ARDKLibrary.libraryName)]
    [SuppressUnmanagedCodeSecurity]
    private static extern void _NAR_RemoteConnection_Release(void* nativePtr);

    [DllImport(_ARDKLibrary.libraryName)]
    [SuppressUnmanagedCodeSecurity]
    private static extern void _NAR_RemoteConnection_Connect
    (
      void* nativePtr,
      byte* deviceGroupPin,
      UInt64 deviceGroupPinLength
    );

    [DllImport(_ARDKLibrary.libraryName)]
    [SuppressUnmanagedCodeSecurity]
    private static extern Int32 _NAR_RemoteConnection_DeviceBuffer
    (
      void* nativePtr,
      _DeviceHandle* outBuffer,
      Int32 sizeOfOutBuffer
    );

    [DllImport(_ARDKLibrary.libraryName)]
    [SuppressUnmanagedCodeSecurity]
    private static extern Int32 _NAR_RemoteConnection_DeviceCount(void* nativePtr);

    [DllImport(_ARDKLibrary.libraryName)]
    [SuppressUnmanagedCodeSecurity]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool _NAR_RemoteConnection_DoesDeviceExist
    (
      void* nativePtr,
      _DeviceHandle* deviceHandle
    );

    [DllImport(_ARDKLibrary.libraryName)]
    [SuppressUnmanagedCodeSecurity]
    private static extern void _NAR_RemoteConnection_Disconnect(void* nativePtr);

    [DllImport(_ARDKLibrary.libraryName)]
    [SuppressUnmanagedCodeSecurity]
    private static extern void _NAR_RemoteConnection_GetDeviceFromRole
    (
      void* nativePtr,
      _RemoteDeviceRole role,
      _DeviceHandle* outDeviceHandle
    );

    [DllImport(_ARDKLibrary.libraryName)]
    [SuppressUnmanagedCodeSecurity]
    private static extern _RemoteDeviceRole _NAR_RemoteConnection_GetRoleFromDevice
    (
      void* nativePtr,
      _DeviceHandle* deviceHandle
    );

    [DllImport(_ARDKLibrary.libraryName)]
    [SuppressUnmanagedCodeSecurity]
    private static extern void _NAR_RemoteConnection_GetLocalDevice
    (
      void* nativePtr,
      _LocalDeviceInfo* outLocalDeviceInfo
    );

    [DllImport(_ARDKLibrary.libraryName)]
    [SuppressUnmanagedCodeSecurity]
    private static extern void _NAR_RemoteConnection_SyncMSValues(void* nativePtr);

    [DllImport(_ARDKLibrary.libraryName)]
    [SuppressUnmanagedCodeSecurity]
    private static extern void _NAR_RemoteConnection_ProcessNetworkMessages(void* nativePtr);

    [DllImport(_ARDKLibrary.libraryName)]
    [SuppressUnmanagedCodeSecurity]
    private static extern void _NAR_RemoteConnection_RegisterMessage(void* nativePtr, Guid* tag);

    [DllImport(_ARDKLibrary.libraryName)]
    [SuppressUnmanagedCodeSecurity]
    private static extern void _NAR_RemoteConnection_SendMessage
    (
      void* nativePtr,
      Guid* tag,
      void* data,
      UInt64 size,
      _DeviceHandle* destination,
      UInt32 connectionType
    );

    // This has to use void* because of a bug with IL2CPP.
    delegate void _NAR_RemoteConnection_MessageReceiveCallback
    (
      IntPtr applicationHandle,
      void* sender, // Device handle
      void* tag, // UUID
      void* buffer,
      UInt64 size
    );

    [DllImport(_ARDKLibrary.libraryName)]
    [SuppressUnmanagedCodeSecurity]
    private static extern void _NAR_RemoteConnection_SetDidReceiveMessageCallback
    (
      void* nativePtr,
      IntPtr applicationHandle,
      _NAR_RemoteConnection_MessageReceiveCallback callback
    );

    [DllImport(_ARDKLibrary.libraryName)]
    [SuppressUnmanagedCodeSecurity]
    private static extern UInt64 _NAR_RemoteConnection_GetDeviceGroupPinLength(void* nativePtr);

    [DllImport(_ARDKLibrary.libraryName)]
    [SuppressUnmanagedCodeSecurity]
    private static extern void _NAR_RemoteConnection_GetDeviceGroupPin
    (
      void* nativePtr,
      void* outPin,
      UInt64 outBufferSize
    );

    [DllImport(_ARDKLibrary.libraryName)]
    [SuppressUnmanagedCodeSecurity]
    private static extern void _NAR_RemoteConnection_GetStatus
    (
      void* nativePtr,
      _RemoteConnectionStatus* outRemoteConnectionStatus
    );
  }
}
