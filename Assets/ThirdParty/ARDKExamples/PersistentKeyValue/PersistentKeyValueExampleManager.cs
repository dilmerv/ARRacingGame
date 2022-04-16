// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;
using System.Text;

using Niantic.ARDK.Extensions;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.BinarySerialization;
using Niantic.ARDK.Utilities.BinarySerialization.ItemSerializers;

using UnityEngine;
using UnityEngine.UI;

using Random = UnityEngine.Random;

namespace Niantic.ARDKExamples
{
  /// Example of using persistent key-store to update the color of a "blobs." See the
  /// "Additional MultipeerNetworking Features" page in the User Manual for more details on
  /// the ARDK APIs used here.
  ///
  /// To run:
  ///   1. Join the same session on multiple mobile devices and/or Unity Editor instances.
  ///   2. Everyone can tap their screen to change the color of the 'Tap Tap' blob.
  ///   3. The 'Server Blob' (top) on all phones will sync to the same color when tapping stops.
  ///      Every phone will not see every color, but once tapping stops, the phones are guaranteed
  ///      to all converge to the last written color.
  public class PersistentKeyValueExampleManager:
    MonoBehaviour
  {
    /// Image of the blob that listens for and updates to the latest server color
    [SerializeField]
    private Image _serverBlob = null;

    /// Image of the blob that the local client controls
    [SerializeField]
    private Image _localBlob = null;

    // Main interaction point for the low-level networking API.
    private IMultipeerNetworking _networking;

    private readonly MemoryStream _stream = new MemoryStream(100);

    private void Awake()
    {
      MultipeerNetworkingFactory.NetworkingInitialized += OnNetworkingInitialized;
    }

    private void OnNetworkingInitialized(AnyMultipeerNetworkingInitializedArgs args)
    {
      _networking = args.Networking;

      _networking.Connected += OnConnect;
      _networking.Disconnected += OnDisconnect;
      _networking.ConnectionFailed += OnConnectionFailed;
      _networking.PersistentKeyValueUpdated += OnKeyValueUpdated;
    }

    private void OnDestroy()
    {
      if (_networking != null)
      {
        _networking.Leave();
        _networking.Dispose();
        _networking = null;
      }
    }

    // Tapping the screen after starting the session will change the local blob color and send
    // a message to the server to store that color as the latest
    void Update()
    {
      if (_networking == null || !_networking.IsConnected || PlatformAgnosticInput.touchCount <= 0)
        return;

      var touch = PlatformAgnosticInput.GetTouch(0);

      if (touch.phase == TouchPhase.Began)
      {
        Color newColor = Random.ColorHSV();
        _localBlob.color = newColor;

        _stream.Position = 0;
        _stream.SetLength(0);

        using (var binarySerializer = new BinarySerializer(_stream))
          ColorSerializer.Instance.Serialize(binarySerializer, newColor);

        var value = _stream.ToArray();
        Debug.LogFormat("Inserting to persistent store <Color: {0}>", newColor);
        _networking.StorePersistentKeyValue("Color", value);
      }
    }

    private void OnConnect(ConnectedArgs args)
    {
      _localBlob.gameObject.SetActive(true);
    }

    private void OnDisconnect(DisconnectedArgs args)
    {
      // Disconnect callback may be received while scene GameObjects are being destroyed,
      // so check that the blobs' are still valid
      if (_localBlob != null && _localBlob.gameObject != null)
        _localBlob.gameObject.SetActive(false);

      if (_serverBlob != null && _serverBlob != null)
        _serverBlob.gameObject.SetActive(false);
    }

    private void OnConnectionFailed(ConnectionFailedArgs args)
    {
      Debug.LogFormat("Connection failed (err code = {0})", args.ErrorCode);
    }

    private void OnKeyValueUpdated(PersistentKeyValueUpdatedArgs args)
    {
      if (_serverBlob == null)
      {
        // Null check required because callback may be received while GameObjects are being
        // destroyed if the scene is unloaded.
        return;
      }

      if (args.Key != "Color")
        throw new Exception("Received an update for a key other than Color");

      _serverBlob.gameObject.SetActive(true);

      using (var stream = args.CreateValueReader())
      {
        using (var binaryDeserializer = new BinaryDeserializer(stream))
        {
          var color = ColorSerializer.Instance.Deserialize(binaryDeserializer);
          _serverBlob.color = color;
        }
      }
    }
  }
}
