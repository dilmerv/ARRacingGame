// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.Utilities.Marker;
using Niantic.ARDK.Utilities.QR;
using UnityEngine;
using UnityEngine.UI;
using ZXing;

namespace Niantic.ARDK.Extensions.MarkerSync {
  /// @note This is part of an experimental feature that is not advised to be used in release builds.
  public class BarcodeDisplay : MonoBehaviour
  {
    /// <summary>
    ///  The image component to draw the generated barcode to
    /// </summary>
    [SerializeField]
    private RawImage _image = null;

    /// <summary>
    /// The image component for displaying a border around the generated barcode
    /// </summary>
    [SerializeField]
    private RawImage _borderImage = null;

    /// <summary>
    /// The image component for hiding the view behind the barcode
    /// </summary>
    [SerializeField]
    private RawImage _backgroundImage = null;

    [SerializeField]
    private BarcodeFormat _format = BarcodeFormat.QR_CODE;

    public Vector2 Center { get; private set; }

    /// <summary>
    /// Each corner provides its screen space value.
    /// The returned array of 4 vertices is clockwise.
    /// It starts bottom left and rotates to top left, then top right, and finally bottom right.
    /// </summary>
    public Vector2[] Points { get; private set; }

    private bool _generatedCode;
    private bool _isShowing;

    private void Awake()
    {
      if (_image == null)
      {
        ARLog._Error("BarcodeDisplay requires a RawImage component to be assigned.");
        return;
      }

      if (_image.canvas.renderMode != RenderMode.ScreenSpaceOverlay)
      {
        // Canvas must be in ScreenSpaceOverlay Source for pixel positions to be correct
        ARLog._Error("BarcodeDisplay requires the image be displayed on a ScreenSpaceOverlay canvas.");
        return;
      }

      SetPixelPositions();
      Hide(true);
    }

    private void SetPixelPositions()
    {
      var rectPosition = _image.rectTransform.position;

      // Todo: Get working for different anchors and offsets
      Center = new Vector2(rectPosition.x, rectPosition.y);
      Points = new Vector2[4];
    }

    public void Show(bool force = false)
    {
      if (!_generatedCode)
      {
        ARLog._Error("Must generate the barcode before showing.");
        return;
      }

      if (_isShowing && !force) { return;}

      ARLog._Debug("Show BarcodeDisplay");
      ToggleComponents(true);
    }

    public void Hide(bool force = false)
    {
      if (!_isShowing && !force) { return; }

      ARLog._Debug("Hide BarcodeDisplay");
      ToggleComponents(false);
    }

    private void ToggleComponents(bool isEnabled)
    {
      _isShowing = isEnabled;

      if (_image != null)
      {
        _image.enabled = isEnabled;
      }

      if (_borderImage != null)
      {
        _borderImage.enabled = isEnabled;
      }

      if (_backgroundImage != null)
      {
        _backgroundImage.enabled = isEnabled;
      }
    }

    public ZXingMarkerGenerator.MarkerGenerationResult GenerateBarcode
    (
      MarkerMetadata info,
      bool showAfterGenerating = false
    )
    {
      _generatedCode = true;

      var dimensions = _image.rectTransform.sizeDelta;

      var width = (int) dimensions.x;
      var height = (int) dimensions.y;
      var generatorResult = ZXingMarkerGenerator.GenerateBarcode
      (
        info,
        _format,
        width,
        height
      );

      // Parse and save corrected point positions
      var parser = new ZXingBarcodeParser();

      IParserResult parserResult;
      var parserSuccess = parser.Decode
      (
        generatorResult.RawPixels,
        width,
        height,
        out parserResult
      );

      if (parserSuccess)
      {
        var translation = new Vector2(Center.x - (width / 2), Center.y - (height / 2));
        for (var i = 0; i < 4; i++)
        {
          Points[i] = parserResult.DetectedPoints[i] + translation;
        }
      }
      else
      {
        ARLog._Error("Error trying to generate barcode texture.");
        return null;
      }

      // Setup the RawImage
      _image.texture = generatorResult.Texture;

      if (showAfterGenerating) {
        Show();
      }

      return generatorResult;
    }
  }
}
