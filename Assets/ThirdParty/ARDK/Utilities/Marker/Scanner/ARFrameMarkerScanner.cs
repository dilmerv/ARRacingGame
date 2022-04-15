// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.AR.Frame;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.Networking.Clock;
using Niantic.ARDK.Utilities.QR;

using UnityEngine;

#if !UNITY_WEBGL
using System.Threading;
#endif

namespace Niantic.ARDK.Utilities.Marker
{
  /// <summary>
  /// This Scanner processes image textures received from the given
  /// ARSession's OnDidUpdateFrame callback to try and parse
  /// a marker in the device camera's view
  /// </summary>
  /// @note This is part of an experimental feature that is not advised to be used in release builds.
  public sealed class ARFrameMarkerScanner:
    IMarkerScanner
  {
    public event ArdkEventHandler<ARFrameMarkerScannerReadyArgs> Ready;
    public event ArdkEventHandler<ARFrameMarkerScannerStatusChangedArgs> StatusChanged;
    public event ArdkEventHandler<ARFrameMarkerScannerGotResultArgs> GotResult;

    // Store references to the AR objects utilized by the scanner
    private readonly IARSession _arSession;
    private ICoordinatedClock _coordinatedClock;
    private ARCameraFeed.TextureType _textureType;

    // Store information about last image / results
    private IARCamera _arCamera;
    private double _timestamp;
    private byte[] _rawPixels;
    private int _rawWidth;
    private int _rawHeight;
    private IParserResult _result;

    // Background thread
    private bool _parserPixelAvailable = false;
    private float _mainThreadLastDecode = 0;
    private bool _decodeInterrupted = true;
#if !UNITY_WEBGL
    private Thread _codeScannerThread;
#endif

    public MarkerScannerStatus Status
    {
      get
      {
        return _status;
      }
      private set
      {
        if (value == _status)
          return;

        _status = value;

        var handler = StatusChanged;
        if (handler != null)
        {
          var args = new ARFrameMarkerScannerStatusChangedArgs(value);
          handler(args);
        }
      }
    }

    private MarkerScannerStatus _status;

    public IMarkerParser MarkerParser { get; private set; }
    private MarkerScannerSettings _settings;

    // If no IMarkerParser is provided, will default to using a barcode parser
    public ARFrameMarkerScanner
    (
      IARNetworking arNetworking,
      MarkerScannerSettings settings = null,
      IMarkerParser markerParser = null
    )
    {
      _arSession = arNetworking.ARSession;
      _coordinatedClock = arNetworking.Networking.CoordinatedClock;
      InitializeFrameSettings();

      _settings = settings ?? new MarkerScannerSettings();
      MarkerParser = markerParser ?? new ZXingBarcodeParser(_settings);

      Status = MarkerScannerStatus.Initialized;
    }

    ~ARFrameMarkerScanner()
    {
      Debug.LogError("ARFrameMarkerScanner should be destroyed by an explicit call to Dispose().");
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);

      Stop();

      // clean events
      StatusChanged = null;
      Ready = null;
      GotResult = null;

      // clean returns
      _result = null;
      _parserPixelAvailable = false;

      // clean references
      MarkerParser = null;
      _coordinatedClock = null;
    }

    private void InitializeFrameSettings()
    {
#if AR_NATIVE_SUPPORT && UNITY_ANDROID
      _textureType = ARCameraFeed.TextureType.BGRA;
#else
      _textureType = ARCameraFeed.TextureType.YCbCr;
#endif
    }

    /// <summary>
    /// Used to start Scanning
    /// </summary>
    /// <param name="callback"></param>
    public void Scan()
    {
      if (Status == MarkerScannerStatus.Running)
      {
        Debug.Log("This ARFrameMarkerScanner is already running.");
        return;
      }

#if !UNITY_WEBGL
      if (_settings.ScannerBackgroundThread)
      {
        if (_codeScannerThread != null)
          Stop();

        _decodeInterrupted = false;
        _codeScannerThread = new Thread(ThreadTryToParse);
        _codeScannerThread.Start();
      }
#endif

      Debug.Log("ARFrameMarkerScanner started.");
      Status = MarkerScannerStatus.Running;
      _arSession.FrameUpdated += Update;
    }

    /// <summary>
    /// Used to Stop Scanning
    /// </summary>
    public void Stop()
    {
      Debug.Log("ARFrameMarkerScanner stopped.");

      // Stop thread / Clean callback
#if !UNITY_WEBGL
      if (_codeScannerThread != null)
      {
        _decodeInterrupted = true;
        _codeScannerThread.Join();
        _codeScannerThread = null;
      }
#endif

      Status = MarkerScannerStatus.Paused;
      _arSession.FrameUpdated -= Update;
    }

#region Unthread
    /// <summary>
    /// Process Image Decoding in the main Thread
    /// Background Thread : OFF
    /// </summary>
    private void TryToParse()
    {
      // Wait
      if (Status != MarkerScannerStatus.Running || !_parserPixelAvailable)
        return;

      try
      {
        ConvertTextureAndDecode();
      }
      catch (Exception e)
      {
        Debug.LogError(e);
      }
    }
#endregion

#region Background Thread
#if !UNITY_WEBGL
    /// <summary>
    /// Process Image Decoding in a Background Thread
    /// Background Thread : ON
    /// </summary>
    private void ThreadTryToParse()
    {
      while (_decodeInterrupted == false && _result == null)
      {
        // Wait
        if (Status != MarkerScannerStatus.Running || !_parserPixelAvailable)
        {
          Thread.Sleep(Mathf.FloorToInt(_settings.ScannerDecodeInterval * 1000));
          continue;
        }

        try
        {
          ConvertTextureAndDecode();
          
          if (_result == null)
            continue;

          // Sleep a little bit and set the signal to get the next frame
          Thread.Sleep(Mathf.FloorToInt(_settings.ScannerDecodeInterval * 1000));
        }
        catch (ThreadAbortException)
        {
        }
        catch (Exception e)
        {
          Debug.LogError(e);
        }
      }
    }
#endif
#endregion

    private void ConvertTextureAndDecode()
    {
      // Convert Texture
      var pixels = new Color32[_rawWidth * _rawHeight];

      var rawIndex = 0;
      for (var idx = 0; idx < _rawWidth * _rawHeight; idx++)
      {
        if (_textureType == ARCameraFeed.TextureType.YCbCr)
        {
          // Use Y value in YCbCr texture to create a greyscale texture
          var val = _rawPixels[idx];
          pixels[idx] = new Color32(val, val, val, 255);
        }
        else
        {
          pixels[idx] =
            new Color32
            (
              _rawPixels[rawIndex + 2],
              _rawPixels[rawIndex + 1],
              _rawPixels[rawIndex],
              _rawPixels[rawIndex + 3]
            );

          rawIndex += 4;
        }
      }

      // And try to Decode
      TryToDecodePixels(pixels);
    }

    private void TryToDecodePixels(Color32[] pixels)
    {
      IParserResult parserResult;
      var parserSuccess =
        MarkerParser.Decode
        (
          pixels,
          _rawWidth,
          _rawHeight,
          out parserResult
        );

      if (parserSuccess)
      {
        parserResult.ARCamera = _arCamera;
        parserResult.Timestamp = _timestamp;

        var rawShortSide = Math.Min(_rawWidth, _rawHeight);
        for (var i = 0; i < parserResult.DetectedPoints.Length; i++)
          parserResult.DetectedPoints[i].y = rawShortSide - parserResult.DetectedPoints[i].y;

        _result = parserResult;
      }

      _parserPixelAvailable = false;
    }

    public void Update()
    {
      var args = new FrameUpdatedArgs(_arSession.CurrentFrame);
      Update(args);
    }

    /// <summary>
    /// This Update Loop is used to :
    /// * Bring back Succeeded to the main thread when using Background Thread
    /// * To execute image Decoding When not using the background Thread
    /// </summary>
    private void Update(FrameUpdatedArgs updateArgs)
    {
      var readyHandler = Ready;
      if (readyHandler != null)
      {
        Ready = null;

        var readyArgs = new ARFrameMarkerScannerReadyArgs();
        readyHandler(readyArgs);
      }

      if (Status == MarkerScannerStatus.Running)
      {
        // Call the callback if a result is there
        if (_result != null)
        {
          var gotResultHandler = GotResult;
          if (gotResultHandler != null)
          {
            var args = new ARFrameMarkerScannerGotResultArgs(_result);
            gotResultHandler(args);
          }

          // clean and return
          _result = null;
          _parserPixelAvailable = false;

          return;
        }

        if (!_parserPixelAvailable)
        {
          var frame = updateArgs.Frame;
          _arCamera = frame.Camera;
          _timestamp = _coordinatedClock.CurrentCorrectedTime;

          // Use raw data instead of doing extra step of getting pixels from texture
          // For Android this will get the BGRA data
          // For iOS this will get the Y of the YCbCr data, which is all the parser needs
          _rawPixels = frame.CapturedImageBuffer.Planes[0].Data.ToArray();
          _rawWidth = frame.Camera.CPUImageResolution.width;
          _rawHeight = frame.Camera.CPUImageResolution.height;

          _parserPixelAvailable = true;
        }

        // If background thread OFF, do the decode main thread with a pause for UI
        bool shouldParse = !_settings.ScannerBackgroundThread &&
          _mainThreadLastDecode < Time.realtimeSinceStartup - _settings.ScannerDecodeInterval;
        
        if (shouldParse)
        {
          TryToParse();
          _mainThreadLastDecode = Time.realtimeSinceStartup;
        }
      }
    }

    public override string ToString()
    {
      return "[ARFrameMarkerScanner]";
    }

    internal void TestOnly_SetTextureType(ARCameraFeed.TextureType newType)
    {
      _textureType = newType;
    }
  }
}
